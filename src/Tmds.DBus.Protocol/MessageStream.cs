using System.Net.Sockets;
using System.Threading.Channels;

namespace Tmds.DBus.Protocol;

#pragma warning disable VSTHRD100 // Avoid "async void" methods

class MessageStream : IMessageStream
{
    private static readonly ReadOnlyMemory<byte> OneByteArray = new[] { (byte)0 };
    private readonly Socket? _socket;
    private readonly Stream _stream;
    private UnixFdCollection? _fdCollection;
    private bool _supportsFdPassing;
    private readonly MessagePool _messagePool;

    // Messages going out.
    private readonly ChannelReader<MessageBuffer> _messageReader;
    private readonly ChannelWriter<MessageBuffer> _messageWriter;

    // Bytes coming in.
    private readonly Sequence<byte> _receiveBuffer;

    private Exception? _completionException;
    private bool _isMonitor;

    public MessageStream(Socket socket)
        : this(socket, null)
    {}

    public MessageStream(Stream stream)
        : this(null, stream)
    {}

    private MessageStream(Socket? socket, Stream? stream)
    {
        _socket = socket;
        _stream = stream ?? new NetworkStream(_socket!, ownsSocket: true);

        Channel<MessageBuffer> channel = Channel.CreateUnbounded<MessageBuffer>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = false
        });
        _messageReader = channel.Reader;
        _messageWriter = channel.Writer;
        _receiveBuffer = new Sequence<byte>(ArrayPool<byte>.Shared) { MinimumSpanLength = 4096 };
        _messagePool = new();
    }

    public void BecomeMonitor()
    {
        _isMonitor = true;
    }

    private async void ReadMessagesIntoSocket()
    {
        while (true)
        {
            if (!await _messageReader.WaitToReadAsync().ConfigureAwait(false))
            {
                // No more messages will be coming.
                return;
            }
            var message = await _messageReader.ReadAsync().ConfigureAwait(false);
            try
            {
                UnixFdCollection? handles = _supportsFdPassing ? message.Handles : null;
                var buffer = message.AsReadOnlySequence();
                if (buffer.IsSingleSegment)
                {
                    await WriteAsync(buffer.First, handles).ConfigureAwait(false);
                }
                else
                {
                    SequencePosition position = buffer.Start;
                    while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                    {
                        await WriteAsync(memory, handles).ConfigureAwait(false);
                        handles = null;
                    }
                }
            }
            catch (Exception exception)
            {
                Close(exception);
                return;
            }
            finally
            {
                message.ReturnToPool();
            }
        }
    }

    private ValueTask WriteAsync(ReadOnlyMemory<byte> memory, UnixFdCollection? handles)
    {
        if (_socket is not null)
        {
            return _socket.SendAsync(memory, handles);
        }
        else
        {
            return _stream.WriteAsync(memory);
        }
    }

    public async void ReceiveMessages<T>(IMessageStream.MessageReceivedHandler<T> handler, T state)
    {
        var receiveBuffer = _receiveBuffer;
        try
        {
            while (true)
            {
                Memory<byte> memory = receiveBuffer.GetMemory(1024);
                int bytesRead = await ReceiveFromSocketAsync(memory).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    ThrowHelper.ThrowConnectionClosedByPeer();
                }
                receiveBuffer.Advance(bytesRead);

                ReadOnlySequence<byte> buffer = receiveBuffer.AsReadOnlySequence;
                ReadMessages(ref buffer, handler, state);
                receiveBuffer.AdvanceTo(buffer.Start);
            }
        }
        catch (Exception exception)
        {
            exception = CloseCore(exception);
            OnException(exception, handler, state);
        }
        finally
        {
            receiveBuffer.Dispose();
            _fdCollection?.Dispose();
        }

        void ReadMessages<TState>(ref ReadOnlySequence<byte> buffer, IMessageStream.MessageReceivedHandler<TState> handler, TState state)
        {
            Message? message;
            while ((message = Message.TryReadMessage(_messagePool, ref buffer, _fdCollection, _isMonitor)) != null)
            {
                // Discard any file descriptors that were received but not claimed by the message.
                _fdCollection?.DiscardHandles();

                handler(closeReason: null, message, state);
            }
        }

        static void OnException(Exception exception, IMessageStream.MessageReceivedHandler<T> handler, T state)
        {
            handler(exception, message: null!, state);
        }
    }

    private async ValueTask<int> ReceiveFromSocketAsync(Memory<byte> memory)
    {
        if (_socket is not null)
        {
            return await _socket.ReceiveAsync(memory, _fdCollection).ConfigureAwait(false);
        }
        else
        {
            return await _stream.ReadAsync(memory).ConfigureAwait(false);
        }
    }

    private struct AuthenticationResult
    {
        public bool IsAuthenticated;
        public bool SupportsFdPassing;
        public Guid Guid;
    }

    public async ValueTask DoClientAuthAsync(Guid guid, string? userId, bool supportsFdPassing)
    {
        // send 1 byte
        await _stream.WriteAsync(OneByteArray).ConfigureAwait(false);
        // auth
        var authenticationResult = await SendAuthCommandsAsync(userId, supportsFdPassing).ConfigureAwait(false);
        _supportsFdPassing = authenticationResult.SupportsFdPassing;
        if (_supportsFdPassing)
        {
            _fdCollection = new();
        }
        if (guid != Guid.Empty)
        {
            if (guid != authenticationResult.Guid)
            {
                throw new DBusConnectFailedException("Authentication failure: Unexpected GUID");
            }
        }

        ReadMessagesIntoSocket();
    }

    private async ValueTask<AuthenticationResult> SendAuthCommandsAsync(string? userId, bool supportsFdPassing)
    {
        AuthenticationResult result;
        if (userId is not null)
        {
            string command = CreateAuthExternalCommand(userId);

            result = await SendAuthCommandAsync(command, supportsFdPassing).ConfigureAwait(false);

            if (result.IsAuthenticated)
            {
                return result;
            }
        }

        result = await SendAuthCommandAsync("AUTH ANONYMOUS\r\n", supportsFdPassing).ConfigureAwait(false);
        if (result.IsAuthenticated)
        {
            return result;
        }

        throw new DBusConnectFailedException("Authentication failure");
    }

    private static string CreateAuthExternalCommand(string userId)
    {
        const string AuthExternal = "AUTH EXTERNAL ";
        const string hexchars = "0123456789abcdef";
#if NETSTANDARD2_0
        StringBuilder sb = new();
        sb.Append(AuthExternal);
        for (int i = 0; i < userId.Length; i++)
        {
            byte b = (byte)userId[i];
            sb.Append(hexchars[(int)(b >> 4)]);
            sb.Append(hexchars[(int)(b & 0xF)]);
        }
        sb.Append("\r\n");
        return sb.ToString();
#else
        return string.Create<string>(
            length: AuthExternal.Length + userId.Length * 2 + 2, userId,
            static (Span<char> span, string userId) =>
            {
                AuthExternal.AsSpan().CopyTo(span);
                span = span.Slice(AuthExternal.Length);

                for (int i = 0; i < userId.Length; i++)
                {
                    byte b = (byte)userId[i];
                    span[i * 2] = hexchars[(int)(b >> 4)];
                    span[i * 2 + 1] = hexchars[(int)(b & 0xF)];
                }
                span = span.Slice(userId.Length * 2);

                span[0] = '\r';
                span[1] = '\n';
            });
#endif
    }

    private async ValueTask<AuthenticationResult> SendAuthCommandAsync(string command, bool supportsFdPassing)
    {
        byte[] lineBuffer = ArrayPool<byte>.Shared.Rent(ProtocolConstants.MaxAuthLineLength);
        try
        {
            AuthenticationResult result = default(AuthenticationResult);
            await WriteAsync(command, lineBuffer).ConfigureAwait(false);
            int lineLength = await ReadLineAsync(lineBuffer).ConfigureAwait(false);

            if (StartsWithAscii(lineBuffer, lineLength, "OK"))
            {
                result.IsAuthenticated = true;
                result.Guid = ParseGuid(lineBuffer, lineLength);

                if (supportsFdPassing)
                {
                    await WriteAsync("NEGOTIATE_UNIX_FD\r\n", lineBuffer).ConfigureAwait(false);

                    lineLength = await ReadLineAsync(lineBuffer).ConfigureAwait(false);

                    result.SupportsFdPassing = StartsWithAscii(lineBuffer, lineLength, "AGREE_UNIX_FD");
                }

                await WriteAsync("BEGIN\r\n", lineBuffer).ConfigureAwait(false);
                return result;
            }
            else if (StartsWithAscii(lineBuffer, lineLength, "REJECTED"))
            {
                return result;
            }
            else
            {
                await WriteAsync("ERROR\r\n", lineBuffer).ConfigureAwait(false);
                return result;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(lineBuffer);
        }

        static bool StartsWithAscii(byte[] line, int length, string expected)
        {
            if (length < expected.Length)
            {
                return false;
            }
            for (int i = 0; i < expected.Length; i++)
            {
                if (line[i] != expected[i])
                {
                    return false;
                }
            }
            return true;
        }

        static Guid ParseGuid(byte[] line, int length)
        {
            Span<byte> span = new Span<byte>(line, 0, length);
            int spaceIndex = span.IndexOf((byte)' ');
            if (spaceIndex == -1)
            {
                return Guid.Empty;
            }
            span = span.Slice(spaceIndex + 1);
            spaceIndex = span.IndexOf((byte)' ');
            if (spaceIndex != -1)
            {
                span = span.Slice(0, spaceIndex);
            }
            Span<char> charBuffer = stackalloc char[span.Length]; // TODO (low prio): check length
            for (int i = 0; i < span.Length; i++)
            {
                // TODO (low prio): validate char
                charBuffer[i] = (char)span[i];
            }
#if NETSTANDARD2_0
            return Guid.ParseExact(charBuffer.AsString(), "N");
#else
            return Guid.ParseExact(charBuffer, "N");
#endif
        }
    }

    private async ValueTask WriteAsync(string message, Memory<byte> lineBuffer)
    {
        int length = Encoding.ASCII.GetBytes(message.AsSpan(), lineBuffer.Span);
        lineBuffer = lineBuffer.Slice(0, length);
        await _stream.WriteAsync(lineBuffer).ConfigureAwait(false);
    }

    private async ValueTask<int> ReadLineAsync(Memory<byte> lineBuffer)
    {
        var receiveBuffer = _receiveBuffer;
        while (true)
        {
            ReadOnlySequence<byte> buffer = receiveBuffer.AsReadOnlySequence;
            SequencePosition? position = buffer.PositionOf((byte)'\n');

            if (position.HasValue)
            {
                ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);
                if (line.Length <= ProtocolConstants.MaxAuthLineLength)
                {
                    int length = CopyBuffer(line, lineBuffer);
                    receiveBuffer.AdvanceTo(buffer.GetPosition(1, position.Value));
                    return length;
                }
            }

            if (buffer.Length > ProtocolConstants.MaxAuthLineLength)
            {
                throw new DBusConnectFailedException("Authentication message from server is too long.");
            }

            // Need more data.
            Memory<byte> memory = receiveBuffer.GetMemory(lineBuffer.Length);
            int bytesRead = await _stream.ReadAsync(memory).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                ThrowHelper.ThrowConnectionClosedByPeer();
            }
            receiveBuffer.Advance(bytesRead);
        }

        int CopyBuffer(ReadOnlySequence<byte> src, Memory<byte> dst)
        {
            Span<byte> span = dst.Span;
            src.CopyTo(span);
            span = span.Slice(0, (int)src.Length);
            if (!span.EndsWith((ReadOnlySpan<byte>)new byte[] { (byte)'\r' }))
            {
                throw new DBusConnectFailedException("Authentication messages from server must end with '\\r\\n'.");
            }
            if (span.Length == 1)
            {
                throw new DBusConnectFailedException("Received empty authentication message from server.");
            }
            return span.Length - 1;
        }
    }

    public bool TrySendMessage(MessageBuffer message)
        => _messageWriter.TryWrite(message);

    public void Close(Exception closeReason) => CloseCore(closeReason);

    private Exception CloseCore(Exception closeReason)
    {
        Exception? previous = Interlocked.CompareExchange(ref _completionException, closeReason, null);
        if (previous is null)
        {
            _socket?.Dispose();
            _stream.Dispose();
            _messageWriter.Complete();
        }
        return previous ?? closeReason;
    }
}

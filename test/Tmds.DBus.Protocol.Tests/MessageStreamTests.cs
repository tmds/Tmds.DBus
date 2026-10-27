using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Tmds.DBus.Protocol.Tests
{
    public class MessageStreamTests
    {
        // A single recvmsg can return the bytes of a message without handles followed by a message
        // with handles, together with the handles of the second message.
        [InlineData(0)]
        [InlineData(64 * 1024)] // The second message spans several reads; its handles arrive with its first bytes.
        [Theory]
        public async Task HandlesAreNotDiscardedByPrecedingMessageInSameRead(int arrayLength)
        {
            using var pair = await StreamPair.CreateAsync();
            // Queue both messages before the stream starts reading.
            await pair.SendAsync(CreateMessage(1, "NoHandle"), fdCount: 0);
            await pair.SendAsync(CreateMessage(2, "WithHandle", unixFds: 1, arrayLength), fdCount: 1);
            pair.StartReceiving();

            Received a = await pair.ReceiveAsync();
            Assert.Null(a.CloseReason);
            Assert.Equal("NoHandle", a.Member);

            Received b = await pair.ReceiveAsync();
            Assert.Null(b.CloseReason);
            Assert.Equal("WithHandle", b.Member);
            Assert.True(b.HandleIsValid);
        }

        [Fact]
        public async Task MoreHandlesThanUnixFdsCloseTheConnection()
        {
            using var pair = await StreamPair.CreateAsync();
            // The first message carries a handle it does not declare, the second declares a handle but carries none.
            await pair.SendAsync(CreateMessage(1, "NoHandle"), fdCount: 1);
            await pair.SendAsync(CreateMessage(2, "WithHandle", unixFds: 1), fdCount: 0);
            pair.StartReceiving();

            Received a = await pair.ReceiveAsync();
            Assert.Equal("NoHandle", a.Member);

            Received b = await pair.ReceiveAsync();
            AssertMoreHandlesThanUnixFds(b);
        }

        [Fact]
        public async Task MoreHandlesThanUnixFdsInMessageSpanningReadsCloseTheConnection()
        {
            using var pair = await StreamPair.CreateAsync();
            // The first message spans several reads and carries one handle more than it declares.
            await pair.SendAsync(CreateMessage(1, "WithHandle", unixFds: 1, arrayLength: 64 * 1024), fdCount: 2);
            await pair.SendAsync(CreateMessage(2, "WithHandle", unixFds: 1), fdCount: 0);
            pair.StartReceiving();

            Received a = await pair.ReceiveAsync();
            AssertMoreHandlesThanUnixFds(a);
        }

        private static void AssertMoreHandlesThanUnixFds(Received received)
        {
            DBusReadException exception = Assert.IsType<DBusReadException>(received.CloseReason);
            Assert.Equal("Received more handles than UNIX_FDS.", exception.Message);
        }

        private static MessageBuffer CreateMessage(uint serial, string member, int unixFds = 0, int arrayLength = 0)
        {
            Assert.InRange(unixFds, 0, 1);
            string signature = (unixFds == 1 ? "h" : "") + (arrayLength > 0 ? "ay" : "");
            using var writer = new MessageWriter(MessageBufferPool.Shared, serial);
            writer.WriteMethodCallHeader(path: "/test", member: member, signature: signature.Length > 0 ? signature : null);
            if (unixFds == 1)
            {
                writer.WriteHandle(File.OpenHandle("/dev/null"));
            }
            if (arrayLength > 0)
            {
                writer.WriteArray(new byte[arrayLength]);
            }
            return writer.CreateMessage();
        }

        private record struct Received(Exception? CloseReason, string? Member = null, bool HandleIsValid = false);

        private sealed class StreamPair : IDisposable
        {
            private readonly string _directory;
            private readonly MessageStream _stream;
            private readonly Socket _peer;
            private readonly Channel<Received> _received = Channel.CreateUnbounded<Received>();

            private StreamPair(string directory, MessageStream stream, Socket peer)
            {
                _directory = directory;
                _stream = stream;
                _peer = peer;
            }

            public static async Task<StreamPair> CreateAsync()
            {
                string directory = Directory.CreateTempSubdirectory().FullName;
                string path = Path.Combine(directory, "socket");
                using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                listener.Bind(new UnixDomainSocketEndPoint(path));
                listener.Listen();

                var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                await client.ConnectAsync(new UnixDomainSocketEndPoint(path));
                Socket peer = await listener.AcceptAsync();

                var stream = new MessageStream(client);
                Task auth = stream.DoClientAuthAsync(Guid.Empty, userId: null, supportsFdPassing: true).AsTask();
                await AcceptAuthAsync(peer);
                await auth;

                return new StreamPair(directory, stream, peer);
            }

            private static async Task AcceptAuthAsync(Socket peer)
            {
                Assert.Equal("\0", await ReadAsync(peer, untilNewLine: false));
                while (true)
                {
                    string line = await ReadAsync(peer, untilNewLine: true);
                    string? response = line switch
                    {
                        _ when line.StartsWith("AUTH ") => "OK 0123456789abcdef0123456789abcdef\r\n",
                        "NEGOTIATE_UNIX_FD\r\n" => "AGREE_UNIX_FD\r\n",
                        "BEGIN\r\n" => null,
                        _ => throw new InvalidOperationException($"Unexpected auth line: {line}")
                    };
                    if (response is null)
                    {
                        return;
                    }
                    await peer.SendAsync(System.Text.Encoding.ASCII.GetBytes(response));
                }

                static async Task<string> ReadAsync(Socket peer, bool untilNewLine)
                {
                    var sb = new System.Text.StringBuilder();
                    byte[] b = new byte[1];
                    do
                    {
                        if (await peer.ReceiveAsync(b) == 0)
                        {
                            throw new IOException("Connection closed by peer");
                        }
                        sb.Append((char)b[0]);
                    } while (untilNewLine && b[0] != '\n');
                    return sb.ToString();
                }
            }

            // Sends the message bytes with a single sendmsg that carries fdCount handles.
            public async Task SendAsync(MessageBuffer message, int fdCount)
            {
                UnixFdCollection? handles = null;
                try
                {
                    if (fdCount > 0)
                    {
                        handles = new UnixFdCollection(isRawHandleCollection: false);
                        for (int i = 0; i < fdCount; i++)
                        {
                            handles.AddHandle(File.OpenHandle("/dev/null"));
                        }
                        handles.RefHandles();
                    }
                    byte[] data = message.AsReadOnlySequence().ToArray();
                    await _peer.SendAsync(data, handles);
                }
                finally
                {
                    handles?.Dispose();
                    message.ReturnToPool();
                }
            }

            public void StartReceiving()
            {
                _stream.ReceiveMessages(static (Exception? closeReason, Message? message, Channel<Received> received) =>
                {
                    if (closeReason is not null)
                    {
                        received.Writer.TryWrite(new Received(closeReason));
                        return;
                    }
                    bool handleIsValid = false;
                    if (message!.UnixFdCount > 0)
                    {
                        using var handle = message.GetBodyReader().ReadHandle<SafeFileHandle>();
                        handleIsValid = handle is { IsInvalid: false, IsClosed: false };
                    }
                    received.Writer.TryWrite(new Received(null, message.MemberAsString, handleIsValid));
                    message.DecrementRef();
                }, _received);
            }

            public async Task<Received> ReceiveAsync()
                => await _received.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

            public void Dispose()
            {
                _stream.Close(new ObjectDisposedException(nameof(StreamPair)));
                _peer.Dispose();
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}

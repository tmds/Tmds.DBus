namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents a received D-Bus message.
/// </summary>
public sealed class Message
{
    private const int HeaderFieldsLengthOffset = 12;

    private readonly MessagePool _pool;
    private readonly Sequence<byte> _data;

    private UnixFdCollection? _handles;
    private ReadOnlySequence<byte> _body;
    private int _refCount = 1;

    /// <summary>
    /// Returns whether the message uses big-endian byte order.
    /// </summary>
    public bool IsBigEndian { get; private set; }
    /// <summary>
    /// Gets the serial number of the message.
    /// </summary>
    public uint Serial { get; private set; }
    /// <summary>
    /// Gets the message flags.
    /// </summary>
    public MessageFlags MessageFlags { get; private set; }
    /// <summary>
    /// Gets the message type.
    /// </summary>
    public MessageType MessageType { get; private set; }

    /// <summary>
    /// Gets the serial number of the message this is a reply to.
    /// </summary>
    public uint? ReplySerial { get; private set; }
    /// <summary>
    /// Gets the number of Unix file descriptors associated with the message.
    /// </summary>
    public int UnixFdCount { get; private set; }

    private HeaderBuffer _path;
    private HeaderBuffer _interface;
    private HeaderBuffer _member;
    private HeaderBuffer _errorName;
    private HeaderBuffer _destination;
    private HeaderBuffer _sender;
    private HeaderBuffer _signature;

    /// <summary>
    /// Gets the object path as a string.
    /// </summary>
    public string? PathAsString => _path.ToString();
    /// <summary>
    /// Gets the interface name as a string.
    /// </summary>
    public string? InterfaceAsString => _interface.ToString();
    /// <summary>
    /// Gets the member name as a string.
    /// </summary>
    public string? MemberAsString => _member.ToString();
    /// <summary>
    /// Gets the error name as a string.
    /// </summary>
    public string? ErrorNameAsString => _errorName.ToString();
    /// <summary>
    /// Gets the destination bus name as a string.
    /// </summary>
    public string? DestinationAsString => _destination.ToString();
    /// <summary>
    /// Gets the sender bus name as a string.
    /// </summary>
    public string? SenderAsString => _sender.ToString();
    /// <summary>
    /// Gets the signature as a string.
    /// </summary>
    public string SignatureAsString
        => _signature.ToString() ?? ""; // Omitting the header is the same as an empty signature.

    /// <summary>
    /// Gets the object path as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> Path => _path.Span;
    /// <summary>
    /// Gets the interface name as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> Interface => _interface.Span;
    /// <summary>
    /// Gets the member name as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> Member => _member.Span;
    /// <summary>
    /// Gets the error name as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> ErrorName => _errorName.Span;
    /// <summary>
    /// Gets the destination bus name as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> Destination => _destination.Span;
    /// <summary>
    /// Gets the sender bus name as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> Sender => _sender.Span;
    /// <summary>
    /// Gets the signature as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> Signature => _signature.Span;

    /// <summary>
    /// Gets a value indicating whether the Path header field is present.
    /// </summary>
    public bool PathIsSet => _path.IsSet;
    /// <summary>
    /// Gets a value indicating whether the Interface header field is present.
    /// </summary>
    public bool InterfaceIsSet => _interface.IsSet;
    /// <summary>
    /// Gets a value indicating whether the Member header field is present.
    /// </summary>
    public bool MemberIsSet => _member.IsSet;
    /// <summary>
    /// Gets a value indicating whether the ErrorName header field is present.
    /// </summary>
    public bool ErrorNameIsSet => _errorName.IsSet;
    /// <summary>
    /// Gets a value indicating whether the Destination header field is present.
    /// </summary>
    public bool DestinationIsSet => _destination.IsSet;
    /// <summary>
    /// Gets a value indicating whether the Sender header field is present.
    /// </summary>
    public bool SenderIsSet => _sender.IsSet;
    /// <summary>
    /// Gets a value indicating whether the Signature header field is present.
    /// </summary>
    public bool SignatureIsSet => _signature.IsSet;

    struct HeaderBuffer
    {
        private byte[] _buffer;
        private int _length;
        private string? _string;

        public Span<byte> Span => new Span<byte>(_buffer, 0, Math.Max(_length, 0));

        public void Set(ReadOnlySpan<byte> data)
        {
            _string = null;
            if (_buffer is null || data.Length > _buffer.Length)
            {
                _buffer = new byte[data.Length];
            }
            data.CopyTo(_buffer);
            _length = data.Length;
        }

        public void Clear()
        {
            _length = -1;
            _string = null;
        }

        public override string? ToString()
        {
            return _length == -1 ? null : _string ??= Encoding.UTF8.GetString(Span);
        }

        public bool IsSet => _length != -1;
    }

    /// <summary>
    /// Gets a <see cref="Reader"/> for reading the message body.
    /// </summary>
    public Reader GetBodyReader() => new Reader(IsBigEndian, _body, _handles, UnixFdCount);

    internal Message(MessagePool messagePool, Sequence<byte> sequence)
    {
        _pool = messagePool;
        _data = sequence;
        ClearHeaders();
    }

    internal void IncrementRef()
    {
        Debug.Assert(_refCount > 0);
        Interlocked.Increment(ref _refCount);
    }

    internal void DecrementRef()
    {
        Debug.Assert(_refCount > 0);
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            _refCount = 1;
            ReturnToPool();
        }
    }

    internal void ReturnToPool()
    {
        Debug.Assert(_refCount == 1);
        _data.Reset();
        ClearHeaders();
        _handles?.Dispose();
        _handles = null;
        _pool.Return(this);
    }

    private void ClearHeaders()
    {
        ReplySerial = null;
        UnixFdCount = 0;

        _path.Clear();
        _interface.Clear();
        _member.Clear();
        _errorName.Clear();
        _destination.Clear();
        _sender.Clear();
        _signature.Clear();
    }

    internal static Message? TryReadMessage(MessagePool messagePool, ref ReadOnlySequence<byte> sequence, UnixFdCollection? handles = null, bool isMonitor = false)
    {
        SequenceReader<byte> seqReader = new(sequence);
        if (!seqReader.TryRead(out byte endianness) ||
            !seqReader.TryRead(out byte msgType) ||
            !seqReader.TryRead(out byte flags) ||
            !seqReader.TryRead(out byte version))
        {
            return null;
        }

        if (version != 1)
        {
            throw new NotSupportedException();
        }

        bool isBigEndian = endianness == 'B';

        if (!TryReadUInt32(ref seqReader, isBigEndian, out uint bodyLength) ||
            !TryReadUInt32(ref seqReader, isBigEndian, out uint serial) ||
            !TryReadUInt32(ref seqReader, isBigEndian, out uint headerFieldLength))
        {
            return null;
        }

        headerFieldLength = (uint)ProtocolConstants.Align((int)headerFieldLength, ProtocolConstants.StructAlignment);

        long totalLength = seqReader.Consumed + headerFieldLength + bodyLength;

        if (sequence.Length < totalLength)
        {
            return null;
        }

        // Copy data so it has a lifetime independent of the source sequence.
        var message = messagePool.Rent();
        Sequence<byte> dst = message._data;
        do
        {
            ReadOnlySpan<byte> srcSpan = sequence.First.Span;
            int length = (int)Math.Min(totalLength, srcSpan.Length);
            Span<byte> dstSpan = dst.GetSpan(0);
            length = Math.Min(length, dstSpan.Length);
            srcSpan.Slice(0, length).CopyTo(dstSpan);
            dst.Advance(length);
            sequence = sequence.Slice(length);
            totalLength -= length;
        } while (totalLength > 0);

        message.IsBigEndian = isBigEndian;
        message.Serial = serial;
        message.MessageType = (MessageType)msgType;
        message.MessageFlags = (MessageFlags)flags;
        message.ParseHeader(handles, isMonitor);

        return message;

        static bool TryReadUInt32(ref SequenceReader<byte> seqReader, bool isBigEndian, out uint value)
        {
            int v;
            bool rv = (isBigEndian && seqReader.TryReadBigEndian(out v) || seqReader.TryReadLittleEndian(out v));
            value = (uint)v;
            return rv;
        }
    }

    private void ParseHeader(UnixFdCollection? handles, bool isMonitor)
    {
        var reader = new Reader(IsBigEndian, _data.AsReadOnlySequence);
        reader.Advance(HeaderFieldsLengthOffset);

        ArrayEnd headersEnd = reader.ReadArrayStart(ProtocolConstants.StructAlignment);
        while (reader.HasNext(headersEnd))
        {
            MessageHeader hdrType = (MessageHeader)reader.ReadByte();
            ReadOnlySpan<byte> sig = reader.ReadSignatureAsSpan();
            switch (hdrType)
            {
                case MessageHeader.Path:
                    _path.Set(reader.ReadObjectPathAsSpan());
                    break;
                case MessageHeader.Interface:
                    _interface.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Member:
                    _member.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.ErrorName:
                    _errorName.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.ReplySerial:
                    ReplySerial = reader.ReadUInt32();
                    break;
                case MessageHeader.Destination:
                    _destination.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Sender:
                    _sender.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Signature:
                    _signature.Set(reader.ReadSignatureAsSpan());
                    break;
                case MessageHeader.UnixFds:
                    UnixFdCount = (int)reader.ReadUInt32();
                    if (UnixFdCount > 0 && !isMonitor)
                    {
                        if (handles is null || UnixFdCount > handles.Count)
                        {
                            // Throw DBusReadException just as we would when trying to read one of these handles which is out of range.
                            throw new DBusReadException("Received less handles than UNIX_FDS.");
                        }
                        if (_handles is null)
                        {
                            _handles = new(handles.IsRawHandleCollection);
                        }
                        handles.MoveTo(_handles, UnixFdCount);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        reader.AlignStruct();

        _body = reader.UnreadSequence;
    }
}
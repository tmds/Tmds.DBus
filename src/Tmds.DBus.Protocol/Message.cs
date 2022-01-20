namespace Tmds.DBus.Protocol;

public readonly ref struct Message
{
    private const int HeaderFieldsLengthOffset = 12;

    private readonly bool _isBigEndian;
    private readonly UnixFdCollection? _handles;
    private readonly ReadOnlySequence<byte> _body;

    public MessageType MessageType { get; }
    public MessageFlags MessageFlags { get; }
    public uint Serial { get; }

    // Header Fields
    public Utf8Span Path { get; }
    public Utf8Span Interface { get; }
    public Utf8Span Member { get; }
    public Utf8Span ErrorName { get; }
    public uint? ReplySerial { get; }
    public Utf8Span Destination { get; }
    public Utf8Span Sender { get; }
    public Utf8Span Signature { get; }
    public int UnixFdCount { get; }

    public Reader GetBodyReader() => new Reader(_isBigEndian, _body, _handles);

    private Message(ReadOnlySequence<byte> sequence, bool isBigEndian, MessageType type, MessageFlags flags, uint serial, UnixFdCollection? handles)
    {
        _isBigEndian = isBigEndian;
        MessageType = type;
        MessageFlags = flags;
        Serial = serial;
        _handles = handles;

        Path = default;
        Interface = default;
        Member = default;
        ErrorName = default;
        ReplySerial = default;
        Destination = default;
        Sender = default;
        Signature = default;
        UnixFdCount = default;

        var reader = new Reader(isBigEndian, sequence, null);
        reader.Advance(HeaderFieldsLengthOffset);

        ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(headersEnd))
        {
            MessageHeader hdrType = (MessageHeader)reader.ReadByte();
            ReadOnlySpan<byte> sig = reader.ReadSignature();
            switch (hdrType)
            {
                case MessageHeader.Path:
                    Path = reader.ReadObjectPath();
                    break;
                case MessageHeader.Interface:
                    Interface = reader.ReadString();
                    break;
                case MessageHeader.Member:
                    Member = reader.ReadString();
                    break;
                case MessageHeader.ErrorName:
                    ErrorName = reader.ReadString();
                    break;
                case MessageHeader.ReplySerial:
                    ReplySerial = reader.ReadUInt32();
                    break;
                case MessageHeader.Destination:
                    Destination = reader.ReadString();
                    break;
                case MessageHeader.Sender:
                    Sender = reader.ReadString();
                    break;
                case MessageHeader.Signature:
                    Signature = reader.ReadSignature();
                    break;
                case MessageHeader.UnixFds:
                    UnixFdCount = (int)reader.ReadUInt32();
                    // TODO: throw if handles contains less.
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        reader.AlignStruct();

        _body = reader.UnreadSequence;
    }

    internal static bool TryReadMessage(ref ReadOnlySequence<byte> sequence, out Message message, UnixFdCollection? handles = null)
    {
        message = default;

        SequenceReader<byte> seqReader = new(sequence);
        if (!seqReader.TryRead(out byte endianness) ||
            !seqReader.TryRead(out byte msgType) ||
            !seqReader.TryRead(out byte flags) ||
            !seqReader.TryRead(out byte version))
        {
            return false;
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
            return false;
        }

        headerFieldLength = (uint)ProtocolConstants.Align((int)headerFieldLength, DBusType.Struct);

        long totalLength = seqReader.Consumed + headerFieldLength + bodyLength;

        if (sequence.Length < totalLength)
        {
            return false;
        }

        message = new Message(sequence.Slice(0, totalLength),
                                    isBigEndian,
                                    (MessageType)msgType,
                                    (MessageFlags)flags,
                                    serial,
                                    handles);

        sequence = sequence.Slice(totalLength);

        return true;

        static bool TryReadUInt32(ref SequenceReader<byte> seqReader, bool isBigEndian, out uint value)
        {
            int v;
            bool rv = (isBigEndian && seqReader.TryReadBigEndian(out v) || seqReader.TryReadLittleEndian(out v));
            value = (uint)v;
            return rv;
        }
    }
}
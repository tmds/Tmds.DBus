namespace Tmds.DBus.Protocol;

public readonly ref struct Message
{
    internal readonly struct MessageData
    {
        public readonly bool IsBigEndian;
        public readonly ReadOnlySequence<byte> Sequence;
        public readonly UnixFdCollection? Handles;
        public readonly MessageType MessageType;
        public readonly MessageFlags MessageFlags;
        public readonly uint Serial;

        public MessageData(ReadOnlySequence<byte> sequence, bool isBigEndian, MessageType msgType, MessageFlags flags, uint serial, UnixFdCollection? handles) : this()
        {
            Sequence = sequence;
            IsBigEndian = isBigEndian;
            MessageType = msgType;
            MessageFlags = flags;
            Serial = serial;
            Handles = handles;
        }
    }

    internal readonly MessageData Data;
    private readonly ReadOnlySequence<byte> _body;

    private const int HeaderFieldsLengthOffset = 12;

    public MessageType MessageType => Data.MessageType;
    public MessageFlags MessageFlags => Data.MessageFlags;
    public uint Serial => Data.Serial;

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

    public Reader GetBodyReader() => new Reader(Data.IsBigEndian, _body, Data.Handles);

    internal Message(in MessageData data)
    {
        Data = data;

        Path = default;
        Interface = default;
        Member = default;
        ErrorName = default;
        ReplySerial = default;
        Destination = default;
        Sender = default;
        Signature = default;
        UnixFdCount = default;

        var reader = new Reader(Data.IsBigEndian, Data.Sequence, null);
        reader.Advance(HeaderFieldsLengthOffset);

        ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(headersEnd))
        {
            MessageHeader hdrType = (MessageHeader)reader.ReadByte();
            ReadOnlySpan<byte> sig = reader.ReadSignature();
            switch (hdrType)
            {
                case MessageHeader.Path:
                    Path = reader.ReadObjectPathAsSpan();
                    break;
                case MessageHeader.Interface:
                    Interface = reader.ReadStringAsSpan();
                    break;
                case MessageHeader.Member:
                    Member = reader.ReadStringAsSpan();
                    break;
                case MessageHeader.ErrorName:
                    ErrorName = reader.ReadStringAsSpan();
                    break;
                case MessageHeader.ReplySerial:
                    ReplySerial = reader.ReadUInt32();
                    break;
                case MessageHeader.Destination:
                    Destination = reader.ReadStringAsSpan();
                    break;
                case MessageHeader.Sender:
                    Sender = reader.ReadStringAsSpan();
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

        message = new Message(new MessageData(sequence.Slice(0, totalLength),
                                              isBigEndian,
                                              (MessageType)msgType,
                                              (MessageFlags)flags,
                                              serial,
                                              handles));

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
namespace Tmds.DBus.Protocol;

public sealed class Message
{
    private const int HeaderFieldsLengthOffset = 12;

    private readonly MessagePool _pool;
    private readonly Sequence<byte> _data;

    private UnixFdCollection? _handles;
    private ReadOnlySequence<byte> _body;

    public bool IsBigEndian { get; private set; }
    public uint Serial { get; private set; }
    public MessageFlags MessageFlags { get; private set; }
    public MessageType MessageType { get; private set; }

    public uint? ReplySerial { get; private set; }
    public int UnixFdCount { get; private set; }
    public string? Path { get; private set; }
    public string? Interface  { get; private set; }
    public string? Member  { get; private set; }
    public string? ErrorName  { get; private set; }
    public string? Destination  { get; private set; }
    public string? Sender  { get; private set; }
    public string? Signature  { get; private set; }

    public Reader GetBodyReader() => new Reader(IsBigEndian, _body, _handles, UnixFdCount);

    internal Message(MessagePool messagePool, Sequence<byte> sequence)
    {
        _pool = messagePool;
        _data = sequence;
    }

    internal void ReturnToPool()
    {
        _data.Reset();
        ReplySerial = null;
        UnixFdCount = 0;
        Path = null;
        Interface = null;
        Member = null;
        ErrorName = null;
        Destination = null;
        Sender = null;
        Signature = null;
        _handles?.DisposeHandles();

        _pool.Return(this);
    }

    internal static Message? TryReadMessage(MessagePool messagePool, ref ReadOnlySequence<byte> sequence, UnixFdCollection? handles = null)
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

        headerFieldLength = (uint)ProtocolConstants.Align((int)headerFieldLength, DBusType.Struct);

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
        message.ParseHeader(handles);

        return message;

        static bool TryReadUInt32(ref SequenceReader<byte> seqReader, bool isBigEndian, out uint value)
        {
            int v;
            bool rv = (isBigEndian && seqReader.TryReadBigEndian(out v) || seqReader.TryReadLittleEndian(out v));
            value = (uint)v;
            return rv;
        }
    }

    private void ParseHeader(UnixFdCollection? handles)
    {
        var reader = new Reader(IsBigEndian, _data.AsReadOnlySequence);
        reader.Advance(HeaderFieldsLengthOffset);

        ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(headersEnd))
        {
            MessageHeader hdrType = (MessageHeader)reader.ReadByte();
            ReadOnlySpan<byte> sig = reader.ReadSignature();
            switch (hdrType)
            {
                case MessageHeader.Path:
                    Path = reader.ReadObjectPathAsString();
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
                    Signature = reader.ReadSignatureAsString();
                    break;
                case MessageHeader.UnixFds:
                    UnixFdCount = (int)reader.ReadUInt32();
                    if (UnixFdCount > 0)
                    {
                        if (handles is null || UnixFdCount > handles.Count)
                        {
                            throw new ProtocolException("Received less handles than UNIX_FDS.");
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
using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    private delegate object ValueReader(ref Reader reader);

    private readonly bool _isBigEndian;
    private UnixFdCollection? _handles;
    private SequenceReader<byte> _reader;

    internal ReadOnlySequence<byte> UnreadSequence => _reader.Sequence.Slice(_reader.Position);

    internal void Advance(long count) => _reader.Advance(count);

    internal Reader(bool isBigEndian, ReadOnlySequence<byte> sequence, UnixFdCollection? handles)
    {
        _reader = new(sequence);

        _isBigEndian = isBigEndian;
        _handles = handles;
    }

    public void AlignStruct() => AlignReader(DBusType.Struct);

    private void AlignReader(DBusType type)
    {
        long pad = ProtocolConstants.GetPadding((int)_reader.Consumed, type);
        if (pad != 0)
        {
            _reader.Advance(pad);
        }
    }

    public ArrayEnd ReadArrayStart(DBusType elementType)
    {
        uint arrayLength = ReadUInt32();
        AlignReader(elementType);
        int endOfArray = (int)(_reader.Consumed + arrayLength);
        return new ArrayEnd(elementType, endOfArray);
    }

    public bool HasNext(ArrayEnd iterator)
    {
        int consumed = (int)_reader.Consumed;
        int nextElement = ProtocolConstants.Align(consumed, iterator.Type);
        if (nextElement >= iterator.EndOfArray)
        {
            return false;
        }
        int advance = nextElement - consumed;
        if (advance != 0)
        {
            _reader.Advance(advance);
        }
        return true;
    }
}

public ref struct ArrayEnd
{
    internal readonly DBusType Type;
    internal readonly int EndOfArray;

    internal ArrayEnd(DBusType type, int endOfArray)
    {
        Type = type;
        EndOfArray = endOfArray;
    }
}
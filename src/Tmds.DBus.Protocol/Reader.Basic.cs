using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public byte ReadByte()
    {
        if (!_reader.TryRead(out byte b))
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return b;
    }

    public bool ReadBool()
    {
        return ReadInt32() != 0;
    }

    public UInt16 ReadUInt16()
        => (UInt16)ReadInt16();

    public Int16 ReadInt16()
    {
        AlignReader(DBusType.Int16);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out Int16 rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    public uint ReadUInt32()
        => (uint)ReadInt32();

    public int ReadInt32()
    {
        AlignReader(DBusType.Int32);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out int rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    public UInt64 ReadUInt64()
        => (UInt64)ReadInt64();

    public Int64 ReadInt64()
    {
        AlignReader(DBusType.Int64);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out Int64 rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    public unsafe double ReadDouble()
    {
        double value;
        *(Int64*)&value = ReadInt64();
        return value;
    }

    public Utf8Span ReadSignature()
    {
        int length = ReadByte();
        return ReadSpan(length);
    }

    public Utf8Span ReadObjectPath() => ReadSpan();

    public Utf8Span ReadString() => ReadSpan();

    private ReadOnlySpan<byte> ReadSpan()
    {
        int length = (int)ReadUInt32();
        return ReadSpan(length);
    }

    private ReadOnlySpan<byte> ReadSpan(int length)
    {
        var span = _reader.UnreadSpan;
        if (span.Length <= length)
        {
            _reader.Advance(length + 1);
            return span.Slice(length);
        }
        else
        {
            var buffer = new byte[length];
            if (!_reader.TryCopyTo(buffer))
            {
                ThrowHelper.ThrowIndexOutOfRange();
            }
            _reader.Advance(length + 1);
            return new ReadOnlySpan<byte>(buffer);
        }
    }
}

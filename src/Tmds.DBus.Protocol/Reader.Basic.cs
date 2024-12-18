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

    public ushort ReadUInt16()
        => (ushort)ReadInt16();

    public short ReadInt16()
    {
        AlignReader(alignment: 2);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out short rv) : _reader.TryReadLittleEndian(out rv);
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
        AlignReader(alignment: 4);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out int rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    public ulong ReadUInt64()
        => (ulong)ReadInt64();

    public long ReadInt64()
    {
        AlignReader(alignment: 8);
        bool dataRead = _isBigEndian ? _reader.TryReadBigEndian(out long rv) : _reader.TryReadLittleEndian(out rv);
        if (!dataRead)
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return rv;
    }

    public unsafe double ReadDouble()
    {
        double value;
        *(long*)&value = ReadInt64();
        return value;
    }

    public Signature ReadSignature()
        => new Signature(ReadSignatureAsSpan());

    public ReadOnlySpan<byte> ReadSignatureAsSpan()
    {
        int length = ReadByte();
        return ReadSpan(length);
    }

    public void ReadSignature(string expected)
    {
        ReadOnlySpan<byte> signature = ReadSignatureAsSpan();
        if (signature.Length != expected.Length)
        {
            ThrowHelper.ThrowUnexpectedSignature(signature, expected);
        }
        for (int i = 0; i < signature.Length; i++)
        {
            if (signature[i] != expected[i])
            {
                ThrowHelper.ThrowUnexpectedSignature(signature, expected);
            }
        }
    }

    public void ReadSignature(ReadOnlySpan<byte> expected)
    {
        ReadOnlySpan<byte> signature = ReadSignatureAsSpan();
        if (!signature.SequenceEqual(expected))
        {
            ThrowHelper.ThrowUnexpectedSignature(signature, Encoding.UTF8.GetString(expected));
        }
    }

    public ReadOnlySpan<byte> ReadObjectPathAsSpan() => ReadSpan();

    public ObjectPath ReadObjectPath() => new ObjectPath(ReadString());

    public string ReadObjectPathAsString() => ReadString();

    public ReadOnlySpan<byte> ReadStringAsSpan() => ReadSpan();

    public string ReadString() => Encoding.UTF8.GetString(ReadSpan());

    public string ReadSignatureAsString() => Encoding.UTF8.GetString(ReadSignatureAsSpan());

    private ReadOnlySpan<byte> ReadSpan()
    {
        int length = (int)ReadUInt32();
        return ReadSpan(length);
    }

    private ReadOnlySpan<byte> ReadSpan(int length)
    {
        var span = _reader.UnreadSpan;
        if (span.Length >= length)
        {
            _reader.Advance(length + 1);
            return span.Slice(0, length);
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

    private bool ReverseEndianness
        => BitConverter.IsLittleEndian != !_isBigEndian;
}

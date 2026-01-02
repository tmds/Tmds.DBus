namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    /// <summary>
    /// Reads a byte value.
    /// </summary>
    public byte ReadByte()
    {
        if (!_reader.TryRead(out byte b))
        {
            ThrowHelper.ThrowIndexOutOfRange();
        }
        return b;
    }

    /// <summary>
    /// Reads a boolean value.
    /// </summary>
    public bool ReadBool()
    {
        return ReadInt32() != 0;
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer.
    /// </summary>
    public ushort ReadUInt16()
        => (ushort)ReadInt16();

    /// <summary>
    /// Reads a signed 16-bit integer.
    /// </summary>
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

    /// <summary>
    /// Reads an unsigned 32-bit integer.
    /// </summary>
    public uint ReadUInt32()
        => (uint)ReadInt32();

    /// <summary>
    /// Reads a signed 32-bit integer.
    /// </summary>
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

    /// <summary>
    /// Reads an unsigned 64-bit integer.
    /// </summary>
    public ulong ReadUInt64()
        => (ulong)ReadInt64();

    /// <summary>
    /// Reads a signed 64-bit integer.
    /// </summary>
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

    /// <summary>
    /// Reads a double.
    /// </summary>
    public unsafe double ReadDouble()
    {
        double value;
        *(long*)&value = ReadInt64();
        return value;
    }

    /// <summary>
    /// Reads a signature.
    /// </summary>
    public Signature ReadSignature()
        => new Signature(ReadSignatureAsSpan());

    /// <summary>
    /// Reads a signature as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> ReadSignatureAsSpan()
    {
        int length = ReadByte();
        return ReadSpan(length);
    }

    /// <summary>
    /// Reads and validates a signature matches the expected value.
    /// </summary>
    /// <param name="expected">The expected signature string.</param>
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

    /// <summary>
    /// Reads and validates a signature matches the expected value.
    /// </summary>
    /// <param name="expected">The expected signature.</param>
    public void ReadSignature(ReadOnlySpan<byte> expected)
    {
        ReadOnlySpan<byte> signature = ReadSignatureAsSpan();
        if (!signature.SequenceEqual(expected))
        {
            ThrowHelper.ThrowUnexpectedSignature(signature, Encoding.UTF8.GetString(expected));
        }
    }

    /// <summary>
    /// Reads an object path as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> ReadObjectPathAsSpan() => ReadSpan();

    /// <summary>
    /// Reads an object path.
    /// </summary>
    public ObjectPath ReadObjectPath() => new ObjectPath(ReadString());

    /// <summary>
    /// Reads an object path as a string.
    /// </summary>
    public string ReadObjectPathAsString() => ReadString();

    /// <summary>
    /// Reads a string as a byte span.
    /// </summary>
    public ReadOnlySpan<byte> ReadStringAsSpan() => ReadSpan();

    /// <summary>
    /// Reads a string.
    /// </summary>
    public string ReadString() => Encoding.UTF8.GetString(ReadSpan());

    /// <summary>
    /// Reads a signature as a string.
    /// </summary>
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

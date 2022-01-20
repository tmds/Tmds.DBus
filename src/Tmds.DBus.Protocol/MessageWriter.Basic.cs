namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteBool(bool value) => WriteUInt32(value ? 1u : 0u);

    public void WriteByte(byte value) => WritePrimitiveCore<Int16>(value, DBusType.Byte);

    public void WriteInt16(Int16 value) => WritePrimitiveCore<Int16>(value, DBusType.Int16);

    public void WriteUInt16(UInt16 value) => WritePrimitiveCore<UInt16>(value, DBusType.UInt16);

    public void WriteInt32(Int32 value) => WritePrimitiveCore<Int32>(value, DBusType.Int32);

    public void WriteUInt32(UInt32 value) => WritePrimitiveCore<UInt32>(value, DBusType.UInt32);

    public void WriteInt64(Int64 value) => WritePrimitiveCore<Int64>(value, DBusType.Int64);

    public void WriteUInt64(UInt64 value) => WritePrimitiveCore<UInt64>(value, DBusType.UInt64);

    public void WriteDouble(double value) => WritePrimitiveCore<double>(value, DBusType.Double);

    public void WriteString(Utf8Span value) => WriteStringCore(value);

    public void WriteString(string value) => WriteStringCore(value);

    public void WriteSignature(Utf8Span value)
    {
        ReadOnlySpan<byte> span = value;
        int length = span.Length;
        WriteByte((byte)length);
        var dst = GetSpan(length);
        span.CopyTo(dst);
        Advance(length);
        WriteByte((byte)0);
    }

    public void WriteSignature(string s)
    {
        Span<byte> lengthSpan = GetSpan(1);
        Advance(1);
        int bytesWritten = (int)Encoding.UTF8.GetBytes(s.AsSpan(), Writer);
        lengthSpan[0] = (byte)bytesWritten;
        _offset += bytesWritten;
        WriteByte(0);
    }

    public void WriteObjectPath(Utf8Span value) => WriteStringCore(value);

    public void WriteObjectPath(string value) => WriteStringCore(value);

    public void WriteVariantBool(bool value)
    {
        WriteSignature(ProtocolConstants.BooleanSignature);
        WriteBool(value);
    }

    public void WriteVariantByte(byte value)
    {
        WriteSignature(ProtocolConstants.ByteSignature);
        WriteByte(value);
    }

    public void WriteVariantInt16(Int16 value)
    {
        WriteSignature(ProtocolConstants.Int16Signature);
        WriteInt16(value);
    }

    public void WriteVariantUInt16(UInt16 value)
    {
        WriteSignature(ProtocolConstants.UInt16Signature);
        WriteUInt16(value);
    }

    public void WriteVariantInt32(Int32 value)
    {
        WriteSignature(ProtocolConstants.Int32Signature);
        WriteInt32(value);
    }

    public void WriteVariantUInt32(UInt32 value)
    {
        WriteSignature(ProtocolConstants.UInt32Signature);
        WriteUInt32(value);
    }

    public void WriteVariantInt64(Int64 value)
    {
        WriteSignature(ProtocolConstants.Int64Signature);
        WriteInt64(value);
    }

    public void WriteVariantUInt64(UInt64 value)
    {
        WriteSignature(ProtocolConstants.UInt64Signature);
        WriteUInt64(value);
    }

    public void WriteVariantDouble(double value)
    {
        WriteSignature(ProtocolConstants.DoubleSignature);
        WriteDouble(value);
    }

    public void WriteVariantString(Utf8Span value)
    {
        WriteSignature(ProtocolConstants.StringSignature);
        WriteString(value);
    }

    public void WriteVariantSignature(Utf8Span value)
    {
        WriteSignature(ProtocolConstants.SignatureSignature);
        WriteSignature(value);
    }

    public void WriteVariantObjectPath(Utf8Span value)
    {
        WriteSignature(ProtocolConstants.ObjectPathSignature);
        WriteObjectPath(value);
    }

    public void WriteVariantString(string value)
    {
        WriteSignature(ProtocolConstants.StringSignature);
        WriteString(value);
    }

    public void WriteVariantSignature(string value)
    {
        WriteSignature(ProtocolConstants.SignatureSignature);
        WriteSignature(value);
    }

    public void WriteVariantObjectPath(string value)
    {
        WriteSignature(ProtocolConstants.ObjectPathSignature);
        WriteObjectPath(value);
    }

    private void WriteStringCore(ReadOnlySpan<byte> span)
    {
        int length = span.Length;
        WriteUInt32((uint)length);
        var dst = GetSpan(length);
        span.CopyTo(dst);
        Advance(length);
        WriteByte((byte)0);
    }

    private void WriteStringCore(string s)
    {
        WritePadding(DBusType.UInt32);
        Span<byte> lengthSpan = GetSpan(4);
        Advance(4);
        int bytesWritten = (int)Encoding.UTF8.GetBytes(s.AsSpan(), Writer);
        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(lengthSpan), (uint)bytesWritten);
        _offset += bytesWritten;
        WriteByte(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePrimitiveCore<T>(T value, DBusType type)
    {
        WritePadding(type);
        int length = ProtocolConstants.GetFixedTypeLength(type);
        var span = GetSpan(length);
        Unsafe.WriteUnaligned<T>(ref MemoryMarshal.GetReference(span), value);
        Advance(length);
    }
}

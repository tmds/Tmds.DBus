namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    private const int MaxSizeHint = 4096;

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public void WriteBool(bool value) => WriteUInt32(value ? 1u : 0u);

    /// <summary>
    /// Writes a byte value.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public void WriteByte(byte value) => WritePrimitiveCore<byte>(value);

    /// <summary>
    /// Writes a signed 16-bit integer.
    /// </summary>
    /// <param name="value">The signed 16-bit integer.</param>
    public void WriteInt16(short value) => WritePrimitiveCore<Int16>(value);

    /// <summary>
    /// Writes an unsigned 16-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 16-bit integer.</param>
    public void WriteUInt16(ushort value) => WritePrimitiveCore<UInt16>(value);

    /// <summary>
    /// Writes a signed 32-bit integer.
    /// </summary>
    /// <param name="value">The signed 32-bit integer.</param>
    public void WriteInt32(int value) => WritePrimitiveCore<Int32>(value);

    /// <summary>
    /// Writes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 32-bit integer.</param>
    public void WriteUInt32(uint value) => WritePrimitiveCore<UInt32>(value);

    /// <summary>
    /// Writes a signed 64-bit integer.
    /// </summary>
    /// <param name="value">The signed 64-bit integer.</param>
    public void WriteInt64(long value) => WritePrimitiveCore<Int64>(value);

    /// <summary>
    /// Writes an unsigned 64-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 64-bit integer.</param>
    public void WriteUInt64(ulong value) => WritePrimitiveCore<UInt64>(value);

    /// <summary>
    /// Writes a double.
    /// </summary>
    /// <param name="value">The double.</param>
    public void WriteDouble(double value) => WritePrimitiveCore<double>(value);

    /// <summary>
    /// Writes a string.
    /// </summary>
    /// <param name="value">The string.</param>
    public void WriteString(scoped ReadOnlySpan<byte> value) => WriteStringCore(value);

    /// <summary>
    /// Writes a string.
    /// </summary>
    /// <param name="value">The string.</param>
    public void WriteString(string value) => WriteStringCore(value);

    /// <summary>
    /// Writes a signature.
    /// </summary>
    /// <param name="value">The signature.</param>
    public void WriteSignature(Signature value)
        => WriteSignature(value.Data);

    /// <summary>
    /// Writes a signature.
    /// </summary>
    /// <param name="value">The signature.</param>
    public void WriteSignature(scoped ReadOnlySpan<byte> value)
    {
        int length = value.Length;
        WriteByte((byte)length);
        var dst = GetSpan(length);
        value.CopyTo(dst);
        Advance(length);
        WriteByte((byte)0);
    }

    /// <summary>
    /// Writes a signature.
    /// </summary>
    /// <param name="s">The signature string.</param>
    public void WriteSignature(string s)
    {
        Span<byte> lengthSpan = GetSpan(1);
        Advance(1);
        int bytesWritten = WriteRaw(s);
        lengthSpan[0] = (byte)bytesWritten;
        WriteByte(0);
    }

    /// <summary>
    /// Writes an object path.
    /// </summary>
    /// <param name="value">The object path.</param>
    public void WriteObjectPath(scoped ReadOnlySpan<byte> value) => WriteStringCore(value);

    /// <summary>
    /// Writes an object path.
    /// </summary>
    /// <param name="value">The object path string.</param>
    public void WriteObjectPath(string value) => WriteStringCore(value);

    /// <summary>
    /// Writes an object path.
    /// </summary>
    /// <param name="value">The object path.</param>
    public void WriteObjectPath(ObjectPath value) => WriteStringCore(value.ToString());

    /// <summary>
    /// Writes a variant-wrapped boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public void WriteVariantBool(bool value)
    {
        WriteSignature(Signature.Boolean);
        WriteBool(value);
    }

    /// <summary>
    /// Writes a variant-wrapped byte.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public void WriteVariantByte(byte value)
    {
        WriteSignature(Signature.Byte);
        WriteByte(value);
    }

    /// <summary>
    /// Writes a variant-wrapped signed 16-bit integer.
    /// </summary>
    /// <param name="value">The signed 16-bit integer.</param>
    public void WriteVariantInt16(short value)
    {
        WriteSignature(Signature.Int16);
        WriteInt16(value);
    }

    /// <summary>
    /// Writes a variant-wrapped unsigned 16-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 16-bit integer.</param>
    public void WriteVariantUInt16(ushort value)
    {
        WriteSignature(Signature.UInt16);
        WriteUInt16(value);
    }

    /// <summary>
    /// Writes a variant-wrapped signed 32-bit integer.
    /// </summary>
    /// <param name="value">The signed 32-bit integer.</param>
    public void WriteVariantInt32(int value)
    {
        WriteSignature(Signature.Int32);
        WriteInt32(value);
    }

    /// <summary>
    /// Writes a variant-wrapped unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 32-bit integer.</param>
    public void WriteVariantUInt32(uint value)
    {
        WriteSignature(Signature.UInt32);
        WriteUInt32(value);
    }

    /// <summary>
    /// Writes a variant-wrapped signed 64-bit integer.
    /// </summary>
    /// <param name="value">The signed 64-bit integer.</param>
    public void WriteVariantInt64(long value)
    {
        WriteSignature(Signature.Int64);
        WriteInt64(value);
    }

    /// <summary>
    /// Writes a variant-wrapped unsigned 64-bit integer.
    /// </summary>
    /// <param name="value">The unsigned 64-bit integer.</param>
    public void WriteVariantUInt64(ulong value)
    {
        WriteSignature(Signature.UInt64);
        WriteUInt64(value);
    }

    /// <summary>
    /// Writes a variant-wrapped double.
    /// </summary>
    /// <param name="value">The double.</param>
    public void WriteVariantDouble(double value)
    {
        WriteSignature(Signature.Double);
        WriteDouble(value);
    }

    /// <summary>
    /// Writes a variant-wrapped string value.
    /// </summary>
    /// <param name="value">The string.</param>
    public void WriteVariantString(scoped ReadOnlySpan<byte> value)
    {
        WriteSignature(Signature.String);
        WriteString(value);
    }

    /// <summary>
    /// Writes a variant-wrapped signature value.
    /// </summary>
    /// <param name="value">The signature.</param>
    public void WriteVariantSignature(scoped ReadOnlySpan<byte> value)
    {
        WriteSignature(Signature.Sig);
        WriteSignature(value);
    }

    /// <summary>
    /// Writes a variant-wrapped object path value.
    /// </summary>
    /// <param name="value">The object path.</param>
    public void WriteVariantObjectPath(scoped ReadOnlySpan<byte> value)
    {
        WriteSignature(Signature.ObjectPath);
        WriteObjectPath(value);
    }

    /// <summary>
    /// Writes a variant-wrapped string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    public void WriteVariantString(string value)
    {
        WriteSignature(Signature.String);
        WriteString(value);
    }

    /// <summary>
    /// Writes a variant-wrapped signature value.
    /// </summary>
    /// <param name="value">The signature string.</param>
    public void WriteVariantSignature(string value)
    {
        WriteSignature(Signature.Sig);
        WriteSignature(value);
    }

    /// <summary>
    /// Writes a variant-wrapped object path value.
    /// </summary>
    /// <param name="value">The object path string.</param>
    public void WriteVariantObjectPath(string value)
    {
        WriteSignature(Signature.ObjectPath);
        WriteObjectPath(value);
    }

    private void WriteStringCore(scoped ReadOnlySpan<byte> span)
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
        WritePadding(ProtocolConstants.UInt32Alignment);
        Span<byte> lengthSpan = GetSpan(4);
        Advance(4);
        int bytesWritten = WriteRaw(s);
        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(lengthSpan), (uint)bytesWritten);
        WriteByte(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePrimitiveCore<T>(T value)
    {
        int length = Marshal.SizeOf<T>();
        WritePadding(length);
        var span = GetSpan(length);
        Unsafe.WriteUnaligned<T>(ref MemoryMarshal.GetReference(span), value);
        Advance(length);
    }

    private int WriteRaw(scoped ReadOnlySpan<byte> data)
    {
        int totalLength = data.Length;
        if (totalLength <= MaxSizeHint)
        {
            var dst = GetSpan(totalLength);
            data.CopyTo(dst);
            Advance(totalLength);
            return totalLength;
        }
        else
        {
            while (!data.IsEmpty)
            {
                var dst = GetSpan(1);
                int length = Math.Min(data.Length, dst.Length);
                data.Slice(0, length).CopyTo(dst);
                Advance(length);
                data = data.Slice(length);
            }
            return totalLength;
        }
    }

    private int WriteRaw(string data)
    {
        const int MaxUtf8BytesPerChar = 3;

        if (data.Length <= MaxSizeHint / MaxUtf8BytesPerChar)
        {
            ReadOnlySpan<char> chars = data.AsSpan();
            int byteCount = Encoding.UTF8.GetByteCount(chars);
            var dst = GetSpan(byteCount);
            byteCount = Encoding.UTF8.GetBytes(data.AsSpan(), dst);
            Advance(byteCount);
            return byteCount;
        }
        else
        {
            ReadOnlySpan<char> chars = data.AsSpan();
            Encoder encoder = Encoding.UTF8.GetEncoder();
            int totalLength = 0;
            do
            {
                Debug.Assert(!chars.IsEmpty);

                var dst = GetSpan(MaxUtf8BytesPerChar);
                encoder.Convert(chars, dst, flush: true, out int charsUsed, out int bytesUsed, out bool completed);

                Advance(bytesUsed);
                totalLength += bytesUsed;

                if (completed)
                {
                    return totalLength;
                }

                chars = chars.Slice(charsUsed);
            } while (true);
        }
    }
}

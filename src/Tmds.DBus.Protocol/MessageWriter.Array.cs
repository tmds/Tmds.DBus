namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteArray(byte[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<byte> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<byte> value)
        => WriteArrayOfT(value);

    public void WriteArray(short[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<short> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<short> value)
        => WriteArrayOfT(value);

    public void WriteArray(ushort[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<ushort> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<ushort> value)
        => WriteArrayOfT(value);

    public void WriteArray(int[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<int> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<int> value)
        => WriteArrayOfT(value);

    public void WriteArray(uint[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<uint> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<uint> value)
        => WriteArrayOfT(value);

    public void WriteArray(long[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<long> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<long> value)
        => WriteArrayOfT(value);

    public void WriteArray(ulong[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<ulong> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<ulong> value)
        => WriteArrayOfT(value);

    public void WriteArray(double[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<double> value)
        => WriteArrayOfNumeric(value);

    public void WriteArray(IEnumerable<double> value)
        => WriteArrayOfT(value);

    public void WriteArray(string[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<string> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<string> value)
        => WriteArrayOfT(value);

    public void WriteArray(Signature[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<Signature> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<Signature> value)
        => WriteArrayOfT(value);

    public void WriteArray(ObjectPath[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<ObjectPath> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<ObjectPath> value)
        => WriteArrayOfT(value);

    public void WriteArray(Variant[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<Variant> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<Variant> value)
        => WriteArrayOfT(value);

    public void WriteArray(SafeHandle[] value)
        => WriteArray(value.AsSpan());

    public void WriteArray(ReadOnlySpan<SafeHandle> value)
        => WriteArrayOfT(value);

    public void WriteArray(IEnumerable<SafeHandle> value)
        => WriteArrayOfT(value);

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteArray)]
    public void WriteArray<T>(IEnumerable<T> value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    [RequiresUnreferencedCode(Strings.UseNonGenericWriteArray)]
    public void WriteArray<T>(T[] value)
        where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            WriteArray((byte[])(object)value);
        }
        else if (typeof(T) == typeof(short))
        {
            WriteArray((short[])(object)value);
        }
        else if (typeof(T) == typeof(ushort))
        {
            WriteArray((ushort[])(object)value);
        }
        else if (typeof(T) == typeof(int))
        {
            WriteArray((int[])(object)value);
        }
        else if (typeof(T) == typeof(uint))
        {
            WriteArray((uint[])(object)value);
        }
        else if (typeof(T) == typeof(long))
        {
            WriteArray((long[])(object)value);
        }
        else if (typeof(T) == typeof(ulong))
        {
            WriteArray((ulong[])(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            WriteArray((double[])(object)value);
        }
        else
        {
            WriteArrayOfT<T>(value.AsSpan());
        }
    }

    private unsafe void WriteArrayOfNumeric<T>(ReadOnlySpan<T> value) where T : unmanaged
    {
        WriteInt32(value.Length * sizeof(T));
        if (sizeof(T) > 4)
        {
            WritePadding(sizeof(T));
        }
        WriteRaw(MemoryMarshal.AsBytes(value));
    }

    private void WriteArrayOfT<T>(ReadOnlySpan<T> value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    private void WriteArrayOfT<T>(IEnumerable<T> value)
        where T : notnull
    {
        if (value is T[] array)
        {
            WriteArrayOfT<T>(array.AsSpan());
            return;
        }
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    private static void WriteArraySignature<T>(ref MessageWriter writer) where T : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Array<T>>(buffer));
    }
}

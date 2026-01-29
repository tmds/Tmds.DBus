namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    /// <summary>
    /// Reads an array of bytes.
    /// </summary>
    public byte[] ReadArrayOfByte()
        => ReadArrayOfNumeric<byte>();

    /// <summary>
    /// Reads an array of booleans.
    /// </summary>
    public bool[] ReadArrayOfBool()
        => ReadArrayOfT<bool>();

    /// <summary>
    /// Reads an array of signed 16-bit integers.
    /// </summary>
    public short[] ReadArrayOfInt16()
        => ReadArrayOfNumeric<short>();

    /// <summary>
    /// Reads an array of unsigned 16-bit integers.
    /// </summary>
    public ushort[] ReadArrayOfUInt16()
        => ReadArrayOfNumeric<ushort>();

    /// <summary>
    /// Reads an array of signed 32-bit integers.
    /// </summary>
    public int[] ReadArrayOfInt32()
        => ReadArrayOfNumeric<int>();

    /// <summary>
    /// Reads an array of unsigned 32-bit integers.
    /// </summary>
    public uint[] ReadArrayOfUInt32()
        => ReadArrayOfNumeric<uint>();

    /// <summary>
    /// Reads an array of signed 64-bit integers.
    /// </summary>
    public long[] ReadArrayOfInt64()
        => ReadArrayOfNumeric<long>();

    /// <summary>
    /// Reads an array of unsigned 64-bit integers.
    /// </summary>
    public ulong[] ReadArrayOfUInt64()
        => ReadArrayOfNumeric<ulong>();

    /// <summary>
    /// Reads an array of double values.
    /// </summary>
    public double[] ReadArrayOfDouble()
        => ReadArrayOfNumeric<double>();

    /// <summary>
    /// Reads an array of strings.
    /// </summary>
    public string[] ReadArrayOfString()
        => ReadArrayOfT<string>();

    /// <summary>
    /// Reads an array of object paths.
    /// </summary>
    public ObjectPath[] ReadArrayOfObjectPath()
        => ReadArrayOfT<ObjectPath>();

    /// <summary>
    /// Reads an array of signatures.
    /// </summary>
    public Signature[] ReadArrayOfSignature()
        => ReadArrayOfT<Signature>();

    /// <summary>
    /// Reads an array of variant values.
    /// </summary>
    public VariantValue[] ReadArrayOfVariantValue()
        => ReadArrayOfT<VariantValue>();

    /// <summary>
    /// Reads an array of Unix file descriptor handles.
    /// </summary>
    /// <typeparam name="T">The SafeHandle type to read.</typeparam>
    public T[] ReadArrayOfHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle, new()
        => ReadArrayOfT<T>();

    private T[] ReadArrayOfT<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        List<T> items = new();
        ArrayEnd arrayEnd = ReadArrayStart(TypeModel.GetTypeAlignment<T>());
        while (HasNext(arrayEnd))
        {
            items.Add(Read<T>());
        }
        return items.ToArray();
    }

    private unsafe T[] ReadArrayOfNumeric<T>() where T : unmanaged
    {
        int length = ReadInt32();
        if (sizeof(T) > 4)
        {
            AlignReader(sizeof(T));
        }
        T[] array = new T[length / sizeof(T)];
        bool dataRead = _reader.TryCopyTo(MemoryMarshal.AsBytes(array.AsSpan()));
        if (!dataRead)
        {
            ThrowHelper.ThrowReaderUnexpectedEndOfData();
        }
        _reader.Advance(sizeof(T) * array.Length); // TryCopyTo succeeded, data is available
        if (sizeof(T) > 1 && ReverseEndianness)
        {
#if NET8_0_OR_GREATER
            if (sizeof(T) == 2)
            {
                var span = MemoryMarshal.Cast<T, short>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (sizeof(T) == 4)
            {
                var span = MemoryMarshal.Cast<T, int>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
            else if (sizeof(T) == 8)
            {
                Span<long> span = MemoryMarshal.Cast<T, long>(array.AsSpan());
                BinaryPrimitives.ReverseEndianness(span, span);
            }
#else
            Span<T> span = array.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (sizeof(T) == 2)
                {
                    span[i] = (T)(object)BinaryPrimitives.ReverseEndianness((short)(object)span[i]);
                }
                else if (sizeof(T) == 4)
                {
                    span[i] = (T)(object)BinaryPrimitives.ReverseEndianness((int)(object)span[i]);
                }
                else if (typeof(T) == typeof(double))
                {
                    span[i] = (T)(object)ReverseDoubleEndianness((double)(object)span[i]);
                }
                else if (sizeof(T) == 8)
                {
                    span[i] = (T)(object)BinaryPrimitives.ReverseEndianness((long)(object)span[i]);
                }
            }
#endif
        }
        return array;

#if !NET8_0_OR_GREATER
        static double ReverseDoubleEndianness(double d)
        {
            long l = *(long*)&d;
            l = BinaryPrimitives.ReverseEndianness(l);
            return *(double*)&d;
        }
#endif
    }
}

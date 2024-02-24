namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
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

    public void WriteArray<T>(T[] value)
        where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            WriteArrayOfNumeric<byte>((byte[])(object)value);
        }
        else if (typeof(T) == typeof(short))
        {
            WriteArrayOfNumeric<short>((short[])(object)value);
        }
        else if (typeof(T) == typeof(ushort))
        {
            WriteArrayOfNumeric<ushort>((ushort[])(object)value);
        }
        else if (typeof(T) == typeof(int))
        {
            WriteArrayOfNumeric<int>((int[])(object)value);
        }
        else if (typeof(T) == typeof(uint))
        {
            WriteArrayOfNumeric<uint>((uint[])(object)value);
        }
        else if (typeof(T) == typeof(long))
        {
            WriteArrayOfNumeric<long>((long[])(object)value);
        }
        else if (typeof(T) == typeof(ulong))
        {
            WriteArrayOfNumeric<ulong>((ulong[])(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            WriteArrayOfNumeric<double>((double[])(object)value);
        }
        else
        {
            WriteArrayOfT<T>(value);
        }
    }

    private unsafe void WriteArrayOfNumeric<T>(T[] value) where T : unmanaged
    {
        WriteInt32(value.Length * sizeof(T));
        if (sizeof(T) > 4)
        {
            WritePadding(sizeof(T));
        }
        WriteRaw(MemoryMarshal.AsBytes(value.AsSpan()));
    }

    private void WriteArrayOfT<T>(T[] value)
        where T : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(TypeModel.GetTypeAlignment<T>());
        foreach (var item in value)
        {
            Write<T>(item);
        }
        WriteArrayEnd(arrayStart);
    }

    sealed class ArrayTypeWriter<T> : ITypeWriter<IEnumerable<T>>
        where T : notnull
    {
        public void Write(ref MessageWriter writer, IEnumerable<T> value)
        {
            writer.WriteArray(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteArraySignature<T>(ref writer);
            writer.WriteArray((IEnumerable<T>)value);
        }
    }

    public static void AddArrayTypeWriter<T>()
        where T : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(IEnumerable<T>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new ArrayTypeWriter<T>());
            }
        }
    }

    private ITypeWriter CreateArrayTypeWriter(Type elementType)
    {
        Type writerType = typeof(ArrayTypeWriter<>).MakeGenericType(new[] { elementType });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteArraySignature<T>(ref MessageWriter writer) where T : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Array<T>>(buffer));
    }
}

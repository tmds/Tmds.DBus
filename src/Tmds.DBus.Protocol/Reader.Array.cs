namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public T[] ReadArray<T>()
    {
        if (typeof(T) == typeof(byte))
        {
            return (T[])(object)ReadArrayOfNumeric<byte>();
        }
        else if (typeof(T) == typeof(short))
        {
            return (T[])(object)ReadArrayOfNumeric<short>();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T[])(object)ReadArrayOfNumeric<ushort>();
        }
        else if (typeof(T) == typeof(int))
        {
            return (T[])(object)ReadArrayOfNumeric<int>();
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T[])(object)ReadArrayOfNumeric<uint>();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T[])(object)ReadArrayOfNumeric<long>();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T[])(object)ReadArrayOfNumeric<ulong>();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T[])(object)ReadArrayOfNumeric<double>();
        }
        else
        {
            return ReadArrayOfT<T>();
        }
    }

    private T[] ReadArrayOfT<T>()
    {
        List<T> items = new();
        ArrayEnd headersEnd = ReadArrayStart(TypeModel.GetTypeAlignment<T>());
        while (HasNext(headersEnd))
        {
            items.Add(Read<T>());
        }
        return items.ToArray();
    }

    public unsafe T[] ReadArrayOfNumeric<T>() where T : unmanaged
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
            ThrowHelper.ThrowIndexOutOfRange();
        }
        _reader.Advance(sizeof(T) * array.Length);
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

        static double ReverseDoubleEndianness(double d)
        {
            long l = *(long*)&d;
            l = BinaryPrimitives.ReverseEndianness(l);
            return *(double*)&d;
        }
    }

    private KeyValuePair<TKey, TValue>[] ReadKeyValueArray<TKey, TValue>()
    {
        List<KeyValuePair<TKey, TValue>> items = new();
        ArrayEnd headersEnd = ReadArrayStart(DBusType.Struct);
        while (HasNext(headersEnd))
        {
            TKey key = Read<TKey>();
            TValue value = Read<TValue>();
            items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }
        return items.ToArray();
    }
}

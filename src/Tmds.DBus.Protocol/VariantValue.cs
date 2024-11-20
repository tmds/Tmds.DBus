namespace Tmds.DBus.Protocol;

public readonly struct VariantValue : IEquatable<VariantValue>
{
    private readonly object? _o;
    private readonly long    _l;

    // The top four bytes are used as follows (highest to lowest):
    // - Nesting as a byte.
    // - Type as a VariantValueType
    // - ArrayItemType/DictionaryKeyType as a VariantValueType
    // - DictionaryValueTypeShift as a VariantValueType
    // For Structs, the lowest of these 4 bytes are used to track if the corresponding field is a Variant.
    private const int NestingShift = 8 * 7;
    private const int TypeShift = 8 * 6;
    private const int ArrayItemTypeShift = 8 * 5;
    private const int DictionaryKeyTypeShift = 8 * 5;
    private const int DictionaryValueTypeShift = 8 * 4;
    private const long StripMetadataMask = 0xffffffffL;
    private const long MetadataMask = ~StripMetadataMask;
    private const long StructVariantMask = 0xffffL;
    private const int StructVariantMaskShift = 8 * 4;
    private const int MaxStructFields = 2 * 8;

    private bool UnsafeIsStructFieldVariant(int index)
        => (_l & (1L << (index + StructVariantMaskShift))) != 0;

    private static long GetTypeMetadata(VariantValueType type, byte nesting)
        => ((long)nesting << NestingShift) | ((long)type << TypeShift);

    private static long GetStructMetadata(long variantMask, int count, byte nesting)
        => GetTypeMetadata(VariantValueType.Struct, nesting) | (variantMask << StructVariantMaskShift) | (long)count;

    private static long GetArrayTypeMetadata(VariantValueType itemType, int count, byte nesting)
        => ((long)nesting << NestingShift) | ((long)VariantValueType.Array << TypeShift) | ((long)itemType << ArrayItemTypeShift) | ((long)count);

    private static long GetDictionaryTypeMetadata(VariantValueType keyType, VariantValueType valueType, int count, byte nesting)
        => ((long)nesting << NestingShift) | ((long)VariantValueType.Dictionary << TypeShift) | ((long)keyType << DictionaryKeyTypeShift) | ((long)valueType << DictionaryValueTypeShift) | ((long)count);

    // For most types, we store the type information in the highest bytes of the long.
    // Except for some types (Int64, UInt64, Double) for which we store the value allocation free
    // in the long, and use the object field to store the type.
    readonly struct TypeData
    {
        public TypeData(long l)
        {
            L = l;
        }

        public readonly long L { get; }
    }
    private const long L_Int64 = ((long)VariantValueType.Int64) << TypeShift;
    private const long L_UInt64 = ((long)VariantValueType.UInt64) << TypeShift;
    private const long L_Double = ((long)VariantValueType.Double) << TypeShift;
    private const long VariantOfInt64 = (1L << NestingShift) | L_Int64;
    private const long VariantOfUInt64 = (1L << NestingShift) | L_UInt64;
    private const long VariantOfDouble = (1L << NestingShift) | L_Double;
    private static readonly object Int64TypeDescriptor = new TypeData(L_Int64);
    private static readonly object UInt64TypeDescriptor = new TypeData(L_UInt64);
    private static readonly object DoubleTypeDescriptor = new TypeData(L_Double);
    private static readonly object VariantOfInt64TypeDescriptor = new TypeData(VariantOfInt64);
    private static readonly object VariantOfUInt64TypeDescriptor = new TypeData(VariantOfUInt64);
    private static readonly object VariantOfDoubleTypeDescriptor = new TypeData(VariantOfDouble);

    private static long UpdateNesting(long l, int change)
    {
        long nesting = l >> NestingShift;
        Debug.Assert(nesting >= 0);
        nesting = nesting + change;
        Debug.Assert(nesting >= 0);
        l = (nesting << NestingShift) | (l & ~(0xffL << NestingShift));
        return l;
    }

    private static object UpdateNesting(TypeData metadata, int change)
    {
        long l = metadata.L;
        l = UpdateNesting(l, change);
        return l switch
        {
            L_Int64 => Int64TypeDescriptor,
            L_UInt64 => UInt64TypeDescriptor,
            L_Double => DoubleTypeDescriptor,
            VariantOfInt64 => VariantOfInt64TypeDescriptor,
            VariantOfUInt64 => VariantOfUInt64TypeDescriptor,
            VariantOfDouble => VariantOfDoubleTypeDescriptor,
            _ => new TypeData(l)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object? GetSignatureObject(int count, ReadOnlySpan<byte> signature)
    {
        // When there are items, the signature is determined based on the first.
        // If the signature length is 1, we determine it on the metadata.
        if (count > 0 || signature.Length == 1)
        {
            return null;
        }
        return signature.ToArray();
    }

    public VariantValueType Type
        => DetermineType();

    private VariantValue(long l, object? o)
    {
        _l = l;
        _o = o;
    }
    internal VariantValue(byte value, byte nesting)
    {
        _l = value | GetTypeMetadata(VariantValueType.Byte, nesting);
        _o = null;
    }
    internal VariantValue(bool value, byte nesting)
    {
        _l = (value ? 1L : 0) | GetTypeMetadata(VariantValueType.Bool, nesting);
        _o = null;
    }
    internal VariantValue(short value, byte nesting)
    {
        _l = (ushort)value | GetTypeMetadata(VariantValueType.Int16, nesting);
        _o = null;
    }
    internal VariantValue(ushort value, byte nesting)
    {
        _l = value | GetTypeMetadata(VariantValueType.UInt16, nesting);
        _o = null;
    }
    internal VariantValue(int value, byte nesting)
    {
        _l = (uint)value | GetTypeMetadata(VariantValueType.Int32, nesting);
        _o = null;
    }
    internal VariantValue(uint value, byte nesting)
    {
        _l = value | GetTypeMetadata(VariantValueType.UInt32, nesting);
        _o = null;
    }
    internal VariantValue(long value, byte nesting)
    {
        _l = value;
        _o = nesting switch
        {
            0 => Int64TypeDescriptor,
            1 => VariantOfInt64TypeDescriptor,
            _ => new TypeData(GetTypeMetadata(VariantValueType.Int64, nesting))
        };
    }
    internal VariantValue(ulong value, byte nesting)
    {
        _l = (long)value;
        _o = nesting switch
        {
            0 => UInt64TypeDescriptor,
            1 => VariantOfUInt64TypeDescriptor,
            _ => new TypeData(GetTypeMetadata(VariantValueType.UInt64, nesting))
        };
    }
    internal unsafe VariantValue(double value, byte nesting)
    {
        _l = *(long*)&value;
        _o = nesting switch
        {
            0 => DoubleTypeDescriptor,
            1 => VariantOfDoubleTypeDescriptor,
            _ => new TypeData(GetTypeMetadata(VariantValueType.Double, nesting))
        };
    }
    internal VariantValue(string value, byte nesting)
    {
        ThrowIfNull(value, nameof(value));
        _l = GetTypeMetadata(VariantValueType.String, nesting);
        _o = value;
    }
    internal VariantValue(ObjectPath value, byte nesting)
    {
        value.ThrowIfEmpty();
        _l = GetTypeMetadata(VariantValueType.ObjectPath, nesting);
        _o = value.ToString();
    }
    internal VariantValue(Signature value, byte nesting)
    {
        _l = GetTypeMetadata(VariantValueType.Signature, nesting);
        _o = value.Data;;
    }
    // Array
    internal VariantValue(VariantValueType itemType, object? itemSignature, VariantValue[] items, byte nesting)
    {
        int count = items.Length;
        _l = GetArrayTypeMetadata(itemType, count, nesting);
        _o = count == 0 ? itemSignature : items;
    }
    internal VariantValue(IList<VariantValue> items, byte nesting)
    {
        Debug.Assert(items is VariantValue[] or List<VariantValue>);
        _l = GetArrayTypeMetadata(VariantValueType.Variant, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<string> items, byte nesting)
    {
        Debug.Assert(items is string[] or List<string>);
        _l = GetArrayTypeMetadata(VariantValueType.String, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<ObjectPath> items, byte nesting)
    {
        Debug.Assert(items is ObjectPath[] or List<ObjectPath>);
        _l = GetArrayTypeMetadata(VariantValueType.ObjectPath, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<byte> items, byte nesting)
    {
        Debug.Assert(items is byte[] or List<byte>);
        _l = GetArrayTypeMetadata(VariantValueType.Byte, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<short> items, byte nesting)
    {
        Debug.Assert(items is short[] or List<short>);
        _l = GetArrayTypeMetadata(VariantValueType.Int16, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<ushort> items, byte nesting)
    {
        Debug.Assert(items is ushort[] or List<ushort>);
        _l = GetArrayTypeMetadata(VariantValueType.UInt16, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<int> items, byte nesting)
    {
        Debug.Assert(items is int[] or List<int>);
        _l = GetArrayTypeMetadata(VariantValueType.Int32, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<uint> items, byte nesting)
    {
        Debug.Assert(items is uint[] or List<uint>);
        _l = GetArrayTypeMetadata(VariantValueType.UInt32, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<long> items, byte nesting)
    {
        Debug.Assert(items is long[] or List<long>);
        _l = GetArrayTypeMetadata(VariantValueType.Int64, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<ulong> items, byte nesting)
    {
        Debug.Assert(items is ulong[] or List<ulong>);
        _l = GetArrayTypeMetadata(VariantValueType.UInt64, items.Count, nesting);
        _o = items;
    }
    internal VariantValue(IList<double> items, byte nesting)
    {
        Debug.Assert(items is double[] or List<double>);
        _l = GetArrayTypeMetadata(VariantValueType.Double, items.Count, nesting);
        _o = items;
    }
    // Dictionary
    internal VariantValue(VariantValueType keyType, VariantValueType valueType, object? valueSignature, KeyValuePair<VariantValue, VariantValue>[] pairs, byte nesting)
    {
        int count = pairs.Length;
        _l = GetDictionaryTypeMetadata(keyType, valueType, count, nesting);
        _o = count == 0 ? valueSignature : pairs;
    }
    // Struct
    internal static VariantValue StructFromStruct(VariantValue[] fields)
        => StructCore(fields, nesting: 0);

    private static VariantValue StructCore(VariantValue[] fields, byte nesting)
    {
        long variantMask = 0;
        for (int i = 0; i < fields.Length; i++)
        {
            VariantValue value = fields[i];
            value.ThrowIfInvalid();

            if (value.Type == VariantValueType.Variant)
            {
                variantMask |= (1L << i);
                fields[i] = value.GetVariantValue();
            }
        }

        return new VariantValue(variantMask, fields, nesting: 0);
    }
    private static void ThrowMaxStructFieldsExceeded()
    {
        throw new NotSupportedException($"Struct types {VariantValue.MaxStructFields}+ fields are not supported.");
    }
    internal VariantValue(long variantMask, VariantValue[] fields, byte nesting)
    {
        if (fields.Length > MaxStructFields)
        {
            ThrowMaxStructFieldsExceeded();
        }
        _l = GetStructMetadata(variantMask, fields.Length, nesting);
        _o = fields;
    }
    // UnixFd
    internal VariantValue(UnixFdCollection? fdCollection, int index) :
        this(fdCollection, index, nesting: 0)
    { }
    internal VariantValue(UnixFdCollection? fdCollection, int index, byte nesting)
    {
        _l = (long)index | GetTypeMetadata(VariantValueType.UnixFd, nesting);
        _o = fdCollection;
    }

    public VariantValue GetVariantValue()
    {
        EnsureTypeIs(VariantValueType.Variant);
        if (_o is TypeData td)
        {
            return new VariantValue(_l, UpdateNesting(td, -1));
        }
        else
        {
            return new VariantValue(UpdateNesting(_l, -1), _o);
        }
    }

    public byte GetByte()
    {
        EnsureTypeIs(VariantValueType.Byte);
        return UnsafeGetByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte UnsafeGetByte()
    {
        return (byte)(_l & StripMetadataMask);
    }

    public bool GetBool()
    {
        EnsureTypeIs(VariantValueType.Bool);
        return UnsafeGetBool();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool UnsafeGetBool()
    {
        return (_l & StripMetadataMask) != 0;
    }

    public short GetInt16()
    {
        EnsureTypeIs(VariantValueType.Int16);
        return UnsafeGetInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private short UnsafeGetInt16()
    {
        return (short)(_l & StripMetadataMask);
    }

    public ushort GetUInt16()
    {
        EnsureTypeIs(VariantValueType.UInt16);
        return UnsafeGetUInt16();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort UnsafeGetUInt16()
    {
        return (ushort)(_l & StripMetadataMask);
    }

    public int GetInt32()
    {
        EnsureTypeIs(VariantValueType.Int32);
        return UnsafeGetInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int UnsafeGetInt32()
    {
        return (int)(_l & StripMetadataMask);
    }

    public uint GetUInt32()
    {
        EnsureTypeIs(VariantValueType.UInt32);
        return UnsafeGetUInt32();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint UnsafeGetUInt32()
    {
        return (uint)(_l & StripMetadataMask);
    }

    public long GetInt64()
    {
        EnsureTypeIs(VariantValueType.Int64);
        return UnsafeGetInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long UnsafeGetInt64()
    {
        return _l;
    }

    public ulong GetUInt64()
    {
        EnsureTypeIs(VariantValueType.UInt64);
        return UnsafeGetUInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong UnsafeGetUInt64()
    {
        return (ulong)(_l);
    }

    public string GetString()
    {
        EnsureTypeIs(VariantValueType.String);
        return UnsafeGetString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string UnsafeGetString()
    {
        return (_o as string)!;
    }

    private ObjectPath UnsafeGetObjectPath()
        => new ObjectPath(UnsafeGetString());

    [Obsolete($"Call {nameof(GetObjectPathAsString)} instead. {nameof(GetObjectPath)} will be changed to return a {nameof(ObjectPath)} in a future version, which will cause a breaking change.")]
    public string GetObjectPath()
    {
        EnsureTypeIs(VariantValueType.ObjectPath);
        return UnsafeGetString();
    }

    public string GetObjectPathAsString()
    {
        EnsureTypeIs(VariantValueType.ObjectPath);
        return UnsafeGetString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Signature UnsafeGetSignature()
    {
        return new Signature((_o as byte[])!);
    }

    public Signature GetSignature()
    {
        EnsureTypeIs(VariantValueType.Signature);
        return UnsafeGetSignature();
    }

    public double GetDouble()
    {
        EnsureTypeIs(VariantValueType.Double);
        return UnsafeGetDouble();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe double UnsafeGetDouble()
    {
        double value;
        *(long*)&value = _l;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T UnsafeGet<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)UnsafeGetByte();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)UnsafeGetBool();
        }
        else if (typeof(T) == typeof(short))
        {
            return (T)(object)UnsafeGetInt16();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T)(object)UnsafeGetUInt16();
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)UnsafeGetInt32();
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T)(object)UnsafeGetUInt32();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T)(object)UnsafeGetInt64();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T)(object)UnsafeGetUInt64();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)UnsafeGetDouble();
        }
        else if (typeof(T) == typeof(string))
        {
            return (T)(object)UnsafeGetString();
        }
        else if (typeof(T) == typeof(Signature))
        {
            return (T)(object)UnsafeGetSignature();
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return (T)(object)UnsafeGetObjectPath();
        }
        else if (typeof(T) == typeof(VariantValue))
        {
            return (T)(object)this;
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            return (T)(object)UnsafeReadHandle<T>()!;
        }

        ThrowCannotRetrieveAs(Type, typeof(T));
        return default!;
    }

    public Dictionary<TKey, TValue> GetDictionary
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
        >
        ()
        where TKey : notnull
        where TValue : notnull
    {
        EnsureTypeIs(VariantValueType.Dictionary);
        EnsureCanUnsafeGet<TKey>(KeyType);
        EnsureCanUnsafeGet<TValue>(ValueType);

        Dictionary<TKey, TValue> dict = new();
        var pairs = (_o as KeyValuePair<VariantValue, VariantValue>[])!.AsSpan();
        foreach (var pair in pairs)
        {
            dict[pair.Key.UnsafeGet<TKey>()] = pair.Value.UnsafeGet<TValue>();
        }
        return dict;
    }

    private ReadOnlySpan<T> UnsafeArrayAsSpan<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
        where T : notnull
    {
        if (UnsafeCount == 0)
        {
            return System.Array.Empty<T>();
        }

        if (_o is T[] a)
        {
            return a;
        }
        else
        {
            List<T> list = (_o as List<T>)!;
#if NET5_0_OR_GREATER
            return CollectionsMarshal.AsSpan(list);
#else
            return list.ToArray();
#endif
        }
    }

    public T[] GetArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
        where T : notnull
    {
        EnsureTypeIs(VariantValueType.Array);
        VariantValueType itemType = UnsafeDetermineInnerType(ArrayItemTypeShift);
        EnsureCanUnsafeGet<T>(itemType);

        if (UnsafeCount == 0)
        {
            return System.Array.Empty<T>();
        }

        // Return the array by reference when we can.
        // Don't bother to make a copy in case the caller mutates the data and
        // calls GetArray again to retrieve the original data. It's an unlikely scenario.
        if (typeof(T) == typeof(byte)
            || typeof(T) == typeof(short)
            || typeof(T) == typeof(int)
            || typeof(T) == typeof(long)
            || typeof(T) == typeof(ushort)
            || typeof(T) == typeof(uint)
            || typeof(T) == typeof(ulong)
            || typeof(T) == typeof(double)
            || (typeof(T) == typeof(string) && itemType != VariantValueType.ObjectPath)
            || typeof(T) == typeof(ObjectPath))
        {
            return _o is T[] a ? a : (_o as List<T>)!.ToArray();
        }
        else if (typeof(T) == typeof(string) && itemType == VariantValueType.ObjectPath)
        {
            IList<ObjectPath> paths = (_o as IList<ObjectPath>)!;
            return (T[])(object)paths.Select(path => path.ToString()).ToArray();
        }
        else
        {
            var items = (_o as VariantValue[])!.AsSpan();
            T[] array = new T[items.Length];
            int i = 0;
            foreach (var item in items)
            {
                array[i++] = item.UnsafeGet<T>();
            }
            return array;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureCanUnsafeGet<T>(VariantValueType type)
    {
        if (typeof(T) == typeof(byte))
        {
            EnsureTypeIs(type, VariantValueType.Byte);
        }
        else if (typeof(T) == typeof(bool))
        {
            EnsureTypeIs(type, VariantValueType.Bool);
        }
        else if (typeof(T) == typeof(short))
        {
            EnsureTypeIs(type, VariantValueType.Int16);
        }
        else if (typeof(T) == typeof(ushort))
        {
            EnsureTypeIs(type, VariantValueType.UInt16);
        }
        else if (typeof(T) == typeof(int))
        {
            EnsureTypeIs(type, VariantValueType.Int32);
        }
        else if (typeof(T) == typeof(uint))
        {
            EnsureTypeIs(type, VariantValueType.UInt32);
        }
        else if (typeof(T) == typeof(long))
        {
            EnsureTypeIs(type, VariantValueType.Int64);
        }
        else if (typeof(T) == typeof(ulong))
        {
            EnsureTypeIs(type, VariantValueType.UInt64);
        }
        else if (typeof(T) == typeof(double))
        {
            EnsureTypeIs(type, VariantValueType.Double);
        }
        else if (typeof(T) == typeof(string))
        {
            EnsureTypeIs(type, [ VariantValueType.String, VariantValueType.ObjectPath ]);
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            EnsureTypeIs(type, VariantValueType.ObjectPath);
        }
        else if (typeof(T) == typeof(Signature))
        {
            EnsureTypeIs(type, VariantValueType.Signature);
        }
        else if (typeof(T) == typeof(VariantValue))
        { }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            EnsureTypeIs(type, VariantValueType.UnixFd);
        }
        else
        {
            ThrowCannotRetrieveAs(type, typeof(T));
        }
    }

    public T? ReadHandle<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    T>() where T : SafeHandle, new()
    {
        EnsureTypeIs(VariantValueType.UnixFd);
        return UnsafeReadHandle<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T? UnsafeReadHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        var handles = (UnixFdCollection?)_o;
        if (handles is null)
        {
            return default;
        }
        int index = (int)_l;
        return handles.ReadHandleGeneric<T>(index);
    }

    private int UnsafeCount => (int)_l;

    // Use for Array, Struct and Dictionary.
    public int Count
    {
        get
        {
            if (Type is VariantValueType.Array or VariantValueType.Struct or VariantValueType.Dictionary)
            {
                return UnsafeCount;
            }
            return -1;
        }
    }

    // Valid for Array, Struct.
    public VariantValue GetItem(int i)
    {
        VariantValueType type = Type;
        EnsureTypeIs(type, [VariantValueType.Array, VariantValueType.Struct]);

        if (UnsafeCount == 0)
        {
            throw new IndexOutOfRangeException();
        }

        if (type == VariantValueType.Array)
        {
            switch (UnsafeDetermineInnerType(ArrayItemTypeShift))
            {
                case VariantValueType.Byte:
                    return Byte((_o as IList<byte>)![i]);
                case VariantValueType.Int16:
                    return Int16((_o as IList<short>)![i]);
                case VariantValueType.UInt16:
                    return UInt16((_o as IList<ushort>)![i]);
                case VariantValueType.Int32:
                    return Int32((_o as IList<int>)![i]);
                case VariantValueType.UInt32:
                    return UInt32((_o as IList<uint>)![i]);
                case VariantValueType.Int64:
                    return Int64((_o as IList<long>)![i]);
                case VariantValueType.UInt64:
                    return UInt64((_o as IList<ulong>)![i]);
                case VariantValueType.Double:
                    return Double((_o as IList<double>)![i]);
                case VariantValueType.String:
                    return String((_o as IList<string>)![i]);
                case VariantValueType.ObjectPath:
                    return ObjectPath((_o as IList<ObjectPath>)![i]);
            }
        }

        var values = _o as IList<VariantValue>;
        return values![i];
    }

    // Valid for Dictionary.
    public KeyValuePair<VariantValue, VariantValue> GetDictionaryEntry(int i)
    {
        EnsureTypeIs(VariantValueType.Dictionary);

        if (UnsafeCount == 0)
        {
            throw new IndexOutOfRangeException();
        }

        var values = _o as KeyValuePair<VariantValue, VariantValue>[];
        if (_o is null)
        {
            ThrowCannotRetrieveAs(Type, VariantValueType.Dictionary);
        }
        return values![i];
    }

    // implicit conversion to VariantValue for basic D-Bus types (except Unix_FD).
    public static implicit operator VariantValue(byte value)
        => Byte(value);
    public static implicit operator VariantValue(bool value)
        => Bool(value);
    public static implicit operator VariantValue(short value)
        => Int16(value);
    public static implicit operator VariantValue(ushort value)
        => UInt16(value);
    public static implicit operator VariantValue(int value)
        => Int32(value);
    public static implicit operator VariantValue(uint value)
        => UInt32(value);
    public static implicit operator VariantValue(long value)
        => Int64(value);
    public static implicit operator VariantValue(ulong value)
        => UInt64(value);
    public static implicit operator VariantValue(double value)
        => Double(value);
    public static implicit operator VariantValue(string value)
        => String(value);
    public static implicit operator VariantValue(ObjectPath value)
        => ObjectPath(value);
    public static implicit operator VariantValue(Signature value)
        => Signature(value);

    public static VariantValue Byte(byte value) => new VariantValue(value, nesting: 0);
    public static VariantValue Bool(bool value) => new VariantValue(value, nesting: 0);
    public static VariantValue Int16(short value) => new VariantValue(value, nesting: 0);
    public static VariantValue UInt16(ushort value) => new VariantValue(value, nesting: 0);
    public static VariantValue Int32(int value) => new VariantValue(value, nesting: 0);
    public static VariantValue UInt32(uint value) => new VariantValue(value, nesting: 0);
    public static VariantValue Int64(long value) => new VariantValue(value, nesting: 0);
    public static VariantValue UInt64(ulong value) => new VariantValue(value, nesting: 0);
    public static VariantValue Double(double value) => new VariantValue(value, nesting: 0);
    public static VariantValue ObjectPath(ObjectPath value) => new VariantValue(value, nesting: 0);
    public static VariantValue Signature(Signature value) => new VariantValue(value, nesting: 0);
    public static VariantValue String(string value)
    {
        ThrowIfNull(value, nameof(value));
        return new VariantValue(value, nesting: 0);
    }
    public static VariantValue UnixFd(SafeHandle handle)
    {
        ThrowIfNull(handle, nameof(handle));
        var fds = new UnixFdCollection(isRawHandleCollection: false);
        fds.AddHandle(handle);
        return new VariantValue(fds, index: 0);
    }
    public static VariantValue Variant(VariantValue value)
    {
        value.ThrowIfInvalid();

        object? o = value._o;
        long l = value._l;
        if (o is TypeData td)
        {
            return new VariantValue(l, UpdateNesting(td, +1));
        }
        else
        {
            return new VariantValue(UpdateNesting(l, +1), o);
        }
    }
    public static VariantValue Struct(VariantValue item1)
        => StructCore(new[] { item1 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2)
        => StructCore(new[] { item1, item2 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3)
        => StructCore(new[] { item1, item2, item3 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4)
        => StructCore(new[] { item1, item2, item3, item4 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4, VariantValue item5)
        => StructCore(new[] { item1, item2, item3, item4, item5 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4, VariantValue item5, VariantValue item6)
        => StructCore(new[] { item1, item2, item3, item4, item5, item6 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4, VariantValue item5, VariantValue item6, VariantValue item7)
        => StructCore(new[] { item1, item2, item3, item4, item5, item6, item7 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4, VariantValue item5, VariantValue item6, VariantValue item7, VariantValue item8)
        => StructCore(new[] { item1, item2, item3, item4, item5, item6, item7, item8 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4, VariantValue item5, VariantValue item6, VariantValue item7, VariantValue item8, VariantValue item9)
        => StructCore(new[] { item1, item2, item3, item4, item5, item6, item7, item8, item9 }, nesting: 0);
    public static VariantValue Struct(VariantValue item1, VariantValue item2, VariantValue item3, VariantValue item4, VariantValue item5, VariantValue item6, VariantValue item7, VariantValue item8, VariantValue item9, VariantValue item10)
        => StructCore(new[] { item1, item2, item3, item4, item5, item6, item7, item8, item9, item10 }, nesting: 0);

    public static VariantValue Array(byte[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<byte> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(short[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<short> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(ushort[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<ushort> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(int[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<int> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(uint[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<uint> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(long[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<long> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(ulong[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<ulong> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(double[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<double> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(ObjectPath[] items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<ObjectPath> items)
    {
        ThrowIfNull(items, nameof(items));
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(string[] items)
    {
        ThrowIfNull(items, nameof(items));
        if (System.Array.IndexOf(items, null) != -1)
        {
            ThrowArgumentNull(nameof(items));
        }
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<string> items)
    {
        ThrowIfNull(items, nameof(items));
        if (items.Contains(null!))
        {
            ThrowArgumentNull(nameof(items));
        }
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue ArrayOfVariant(VariantValue[] items)
    {
        ThrowIfNull(items, nameof(items));
        foreach (var item in items)
        {
            item.ThrowIfInvalid();
        }
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue ArrayOfVariant(List<VariantValue> items)
    {
        ThrowIfNull(items, nameof(items));
        foreach (var item in items)
        {
            item.ThrowIfInvalid();
        }
        return new VariantValue(items, nesting: 0);
    }
    public static VariantValue Array(List<SafeHandle> items)
        => Array(items.ToArray());
    public static VariantValue Array(SafeHandle[] items)
    {
        ThrowIfNull(items, nameof(items));
        if (System.Array.IndexOf(items, null) != -1)
        {
            ThrowArgumentNull(nameof(items));
        }
        VariantValue[] values = new VariantValue[items.Length];
        var fds = new UnixFdCollection(isRawHandleCollection: false);
        for (int i = 0; i < items.Length; i++)
        {
            SafeHandle handle = items[i];
            fds.AddHandle(handle);
            values[i] = new VariantValue(fds, i);
        }
        return new VariantValue(VariantValueType.UnixFd, itemSignature: null, values, nesting: 0);
    }
    public static VariantValue Array(List<Signature> items)
        => Array(items.ToArray());
    public static VariantValue Array(Signature[] items)
    {
        ThrowIfNull(items, nameof(items));
        VariantValue[] values = new VariantValue[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            values[i] = Signature(items[i]);
        }
        return new VariantValue(VariantValueType.Signature, itemSignature: null, values, nesting: 0);
    }
    public static VariantValue Array(List<bool> items)
        => Array(items.ToArray());
    public static VariantValue Array(bool[] items)
    {
        ThrowIfNull(items, nameof(items));
        VariantValue[] values = new VariantValue[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            values[i] = Bool(items[i]);
        }
        return new VariantValue(VariantValueType.Bool, itemSignature: null, values, nesting: 0);
    }
    internal static VariantValue Array(ReadOnlySpan<byte> itemSignature, VariantValue[] items)
    {
        ThrowIfNull(items, nameof(items));
        ThrowIfSignatureEmpty(itemSignature, nameof(itemSignature));

        VariantValueType firstType = DetermineType(itemSignature);

        if (firstType != VariantValueType.Array
            && firstType != VariantValueType.Struct
            && firstType != VariantValueType.Dictionary
            && firstType != VariantValueType.Variant)
        {
            throw new ArgumentException($"Unsupported item type: {firstType}. Use a type-safe overload for the item type.");
        }

        int count = items.Length;
        if (count == 0)
        {
            ThrowIfSignatureNotSingleComplete(itemSignature, nameof(itemSignature));
        }
        else
        {
            SignatureCheck sigCheck = new(itemSignature);
            foreach (var item in items)
            {
                sigCheck.ThrowIfNoMatch(item);
            }
        }

        return new VariantValue(firstType, itemSignature: GetSignatureObject(count, itemSignature), items, nesting: 0);
    }

    private static void ThrowIfSignatureEmpty(ReadOnlySpan<byte> signature, string paramName)
    {
        if (signature.IsEmpty)
        {
            ThrowSignatureEmpty(paramName);
        }
    }

    private static void ThrowIfSignatureNotSingleComplete(ReadOnlySpan<byte> signature, string paramName)
    {
        Debug.Assert(signature.Length > 0);
        if (!IsSingleOrComplete(signature))
        {
            ThrowSignatureIsNotSingleOrComplete(signature, paramName);
        }
    }

    private static bool IsSingleOrComplete(ReadOnlySpan<byte> signature)
    {
        Debug.Assert(signature.Length > 0);
        if (signature.Length == 1)
        {
            return ProtocolConstants.IsSingleCompleteType(signature[0]);
        }
        else
        {
            DBusType type = (DBusType)signature[0];
            switch (type)
            {
                case DBusType.Array:
                    if (signature.Length == 1)
                    {
                        return false;
                    }
                    if (signature[1] == (byte)DBusType.DictEntry)
                    {
                        if (signature[signature.Length - 1] != (byte)'}' || signature.Length < 5)
                        {
                            return false;
                        }
                        return IsSingleOrComplete(signature.Slice(2, 1)) && IsSingleOrComplete(signature.Slice(3, signature.Length - 4));
                    }
                    else
                    {
                        return IsSingleOrComplete(signature.Slice(1));
                    }
                case DBusType.Struct:
                    if (signature[signature.Length - 1] != (byte)')' || signature.Length < 3)
                    {
                        return false;
                    }
                    signature = signature.Slice(1, signature.Length - 2);
                    while (TryReadSingleCompleteType(ref signature, out ReadOnlySpan<byte> itemSignature))
                    {
                        if (!IsSingleOrComplete(itemSignature))
                        {
                            return false;
                        }
                    }
                    return true;
                default:
                    return false;

            }
        }
    }

    private static void ThrowSignatureEmpty(string paramName)
    {
        throw new ArgumentException("Signature is empty.", paramName);
    }

    private static void ThrowSignatureIsNotSingleOrComplete(ReadOnlySpan<byte> signature, string paramName)
    {
        throw new ArgumentException($"Signature '{Encoding.UTF8.GetString(signature)}' is not a single complete type.", paramName);
    }

    internal static VariantValue Dictionary(DBusType keyType, ReadOnlySpan<byte> valueSignature, KeyValuePair<VariantValue, VariantValue>[] items)
    {
        ThrowIfNull(items, nameof(items));
        ThrowIfSignatureEmpty(valueSignature, nameof(valueSignature));

        ReadOnlySpan<byte> keySignature = [ (byte)keyType ];

        int count = items.Length;
        if (count == 0)
        {
            ThrowIfSignatureNotSingleComplete(keySignature, nameof(keyType));
            ThrowIfSignatureNotSingleComplete(valueSignature, nameof(valueSignature));
        }
        else
        {
            SignatureCheck keySigCheck = new(keySignature);
            SignatureCheck valueSigCheck = new(valueSignature);
            foreach (var item in items)
            {
                keySigCheck.ThrowIfNoMatch(item.Key);
                valueSigCheck.ThrowIfNoMatch(item.Value);
            }
        }

        return new VariantValue((VariantValueType)keyType, DetermineType(valueSignature), GetSignatureObject(count, valueSignature), items, nesting: 0);
    }

    internal static VariantValueType DetermineType(ReadOnlySpan<byte> signature)
    {
        VariantValueType type = (VariantValueType)signature[0];
        if (type == VariantValueType.Array && (DBusType)signature[1] == DBusType.DictEntry)
        {
            type = VariantValueType.Dictionary;
        }
        return type;
    }

    readonly ref struct SignatureCheck
    {
        private readonly ReadOnlySpan<byte> _signature;
        private readonly object? _typeDef;
        private readonly long _typeL;
        private readonly bool _extendedCheck;

        public SignatureCheck(ReadOnlySpan<byte> signature)
        {
            Debug.Assert(signature.Length > 0);

            VariantValueType signatureType = (VariantValueType)signature[0];
            
            _signature = signature;
            _typeDef = null;
            _extendedCheck = false;

            if (signatureType == VariantValueType.Array)
            {
                if (signature.Length > 1)
                {
                    if (signature[1] == (byte)DBusType.DictEntry)
                    {
                        if (signature.Length >= 5 &&
                            signature[signature.Length - 1] == (byte)'}')
                        {
                            VariantValueType keyType = (VariantValueType)signature[2];
                            VariantValueType valueType = (VariantValueType)signature[3];
                            _extendedCheck = valueType == VariantValueType.Array || valueType == VariantValueType.Struct;
                            if (valueType == VariantValueType.Array && signature[4] == '{') // a{xa{
                            {
                                valueType = VariantValueType.Dictionary;
                            }

                            _typeL = (((long)VariantValueType.Dictionary) << TypeShift) |
                                     (((long)keyType) << DictionaryKeyTypeShift) |
                                     (((long)valueType) << DictionaryValueTypeShift);
                            return;
                        }
                    }
                    else
                    {
                        VariantValueType itemType = (VariantValueType)signature[1];
                        _extendedCheck = itemType == VariantValueType.Array || itemType == VariantValueType.Struct;
                        if (itemType == VariantValueType.Array)
                        {
                            if (signature.Length < 3)
                            {
                                ThrowInvalidSignature(signature);
                            }
                            if (signature[2] == '{') // aa{
                            {
                                itemType = VariantValueType.Dictionary;
                            }
                        }

                        _typeL = (((long)VariantValueType.Array) << TypeShift) |
                                 (((long)itemType) << ArrayItemTypeShift);
                        return;
                    }
                }
            }
            else if (signatureType == VariantValueType.Struct)
            {
                if (signature[signature.Length - 1] == (byte)')')
                {
                    _typeL = (((long)VariantValueType.Struct) << TypeShift) |
                             (((long)GetStructVariantMask(signature)) << StructVariantMaskShift);
                    _extendedCheck = true;
                    return;
                }
            }
            else if (signature.Length == 1)
            {
                switch (signatureType)
                {
                    case VariantValueType.Invalid:
                        ThrowInvalidSignature(signature);
                        return;
                    case VariantValueType.Variant:
                        _typeL = 0;
                        return;
                    case VariantValueType.Int64:
                        _typeDef = Int64TypeDescriptor;
                        _typeL = 0;
                        return;
                    case VariantValueType.UInt64:
                        _typeDef = UInt64TypeDescriptor;
                        _typeL = 0;
                        return;
                    case VariantValueType.Double:
                        _typeDef = DoubleTypeDescriptor;
                        _typeL = 0;
                        return;
                    default:
                        _typeL = ((long)signatureType) << TypeShift;
                        return;
                }
            }

            ThrowInvalidSignature(signature);
        }

        private static long GetStructVariantMask(ReadOnlySpan<byte> signature)
        {
            signature = signature.Slice(1, signature.Length - 1);
            int i = 0;
            long mask = 0;
            while (TryReadSingleCompleteType(ref signature, out ReadOnlySpan<byte> itemSignature))
            {
                if (itemSignature.SequenceEqual(Tmds.DBus.Protocol.Signature.Variant))
                {
                    mask |= 1L << i;
                }
                i++;
            }
            return mask;
        }

        private void ThrowInvalidSignature(ReadOnlySpan<byte> signature)
        {
            throw new ArgumentException($"Invalid signature: '{Encoding.UTF8.GetString(signature)}'", nameof(signature));
        }

        public void ThrowIfNoMatch(VariantValue vv)
        {
            if (_typeDef is not null)
            {
                if (object.ReferenceEquals(vv._o, _typeDef))
                {
                    return;
                }
            }
            else if (_typeL != 0)
            {
                if ((vv._l & MetadataMask) == _typeL)
                {
                    if (_extendedCheck)
                    {
                        ExtendedCheck(vv);
                    }
                    return;
                }
            }
            else
            {
                // signature is Variant, just ensure the value is valid.
                vv.ThrowIfInvalid();
                return;
            }

            ThrowValueSignatureMismatch(vv, _signature);
        }

        private static void ThrowValueSignatureMismatch(VariantValue vv, ReadOnlySpan<byte> signature)
        {
            throw new ArgumentException($"Value {vv} does not match signature: '{Encoding.UTF8.GetString(signature)}'");
        }

        private void ExtendedCheck(VariantValue vv)
        {
            // We've already verified type matches the signature.
            VariantValueType type = vv.UnsafeDetermineInnerType(TypeShift);
            if (type == VariantValueType.Array)
            {
                ReadOnlySpan<byte> itemSignature = _signature.Slice(1);
                if (vv.UnsafeCount == 0)
                {
                    if (itemSignature.SequenceEqual((vv._o as byte[])!))
                    {
                        return;
                    }
                }
                else
                {
                    VariantValue item = (vv._o as IList<VariantValue>)![0];
                    SignatureCheck sigCheck = new(itemSignature);
                    sigCheck.ThrowIfNoMatch(item);
                    return;
                }
            }
            else if (type == VariantValueType.Dictionary)
            {
                // We've already verified the key type matches.
                ReadOnlySpan<byte> valueSignature = _signature.Slice(3, _signature.Length - 4);
                if (vv.UnsafeCount == 0)
                {
                    if (valueSignature.SequenceEqual((vv._o as byte[])!))
                    {
                        return;
                    }
                }
                else
                {
                    KeyValuePair<VariantValue, VariantValue> pair = (vv._o as KeyValuePair<VariantValue, VariantValue>[])![0];
                    SignatureCheck sigCheck = new(valueSignature);
                    sigCheck.ThrowIfNoMatch(pair.Value);
                    return;
                }
            }
            else if (type == VariantValueType.Struct)
            {
                // We've already verified the variant mask matches.
                ReadOnlySpan<byte> signature = _signature.Slice(1, _signature.Length - 2);
                var items = (vv._o as VariantValue[])!;
                int mask = (int)(vv._l >> StructVariantMaskShift);
                foreach (var item in items)
                {
                    if (!TryReadSingleCompleteType(ref signature, out ReadOnlySpan<byte> itemSignature))
                    {
                        goto NoMatch;
                    }
                    if ((mask & 1) == 0)
                    {
                        SignatureCheck sigCheck = new(itemSignature);
                        sigCheck.ThrowIfNoMatch(item);
                    }
                    mask >>= 1;
                }
                if (signature.Length == 0)
                {
                    return;
                }
            }

            NoMatch:
            ThrowValueSignatureMismatch(vv, _signature);
        }
    }

    // The signature returned in itemSignature is best-effort. It may be an invalid signature.
    private static bool TryReadSingleCompleteType(ref ReadOnlySpan<byte> signature, out ReadOnlySpan<byte> itemSignature)
    {
        ReadOnlySpan<byte> s = signature;

        int length = 0;
        while (s.Length > 0 && s[0] == (byte)DBusType.Array)
        {
            s = s.Slice(1);
            length++;
        }

        if (s.Length > 0)
        {
            DBusType type = (DBusType)s[0];
            length++;
            if (type == DBusType.Struct || type == DBusType.DictEntry)
            {
                s = s.Slice(1);
                byte startChar = (byte)type;
                byte endChar = type == DBusType.Struct ? (byte)')' : (byte)'}';
                int count = 1;
                do
                {
                    int offset = s.IndexOfAny(startChar, endChar);
                    if (offset == -1)
                    {
                        // Signature is invalid. Return the whole thing.
                        length = signature.Length;
                        break;
                    }

                    if (s[offset] == startChar)
                    {
                        count++;
                    }
                    else
                    {
                        count--;
                    }

                    length += offset + 1;
                    s = s.Slice(offset + 1);
                } while (count > 0);
            }
        }

        itemSignature = signature.Slice(0, length);
        signature = signature.Slice(length);
        return length != 0;
    }

    public VariantValueType ItemType
        => DetermineInnerType(VariantValueType.Array, ArrayItemTypeShift);

    public VariantValueType KeyType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryKeyTypeShift);

    public VariantValueType ValueType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryValueTypeShift);

    // For Testing
    internal VariantValueType GetStructFieldType(int index)
    {
        EnsureTypeIs(VariantValueType.Struct);
        if (index < 0 || index > UnsafeCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (UnsafeIsStructFieldVariant(index))
        {
            return VariantValueType.Variant;
        }
        return ((VariantValue[])_o!)[index].Type;
    }

    internal void ThrowIfInvalid()
    {
        if (_l == 0 && _o is null)
        {
            ThrowArgumentNull("value");
        }
    }

    private static void ThrowIfNull(object? argument, string paramName)
    {
        if (argument is null)
        {
            ThrowArgumentNull(paramName);
        }
    }

    internal static void ThrowArgumentNull(string paramName) =>
        throw new ArgumentNullException(paramName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTypeIs(VariantValueType expected)
        => EnsureTypeIs(Type, expected);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureTypeIs(VariantValueType actual, VariantValueType expected)
    {
        if (actual != expected)
        {
            ThrowCannotRetrieveAs(actual, expected);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureTypeIs(VariantValueType actual, VariantValueType[] expected)
    {
        if (System.Array.IndexOf<VariantValueType>(expected, actual) == -1)
        {
            ThrowCannotRetrieveAs(actual, expected);
        }
    }

    private VariantValueType UnsafeDetermineInnerType(int typeShift)
    {
        return (VariantValueType)((_l >> typeShift) & 0xff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValueType DetermineInnerType(VariantValueType outer, int typeShift)
    {
        VariantValueType type = DetermineType();
        return type == outer ? UnsafeDetermineInnerType(typeShift) : VariantValueType.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (VariantValueType type, byte nesting) DetermineTypeAndNesting()
    {
        long l = _o is TypeData td ? td.L : _l;

        byte nesting = (byte)(l >> NestingShift);
        VariantValueType type = (VariantValueType)((l >> TypeShift) & 0xff);
        return (type, nesting);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValueType DetermineType()
    {
        long l = _o is TypeData td ? td.L : _l;

        l >>= TypeShift;
        return l > 0xff ? VariantValueType.Variant : (VariantValueType)l;
    }

    private static void ThrowCannotRetrieveAs(VariantValueType from, VariantValueType to)
        => ThrowCannotRetrieveAs(from.ToString(), [ to.ToString() ]);

    private static void ThrowCannotRetrieveAs(VariantValueType from, VariantValueType[] to)
        => ThrowCannotRetrieveAs(from.ToString(), to.Select(expected => expected.ToString()));

    private static void ThrowCannotRetrieveAs(string from, string to)
        => ThrowCannotRetrieveAs(from, [ to ]);

    private static void ThrowCannotRetrieveAs(VariantValueType from, Type to)
        => ThrowCannotRetrieveAs(from.ToString(), to.FullName ?? "?<Type>?");

    private static void ThrowCannotRetrieveAs(string from, IEnumerable<string> to)
    {
        throw new InvalidOperationException($"Type {from} can not be retrieved as {string.Join("/", to)}.");
    }

    public override string ToString()
        => ToString(includeTypeSuffix: true);

    public string ToString(bool includeTypeSuffix)
    {
        // This is implemented so something user-friendly shows in the debugger.
        // By overriding the ToString method, it will also affect generic types like KeyValueType<TKey, TValue> that call ToString.
        (VariantValueType type, byte nesting) = DetermineTypeAndNesting();
        switch (type)
        {
            case VariantValueType.Byte:
                return $"{UnsafeGetByte()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Bool:
                return $"{UnsafeGetBool()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Int16:
                return $"{UnsafeGetInt16()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.UInt16:
                return $"{UnsafeGetUInt16()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Int32:
                return $"{UnsafeGetInt32()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.UInt32:
                return $"{UnsafeGetUInt32()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Int64:
                return $"{UnsafeGetInt64()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.UInt64:
                return $"{UnsafeGetUInt64()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Double:
                return $"{UnsafeGetDouble()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.String:
            case VariantValueType.ObjectPath:
                return $"{UnsafeGetString()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Signature:
                return $"{UnsafeGetSignature()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Struct:
                var values = (_o as VariantValue[])!;
                return $"({string.Join(", ", values.Select(v => v.ToString(includeTypeSuffix: false)))}){TypeSuffix(includeTypeSuffix, type, nesting)}";
            default:
                return $"{TypeString(type, nesting)}";
        }
    }

    string TypeSuffix(bool includeTypeSuffix, VariantValueType type, byte nesting)
        => !includeTypeSuffix ? "" : $" {TypeString(type, nesting)}";

    string TypeString(VariantValueType type, byte nesting)
    {
        string suffix = "";
        if (type == VariantValueType.Struct)
        {
            var _this = this;
            var values = (_o as VariantValue[])!;
            suffix = $"<{string.Join(", ", values.Select((v, idx) => _this.UnsafeIsStructFieldVariant(idx) ? VariantValueType.Variant : v.Type))}>";
        }
        else if (type == VariantValueType.Array)
        {
            suffix = $"<{UnsafeDetermineInnerType(ArrayItemTypeShift)}>, Count={UnsafeCount}";
        }
        else if (type == VariantValueType.Dictionary)
        {
            suffix = $"<{UnsafeDetermineInnerType(DictionaryKeyTypeShift)}, {UnsafeDetermineInnerType(DictionaryValueTypeShift)}>, Count={UnsafeCount}";
        }
        return nesting switch
        {
            0 => $"[{type}{suffix}]",
            1 => $"[{nameof(VariantValueType.Variant)}<{type}{suffix}>]",
            _ => $"[{nameof(VariantValueType.Variant)}^{nesting}<{type}{suffix}]>"
        };
    }

    public static bool operator==(VariantValue lhs, VariantValue rhs)
        => lhs.Equals(rhs);

    public static bool operator!=(VariantValue lhs, VariantValue rhs)
        => !lhs.Equals(rhs);

    public override bool Equals(object? obj)
    {
        if (obj is not null && obj.GetType() == typeof(VariantValue))
        {
            return ((VariantValue)obj).Equals(this);
        }
        return false;
    }

    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        return _l.GetHashCode() + 17 * (_o?.GetHashCode() ?? 0);
#else
        return HashCode.Combine(_l, _o);
#endif
    }

    public bool Equals(VariantValue other)
    {
        if (_l != other._l)
        {
            return false;
        }
        if (object.ReferenceEquals(_o, other._o))
        {
            return true;
        }
        (VariantValueType type, byte nesting) = DetermineTypeAndNesting();
        if ((type, nesting) != other.DetermineTypeAndNesting())
        {
            return false;
        }
        switch (type)
        {
            case VariantValueType.Int64:
            case VariantValueType.UInt64:
            case VariantValueType.Double:
                // l, type, and nesting are the same (per previous checks)
                return true;
            case VariantValueType.String:
            case VariantValueType.ObjectPath:
                return (_o as string)!.Equals(other._o as string, StringComparison.Ordinal);
            case VariantValueType.Signature:
                return (_o as byte[])!.SequenceEqual((other._o as byte[])!);
        }
        // Always return false for composite types and handles.
        return false;
    }

    // For testing.
    internal string GetDBusSignature()
    {
        Span<byte> span = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        return Encoding.UTF8.GetString(GetSignature(Type, span));
    }

    internal void WriteVariantTo(ref MessageWriter writer)
    {
        WriteValueTo(ref writer, nestingOffset: +1);
    }

    private void WriteValueTo(ref MessageWriter writer, int nestingOffset)
    {
        (VariantValueType type, int nesting) = DetermineTypeAndNesting();

        nesting += nestingOffset;
        while (nesting > 1)
        {
            writer.WriteSignature(Protocol.Signature.Variant);
            nesting--;
        }
        if (nesting == 1)
        {
            WriteSignatureTo(type, ref writer);
        }

        switch (type)
        {
            case VariantValueType.Byte:
                writer.WriteByte(UnsafeGetByte());
                break;
            case VariantValueType.Bool:
                writer.WriteBool(UnsafeGetBool());
                break;
            case VariantValueType.Int16:
                writer.WriteInt16(UnsafeGetInt16());
                break;
            case VariantValueType.UInt16:
                writer.WriteUInt16(UnsafeGetUInt16());
                break;
            case VariantValueType.Int32:
                writer.WriteInt32(UnsafeGetInt32());
                break;
            case VariantValueType.UInt32:
                writer.WriteUInt32(UnsafeGetUInt32());
                break;
            case VariantValueType.Int64:
                writer.WriteInt64(UnsafeGetInt64());
                break;
            case VariantValueType.UInt64:
                writer.WriteUInt64(UnsafeGetUInt64());
                break;
            case VariantValueType.Double:
                writer.WriteDouble(UnsafeGetDouble());
                break;
            case VariantValueType.String:
                writer.WriteString(UnsafeGetString());
                break;
            case VariantValueType.ObjectPath:
                writer.WriteObjectPath(UnsafeGetString());
                break;
            case VariantValueType.Signature:
                writer.WriteSignature(UnsafeGetSignature());
                break;
            case VariantValueType.UnixFd:
                SafeHandle? handle = UnsafeReadHandle<Microsoft.Win32.SafeHandles.SafeFileHandle>();
                if (handle is null)
                {
                    throw new InvalidOperationException("Handle already read");
                }
                break;
            case VariantValueType.Array:
                WriteArrayTo(ref writer);
                break;
            case VariantValueType.Struct:
                WriteStructTo(ref writer);
                break;
            case VariantValueType.Dictionary:
                WriteDictionaryTo(ref writer);
                break;
            default:
                throw new ArgumentException($"VariantValueType: {type}");
        }
    }

    private void WriteStructTo(ref MessageWriter writer)
    {
        writer.WriteStructureStart();
        var items = (_o as VariantValue[])!;
        int mask = (int)(_l >> StructVariantMaskShift);
        foreach (var item in items)
        {
            item.WriteValueTo(ref writer, mask & 1);
            mask >>= 1;
        }
    }

    private void WriteDictionaryTo(ref MessageWriter writer)
    {
        ArrayStart arrayStart = writer.WriteDictionaryStart();
        if (UnsafeCount > 0)
        {
            DBusType keyType = ToDBusType(UnsafeDetermineInnerType(DictionaryKeyTypeShift));
            DBusType valueType = ToDBusType(UnsafeDetermineInnerType(DictionaryValueTypeShift));
            int keyNestingOffset = keyType == DBusType.Variant ? 1 : 0;
            int valueNestingOffset = valueType == DBusType.Variant ? 1 : 0;

            var pairs = (_o as KeyValuePair<VariantValue, VariantValue>[])!;
            foreach (var pair in pairs)
            {
                writer.WriteDictionaryEntryStart();
                pair.Key.WriteValueTo(ref writer, keyNestingOffset);
                pair.Value.WriteValueTo(ref writer, valueNestingOffset);
            }
        }
        writer.WriteDictionaryEnd(arrayStart);
    }

    private void WriteArrayTo(ref MessageWriter writer)
    {
        DBusType itemType = ToDBusType(UnsafeDetermineInnerType(ArrayItemTypeShift));
        switch (itemType)
        {
            case DBusType.Byte:
                writer.WriteArray(UnsafeArrayAsSpan<byte>());
                return;
            case DBusType.Int16:
                writer.WriteArray(UnsafeArrayAsSpan<short>());
                return;
            case DBusType.UInt16:
                writer.WriteArray(UnsafeArrayAsSpan<ushort>());
                return;
            case DBusType.Int32:
                writer.WriteArray(UnsafeArrayAsSpan<int>());
                return;
            case DBusType.UInt32:
                writer.WriteArray(UnsafeArrayAsSpan<uint>());
                return;
            case DBusType.Int64:
                writer.WriteArray(UnsafeArrayAsSpan<long>());
                return;
            case DBusType.UInt64:
                writer.WriteArray(UnsafeArrayAsSpan<ulong>());
                return;
            case DBusType.Double:
                writer.WriteArray(UnsafeArrayAsSpan<double>());
                return;
            case DBusType.String:
                writer.WriteArray(UnsafeArrayAsSpan<string>());
                return;
            case DBusType.ObjectPath:
                writer.WriteArray(UnsafeArrayAsSpan<ObjectPath>());
                return;
        }

        ArrayStart arrayStart = writer.WriteArrayStart(itemType);
        var items = _o as VariantValue[];
        if (items is not null)
        {
            int nestingOffset = itemType == DBusType.Variant ? 1 : 0;
            foreach (var item in items)
            {
                item.WriteValueTo(ref writer, nestingOffset);
            }
        }
        writer.WriteArrayEnd(arrayStart);
    }

    private static DBusType ToDBusType(VariantValueType type)
        => (DBusType)type;

    private void WriteSignatureTo(VariantValueType type, ref MessageWriter writer)
    {
        Span<byte> span = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        ReadOnlySpan<byte> signature = GetSignature(type, span);
        writer.WriteSignature(signature);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetSignature(VariantValueType type, Span<byte> buffer)
    {
        Debug.Assert(buffer.Length >= ProtocolConstants.MaxSignatureLength);

        int bytesWritten = AppendTypeSignature(type, buffer);
        return buffer.Slice(0, bytesWritten);
    }

    private int AppendTypeSignature(Span<byte> signature)
        => AppendTypeSignature(Type, signature);

    private int AppendTypeSignature(VariantValueType type, Span<byte> signature)
    {
        switch (type)
        {
            case VariantValueType.Invalid:
                ThrowTypeInvalid();
                break;
            case VariantValueType.Array:
            {
                int length = 0;
                signature[length++] = (byte)'a';
                VariantValueType itemType = UnsafeDetermineInnerType(ArrayItemTypeShift);
                if (!TryAppendSignatureFromMetadata(signature, ref length, itemType))
                {
                    VariantValue vv = (_o as IList<VariantValue>)![0];
                    length += vv.AppendTypeSignature(signature.Slice(length));
                }
                return length;
            }
            case VariantValueType.Struct:
            {
                int length = 0;
                signature[length++] = (byte)'(';
                int count = UnsafeCount;
                for (int i = 0; i < count; i++)
                {
                    if (UnsafeIsStructFieldVariant(i))
                    {
                        signature[length++] = (byte)VariantValueType.Variant;
                    }
                    else
                    {
                        VariantValue vv = (_o as VariantValue[])![i];
                        length += vv.AppendTypeSignature(signature.Slice(length));
                    }
                }
                signature[length++] = (byte)')';
                return length;
            }
            case VariantValueType.Dictionary:
            {
                int length = 0;
                signature[length++] = (byte)'a';
                signature[length++] = (byte)'{';
                signature[length++] = (byte)UnsafeDetermineInnerType(DictionaryKeyTypeShift);
                VariantValueType valueType = UnsafeDetermineInnerType(DictionaryValueTypeShift);
                if (!TryAppendSignatureFromMetadata(signature, ref length, valueType))
                {
                    VariantValue vv = (_o as KeyValuePair<VariantValue, VariantValue>[])![0].Value;
                    length += vv.AppendTypeSignature(signature.Slice(length));
                }
                signature[length++] = (byte)'}';
                return length;
            }
        }
        signature[0] = (byte)type;
        return 1;
    }

    bool TryAppendSignatureFromMetadata(Span<byte> signature, ref int length, VariantValueType type)
    {
        if (IsSimpleSignature(type))
        {
            signature[length++] = (byte)type;
            return true;
        }
        else if (UnsafeCount == 0)
        {
            byte[] itemSig = (byte[])_o!;
            itemSig.CopyTo(signature.Slice(length));
            length += itemSig.Length;
            return true;
        }
        else
        {
            return false;
        }
    }

    // Signature consists of a single character.
    private static bool IsSimpleSignature(VariantValueType type)
            => type != VariantValueType.Array &&
               type != VariantValueType.Dictionary &&
               type != VariantValueType.Struct;

    private static void ThrowTypeInvalid()
    {
        throw new ArgumentNullException();
    }
}
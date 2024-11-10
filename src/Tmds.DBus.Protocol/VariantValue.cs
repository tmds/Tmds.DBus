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
    private const long StructVariantMask = 0xffffL;
    private const int StructVariantMaskShift = 8 * 4;
    internal const int MaxStructFields = 2 * 8;

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
    private const long Int64 = ((long)VariantValueType.Int64) << TypeShift;
    private const long UInt64 = ((long)VariantValueType.UInt64) << TypeShift;
    private const long Double = ((long)VariantValueType.Double) << TypeShift;
    private const long VariantOfInt64 = (1L << NestingShift) | Int64;
    private const long VariantOfUInt64 = (1L << NestingShift) | UInt64;
    private const long VariantOfDouble = (1L << NestingShift) | Double;
    private static readonly object Int64TypeDescriptor = new TypeData(Int64);
    private static readonly object UInt64TypeDescriptor = new TypeData(UInt64);
    private static readonly object DoubleTypeDescriptor = new TypeData(Double);
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
            Int64 => Int64TypeDescriptor,
            UInt64 => UInt64TypeDescriptor,
            Double => DoubleTypeDescriptor,
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
    internal VariantValue(byte value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(byte value, byte nesting)
    {
        _l = value | GetTypeMetadata(VariantValueType.Byte, nesting);
        _o = null;
    }
    internal VariantValue(bool value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(bool value, byte nesting)
    {
        _l = (value ? 1L : 0) | GetTypeMetadata(VariantValueType.Bool, nesting);
        _o = null;
    }
    internal VariantValue(short value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(short value, byte nesting)
    {
        _l = (ushort)value | GetTypeMetadata(VariantValueType.Int16, nesting);
        _o = null;
    }
    internal VariantValue(ushort value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(ushort value, byte nesting)
    {
        _l = value | GetTypeMetadata(VariantValueType.UInt16, nesting);
        _o = null;
    }
    internal VariantValue(int value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(int value, byte nesting)
    {
        _l = (uint)value | GetTypeMetadata(VariantValueType.Int32, nesting);
        _o = null;
    }
    internal VariantValue(uint value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(uint value, byte nesting)
    {
        _l = value | GetTypeMetadata(VariantValueType.UInt32, nesting);
        _o = null;
    }
    internal VariantValue(long value) :
        this(value, nesting: 0)
    { }
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
    internal VariantValue(ulong value) :
        this(value, nesting: 0)
    { }
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
    internal unsafe VariantValue(double value) :
        this(value, nesting: 0)
    { }
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
    internal VariantValue(string value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(string value, byte nesting)
    {
        _l = GetTypeMetadata(VariantValueType.String, nesting);
        _o = value ?? throw new ArgumentNullException(nameof(value));
    }
    internal VariantValue(ObjectPath value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(ObjectPath value, byte nesting)
    {
        _l = GetTypeMetadata(VariantValueType.ObjectPath, nesting);
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    internal VariantValue(Signature value) :
        this(value, nesting: 0)
    { }
    internal VariantValue(Signature value, byte nesting)
    {
        _l = GetTypeMetadata(VariantValueType.Signature, nesting);
        byte[] data = value.Data;
        if (data.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = data;
    }
    // Array
    internal VariantValue(VariantValueType itemType, object? itemSignature, VariantValue[] items, byte nesting)
    {
        Debug.Assert(
            itemType != VariantValueType.Byte &&
            itemType != VariantValueType.Int16 &&
            itemType != VariantValueType.UInt16 &&
            itemType != VariantValueType.Int32 &&
            itemType != VariantValueType.UInt32 &&
            itemType != VariantValueType.Int64 &&
            itemType != VariantValueType.UInt64 &&
            itemType != VariantValueType.Double
        );
        int count = items.Length;
        _l = GetArrayTypeMetadata(itemType, count, nesting);
        _o = count == 0 ? itemSignature : items;
        if (_o?.GetType() == typeof(int))
        {
            throw new Exception();
        }
    }
    // For testing
    internal VariantValue(VariantValueType itemType, VariantValue[] items, object? itemSignature = null) :
        this(itemType, itemSignature, items, nesting: 0)
    { }
    internal VariantValue(VariantValueType itemType, string[] items) :
        this(itemType, items, nesting: 0)
    { }
    internal VariantValue(VariantValueType itemType, string[] items, byte nesting)
    {
        Debug.Assert(itemType == VariantValueType.String || itemType == VariantValueType.ObjectPath);
        _l = GetArrayTypeMetadata(itemType, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(byte[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(byte[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.Byte, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(short[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(short[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.Int16, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(ushort[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(ushort[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.UInt16, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(int[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(int[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.Int32, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(uint[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(uint[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.UInt32, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(long[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(long[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.Int64, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(ulong[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(ulong[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.UInt64, items.Length, nesting);
        _o = items;
    }
    internal VariantValue(double[] items) :
        this(items, nesting: 0)
    { }
    internal VariantValue(double[] items, byte nesting)
    {
        _l = GetArrayTypeMetadata(VariantValueType.Double, items.Length, nesting);
        _o = items;
    }
    // Dictionary
    internal VariantValue(VariantValueType keyType, VariantValueType valueType, object? valueSignature, KeyValuePair<VariantValue, VariantValue>[] pairs, byte nesting)
    {
        int count = pairs.Length;
        _l = GetDictionaryTypeMetadata(keyType, valueType, count, nesting);
        _o = count == 0 ? valueSignature : pairs;
    }
    // For testing
    internal VariantValue(VariantValueType keyType, VariantValueType valueType, KeyValuePair<VariantValue, VariantValue>[] pairs, object? valueSignature = null) :
        this(keyType, valueType, valueSignature, pairs, nesting: 0)
    { }
    // Struct
    internal VariantValue(VariantValue[] fields) :
        this(fields, nesting: 0)
    { }
    internal VariantValue(VariantValue[] fields, byte nesting)
    {
        long variantMask = 0;
        for (int i = 0; i < fields.Length; i++)
        {
            if (i > VariantValue.MaxStructFields)
            {
                ThrowMaxStructFieldsExceeded();
            }
            VariantValue value = fields[i];
            if (value.Type == VariantValueType.Variant)
            {
                variantMask |= (1L << i);
                fields[i] = value.GetVariantValue();
            }
        }
        _l = GetStructMetadata(variantMask, fields.Length, nesting);
        _o = fields;
    }
    internal static void ThrowMaxStructFieldsExceeded()
    {
        throw new NotSupportedException($"Struct types {VariantValue.MaxStructFields}+ fields are not supported.");
    }
    internal VariantValue(long variantMask, VariantValue[] fields, byte nesting)
    {
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
    // Variant
    internal static VariantValue CreateVariant(VariantValue value)
    {
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

    public string GetObjectPath()
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

    public T[] GetArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
        where T : notnull
    {
        EnsureTypeIs(VariantValueType.Array);
        EnsureCanUnsafeGet<T>(ItemType);

        if (UnsafeCount == 0)
        {
            return Array.Empty<T>();
        }

        // Return the array by reference when we can.
        // Don't bother to make a copy in case the caller mutates the data and
        // calls GetArray again to retrieve the original data. It's an unlikely scenario.
        if (typeof(T) == typeof(byte))
        {
            return (T[])(object)(_o as byte[])!;
        }
        else if (typeof(T) == typeof(short))
        {
            return (T[])(object)(_o as short[])!;
        }
        else if (typeof(T) == typeof(int))
        {
            return (T[])(object)(_o as int[])!;
        }
        else if (typeof(T) == typeof(long))
        {
            return (T[])(object)(_o as long[])!;
        }
        else if (typeof(T) == typeof(ushort))
        {
            return (T[])(object)(_o as ushort[])!;
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T[])(object)(_o as uint[])!;
        }
        else if (typeof(T) == typeof(ulong))
        {
            return (T[])(object)(_o as ulong[])!;
        }
        else if (typeof(T) == typeof(double))
        {
            return (T[])(object)(_o as double[])!;
        }
        else if (typeof(T) == typeof(string))
        {
            return (T[])(object)(_o as string[])!;
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
                    return new VariantValue((_o as byte[])![i]);
                case VariantValueType.Int16:
                    return new VariantValue((_o as short[])![i]);
                case VariantValueType.UInt16:
                    return new VariantValue((_o as ushort[])![i]);
                case VariantValueType.Int32:
                    return new VariantValue((_o as int[])![i]);
                case VariantValueType.UInt32:
                    return new VariantValue((_o as uint[])![i]);
                case VariantValueType.Int64:
                    return new VariantValue((_o as long[])![i]);
                case VariantValueType.UInt64:
                    return new VariantValue((_o as ulong[])![i]);
                case VariantValueType.Double:
                    return new VariantValue((_o as double[])![i]);
                case VariantValueType.String:
                case VariantValueType.ObjectPath:
                    return new VariantValue((_o as string[])![i]);
            }
        }

        var values = _o as VariantValue[];
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
        => new VariantValue(value);
    public static implicit operator VariantValue(bool value)
        => new VariantValue(value);
    public static implicit operator VariantValue(short value)
        => new VariantValue(value);
    public static implicit operator VariantValue(ushort value)
        => new VariantValue(value);
    public static implicit operator VariantValue(int value)
        => new VariantValue(value);
    public static implicit operator VariantValue(uint value)
        => new VariantValue(value);
    public static implicit operator VariantValue(long value)
        => new VariantValue(value);
    public static implicit operator VariantValue(ulong value)
        => new VariantValue(value);
    public static implicit operator VariantValue(double value)
        => new VariantValue(value);
    public static implicit operator VariantValue(string value)
        => new VariantValue(value);
    public static implicit operator VariantValue(ObjectPath value)
        => new VariantValue(value);
    public static implicit operator VariantValue(Signature value)
        => new VariantValue(value);

    public VariantValueType ItemType
        => DetermineInnerType(VariantValueType.Array, ArrayItemTypeShift);

    public VariantValueType KeyType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryKeyTypeShift);

    public VariantValueType ValueType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryValueTypeShift);

    public VariantValueType GetStructFieldType(int index)
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
        if (Array.IndexOf<VariantValueType>(expected, actual) == -1)
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
                return $"{UnsafeGetString()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.ObjectPath:
                return $"{UnsafeGetString()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Signature:
                return $"{UnsafeGetSignature()}{TypeSuffix(includeTypeSuffix, type, nesting)}";
            case VariantValueType.Struct:
                var values = (_o as VariantValue[]) ?? Array.Empty<VariantValue>();
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
            var values = (_o as VariantValue[]) ?? Array.Empty<VariantValue>();
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
    internal string Signature
    {
        get
        {
            Span<byte> span = stackalloc byte[ProtocolConstants.MaxSignatureLength];
            return Encoding.UTF8.GetString(GetSignature(span));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetSignature(scoped Span<byte> buffer)
    {
        Debug.Assert(buffer.Length >= ProtocolConstants.MaxSignatureLength);

        int bytesWritten = AppendTypeSignature(buffer);
        return buffer.Slice(0, bytesWritten).ToArray();
    }

    private int AppendTypeSignature(Span<byte> signature)
    {
        VariantValueType type = Type;
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
                    VariantValue vv = (_o as VariantValue[])![0];
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

        static bool IsSimpleSignature(VariantValueType type)
            => type != VariantValueType.Array &&
               type != VariantValueType.Dictionary &&
               type != VariantValueType.Struct;
    }

    private static void ThrowTypeInvalid()
    {
        throw new ArgumentNullException();
    }
}
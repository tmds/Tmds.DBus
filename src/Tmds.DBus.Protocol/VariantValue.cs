namespace Tmds.DBus.Protocol;

public readonly struct VariantValue : IEquatable<VariantValue>
{
    private static readonly object Int64Type = VariantValueType.Int64;
    private static readonly object UInt64Type = VariantValueType.UInt64;
    private static readonly object DoubleType = VariantValueType.Double;
    private readonly object? _o;
    private readonly long    _l;

    private const int TypeShift = 8 * 7;
    private const int ArrayItemTypeShift = 8 * 0;
    private const int DictionaryKeyTypeShift = 8 * 0;
    private const int DictionaryValueTypeShift = 8 * 1;
    private const long StripTypeMask = ~(0xffL << TypeShift);

    private const long ArrayOfByte = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Byte << ArrayItemTypeShift);
    private const long ArrayOfInt16 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Int16 << ArrayItemTypeShift);
    private const long ArrayOfUInt16 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.UInt16 << ArrayItemTypeShift);
    private const long ArrayOfInt32 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Int32 << ArrayItemTypeShift);
    private const long ArrayOfUInt32 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.UInt32 << ArrayItemTypeShift);
    private const long ArrayOfInt64 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Int64 << ArrayItemTypeShift);
    private const long ArrayOfUInt64 = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.UInt64 << ArrayItemTypeShift);
    private const long ArrayOfDouble = ((long)VariantValueType.Array << TypeShift) | ((long)VariantValueType.Double << ArrayItemTypeShift);

    public VariantValueType Type
        => DetermineType();

    internal VariantValue(byte value)
    {
        _l = value | ((long)VariantValueType.Byte << TypeShift);
        _o = null;
    }
    internal VariantValue(bool value)
    {
        _l = (value ? 1L : 0) | ((long)VariantValueType.Bool << TypeShift);
        _o = null;
    }
    internal VariantValue(short value)
    {
        _l = (ushort)value | ((long)VariantValueType.Int16 << TypeShift);
        _o = null;
    }
    internal VariantValue(ushort value)
    {
        _l = value | ((long)VariantValueType.UInt16 << TypeShift);
        _o = null;
    }
    internal VariantValue(int value)
    {
        _l = (uint)value | ((long)VariantValueType.Int32 << TypeShift);
        _o = null;
    }
    internal VariantValue(uint value)
    {
        _l = value | ((long)VariantValueType.UInt32 << TypeShift);
        _o = null;
    }
    internal VariantValue(long value)
    {
        _l = value;
        _o = Int64Type;
    }
    internal VariantValue(ulong value)
    {
        _l = (long)value;
        _o = UInt64Type;
    }
    internal unsafe VariantValue(double value)
    {
        _l = *(long*)&value;
        _o = DoubleType;
    }
    internal VariantValue(string value)
    {
        _l = (long)VariantValueType.String << TypeShift;
        _o = value ?? throw new ArgumentNullException(nameof(value));
    }
    internal VariantValue(ObjectPath value)
    {
        _l = (long)VariantValueType.ObjectPath << TypeShift;
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    internal VariantValue(Signature value)
    {
        _l = (long)VariantValueType.Signature << TypeShift;
        string s = value.ToString();
        if (s.Length == 0)
        {
            throw new ArgumentException(nameof(value));
        }
        _o = s;
    }
    // Array
    internal VariantValue(VariantValueType itemType, VariantValue[] items)
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
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    // Array of Byte
    internal VariantValue(VariantValueType itemType, byte[] items)
    {
        Debug.Assert(itemType == VariantValueType.Byte);
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    // Array of Int16, UInt16
    internal VariantValue(VariantValueType itemType, short[] items)
    {
        Debug.Assert(itemType == VariantValueType.Int16 ||
                     itemType == VariantValueType.UInt16);
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    // Array of Int32, UInt32
    internal VariantValue(VariantValueType itemType, int[] items)
    {
        Debug.Assert(itemType == VariantValueType.Int32 ||
                     itemType == VariantValueType.UInt32);
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    // Array of Int64, UInt64, Double
    internal VariantValue(VariantValueType itemType, long[] items)
    {
        Debug.Assert(itemType == VariantValueType.Int64 ||
                     itemType == VariantValueType.UInt64 ||
                     itemType == VariantValueType.Double);
        _l = ((long)VariantValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    // Dictionary
    internal VariantValue(VariantValueType keyType, VariantValueType valueType, KeyValuePair<VariantValue, VariantValue>[] pairs)
    {
        _l = ((long)VariantValueType.Dictionary << TypeShift) |
             ((long)keyType << DictionaryKeyTypeShift) |
             ((long)valueType << DictionaryValueTypeShift);
        _o = pairs;
    }
    // Struct
    internal VariantValue(VariantValue[] fields)
    {
        _l = ((long)VariantValueType.Struct << TypeShift);
        _o = fields;
    }
    // UnixFd
    internal VariantValue(UnixFdCollection? fdCollection, int index)
    {
        _l = (long)index | ((long)VariantValueType.UnixFd << TypeShift);
        _o = fdCollection;
    }

    public byte GetByte()
    {
        EnsureTypeIs(VariantValueType.Byte);
        return (byte)(_l & StripTypeMask);
    }
    public bool GetBool()
    {
        EnsureTypeIs(VariantValueType.Bool);
        return (_l & StripTypeMask) != 0;
    }
    public short GetInt16()
    {
        EnsureTypeIs(VariantValueType.Int16);
        return (short)(_l & StripTypeMask);
    }
    public ushort GetUInt16()
    {
        EnsureTypeIs(VariantValueType.UInt16);
        return (ushort)(_l & StripTypeMask);
    }
    public int GetInt32()
    {
        EnsureTypeIs(VariantValueType.Int32);
        return (int)(_l & StripTypeMask);
    }
    public uint GetUInt32()
    {
        EnsureTypeIs(VariantValueType.UInt32);
        return (uint)(_l & StripTypeMask);
    }
    public long GetInt64()
    {
        EnsureTypeIs(VariantValueType.Int64);
        return _l;
    }
    public ulong GetUInt64()
    {
        EnsureTypeIs(VariantValueType.UInt64);
        return (ulong)(_l);
    }
    public unsafe double GetDouble()
    {
        EnsureTypeIs(VariantValueType.Double);
        double value;
        *(long*)&value = _l;
        return value;
    }
    public string GetString()
    {
        EnsureTypeIs(VariantValueType.String);
        return (_o as string)!;
    }
    public string GetObjectPath()
    {
        EnsureTypeIs(VariantValueType.ObjectPath);
        return (_o as string)!;
    }
    public string GetSignature()
    {
        EnsureTypeIs(VariantValueType.Signature);
        return (_o as string)!;
    }
    public T? ReadHandle<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    T>() where T : SafeHandle
    {
        EnsureTypeIs(VariantValueType.UnixFd);
        var handles = (UnixFdCollection?)_o;
        int index = (int)_l;
        return handles?.ReadHandle<T>(index);
    }

    // Use for Array, Struct and Dictionary.
    public int Count
    {
        get
        {
            Array? array = _o as Array;
            return array?.Length ?? -1;
        }
    }

    // Valid for Array, Struct.
    public VariantValue GetItem(int i)
    {
        if (Type == VariantValueType.Array)
        {
            switch (_l)
            {
                case ArrayOfByte:
                    return new VariantValue((_o as byte[])![i]);
                case ArrayOfInt16:
                    return new VariantValue((_o as short[])![i]);
                case ArrayOfUInt16:
                    return new VariantValue((ushort)((_o as short[])![i]));
                case ArrayOfInt32:
                    return new VariantValue((_o as int[])![i]);
                case ArrayOfUInt32:
                    return new VariantValue((uint)((_o as int[])![i]));
                case ArrayOfInt64:
                    return new VariantValue((_o as long[])![i]);
                case ArrayOfUInt64:
                    return new VariantValue((ulong)((_o as long[])![i]));
                case ArrayOfDouble:
                    return new VariantValue(ToDouble((_o as long[])![i]));
            }
        }
        var values = _o as VariantValue[];
        if (_o is null)
        {
            ThrowUnexpectedType([VariantValueType.Array, VariantValueType.Struct], Type);
        }
        return values![i];

        static unsafe double ToDouble(long l)
        {
            return *(double*)&l;
        }
    }

    // Valid for Dictionary.
    public KeyValuePair<VariantValue, VariantValue> GetDictionaryEntry(int i)
    {
        var values = _o as KeyValuePair<VariantValue, VariantValue>[];
        if (_o is null)
        {
            ThrowUnexpectedType(VariantValueType.Dictionary, Type);
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

    public VariantValueType ArrayItemType
        => DetermineInnerType(VariantValueType.Array, ArrayItemTypeShift);

    public VariantValueType DictionaryKeyType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryKeyTypeShift);

    public VariantValueType DictionaryValueType
        => DetermineInnerType(VariantValueType.Dictionary, DictionaryValueTypeShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTypeIs(VariantValueType expected)
    {
        VariantValueType actual = Type;
        if (actual != expected)
        {
            ThrowUnexpectedType(expected, actual);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValueType DetermineInnerType(VariantValueType outer, int typeShift)
    {
        VariantValueType type = DetermineType();
        return type == outer ? (VariantValueType)((_l >> typeShift) & 0xff) : VariantValueType.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VariantValueType DetermineType()
    {
        // For most types, we store the VariantValueType in the highest byte of the long.
        // Except for some types, like Int64, for which we store the value allocation free
        // in the long, and use the object field to store the type.
        VariantValueType type = (VariantValueType)(_l >> TypeShift);
        if (_o is not null)
        {
            if (_o.GetType() == typeof(VariantValueType))
            {
                type = (VariantValueType)_o;
            }
        }
        return type;
    }

    private void ThrowUnexpectedType(VariantValueType expected, VariantValueType actual)
        => ThrowUnexpectedType([ expected ], actual);

    private void ThrowUnexpectedType(VariantValueType[] expected, VariantValueType actual)
    {
        throw new InvalidOperationException($"Type {actual} can not be retrieved as {string.Join("/", expected)}.");
    }

    public override string ToString()
        => ToString(includeTypeSuffix: true);

    public string ToString(bool includeTypeSuffix)
    {
        // This is implemented so something user-friendly shows in the debugger.
        // By overriding the ToString method, it will also affect generic types like KeyValueType<TKey, TValue> that call ToString.
        VariantValueType type = Type;
        switch (type)
        {
            case VariantValueType.Byte:
                return $"{GetByte()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Bool:
                return $"{GetBool()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Int16:
                return $"{GetInt16()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.UInt16:
                return $"{GetUInt16()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Int32:
                return $"{GetInt32()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.UInt32:
                return $"{GetUInt32()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Int64:
                return $"{GetInt64()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.UInt64:
                return $"{GetUInt64()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Double:
                return $"{GetDouble()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.String:
                return $"{GetString()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.ObjectPath:
                return $"{GetObjectPath()}{TypeSuffix(includeTypeSuffix, type)}";
            case VariantValueType.Signature:
                return $"{GetSignature()}{TypeSuffix(includeTypeSuffix, type)}";

            case VariantValueType.Array:
                return $"[{nameof(VariantValueType.Array)}<{ArrayItemType}>, Count={Count}]";
            case VariantValueType.Struct:
                var values = (_o as VariantValue[]) ?? Array.Empty<VariantValue>();
                return $"({
                            string.Join(", ", values.Select(v => v.ToString(includeTypeSuffix: false)))
                          }){(
                            !includeTypeSuffix ? ""
                                : $" [{nameof(VariantValueType.Struct)}]<{
                                    string.Join(", ", values.Select(v => v.Type))
                                }>]")})";
            case VariantValueType.Dictionary:
                return $"[{nameof(VariantValueType.Dictionary)}<{DictionaryKeyType}, {DictionaryValueType}>, Count={Count}]";
            case VariantValueType.UnixFd:
                return $"[{nameof(VariantValueType.UnixFd)}]";

            case VariantValueType.Invalid:
                return $"[{nameof(VariantValueType.Invalid)}]";
            case VariantValueType.VariantValue: // note: No VariantValue returns this as its Type.
            default:
                return $"[?{Type}?]";
        }
    }

    static string TypeSuffix(bool includeTypeSuffix, VariantValueType type)
        => includeTypeSuffix ? $" [{type}]" : "";

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
        if (_l == other._l && object.ReferenceEquals(_o, other._o))
        {
            return true;
        }
        VariantValueType type = Type;
        if (type != other.Type)
        {
            return false;
        }
        switch (type)
        {
            case VariantValueType.String:
            case VariantValueType.ObjectPath:
            case VariantValueType.Signature:
                return (_o as string)!.Equals(other._o as string, StringComparison.Ordinal);
        }
        // Always return false for composite types and handles.
        return false;
    }
}
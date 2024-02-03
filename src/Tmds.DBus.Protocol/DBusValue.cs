namespace Tmds.DBus.Protocol;

public readonly struct DBusValue
{
    private static readonly object Int64Type = DBusValueType.Int64;
    private static readonly object UInt64Type = DBusValueType.UInt64;
    private static readonly object DoubleType = DBusValueType.Double;
    private readonly object? _o;
    private readonly long    _l;

    private const int TypeShift = 8 * 7;
    private const int ArrayItemTypeShift = 8 * 0;
    private const int DictionaryKeyTypeShift = 8 * 0;
    private const int DictionaryValueTypeShift = 8 * 0;
    private const long StripTypeMask = ~(0xff << TypeShift);

    public DBusValueType Type
        => DetermineType();

    internal DBusValue(byte value)
    {
        _l = value | ((long)DBusValueType.Byte << TypeShift);
        _o = null;
    }
    internal DBusValue(bool value)
    {
        _l = (value ? 1L : 0) | ((long)DBusValueType.Bool << TypeShift);
        _o = null;
    }
    internal DBusValue(short value)
    {
        _l = (long)value | ((long)DBusValueType.Int16 << TypeShift);
        _o = null;
    }
    internal DBusValue(ushort value)
    {
        _l = value | ((long)DBusValueType.UInt16 << TypeShift);
        _o = null;
    }
    internal DBusValue(int value)
    {
        _l = (long)value | ((long)DBusValueType.Int32 << TypeShift);
        _o = null;
    }
    internal DBusValue(uint value)
    {
        _l = value | ((long)DBusValueType.UInt32 << TypeShift);
        _o = null;
    }
    internal DBusValue(long value)
    {
        _l = value;
        _o = Int64Type;
    }
    internal DBusValue(ulong value)
    {
        _l = (long)value;
        _o = UInt64Type;
    }
    internal unsafe DBusValue(double value)
    {
        _l = *(long*)&value;
        _o = DoubleType;
    }
    internal DBusValue(string value)
    {
        _l = (long)DBusValueType.String << TypeShift;
        _o = value ?? "";
    }
    internal DBusValue(ObjectPath value)
    {
        _l = (long)DBusValueType.ObjectPath << TypeShift;
        _o = value.ToString();
    }
    internal DBusValue(Signature value)
    {
        _l = (long)DBusValueType.Signature << TypeShift;
        _o = value.ToString();
    }
    // Array
    internal DBusValue(DBusValueType itemType, DBusValue[] items)
    {
        _l = ((long)DBusValueType.Array << TypeShift) |
             ((long)itemType << ArrayItemTypeShift);
        _o = items;
    }
    // Dictionary
    internal DBusValue(DBusValueType keyType, DBusValueType valueType, KeyValuePair<DBusValue, DBusValue>[] pairs)
    {
        _l = ((long)DBusValueType.Dictionary << TypeShift) |
             ((long)keyType << DictionaryKeyTypeShift) |
             ((long)valueType << DictionaryValueTypeShift);
        _o = pairs;
    }
    // Struct
    internal DBusValue(DBusValue[] fields)
    {
        _l = ((long)DBusValueType.Struct << TypeShift);
        _o = fields;
    }
    // UnixFd
    internal DBusValue(UnixFdCollection? fdCollection, int index)
    {
        _l = (long)index | ((long)DBusValueType.UnixFd << TypeShift);
        _o = fdCollection;
    }

    public byte GetByte()
    {
        EnsureTypeIs(DBusValueType.Byte);
        return (byte)(_l & StripTypeMask);
    }
    public bool GetBool()
    {
        EnsureTypeIs(DBusValueType.Bool);
        return (_l & StripTypeMask) != 0;
    }
    public short GetInt16()
    {
        EnsureTypeIs(DBusValueType.Int16);
        return (short)(_l & StripTypeMask);
    }
    public ushort GetUInt16()
    {
        EnsureTypeIs(DBusValueType.UInt16);
        return (ushort)(_l & StripTypeMask);
    }
    public int GetInt32()
    {
        EnsureTypeIs(DBusValueType.Int32);
        return (int)(_l & StripTypeMask);
    }
    public uint GetUInt32()
    {
        EnsureTypeIs(DBusValueType.UInt32);
        return (uint)(_l & StripTypeMask);
    }
    public long GetInt64()
    {
        EnsureTypeIs(DBusValueType.Int64);
        return _l;
    }
    public ulong GetUInt64()
    {
        EnsureTypeIs(DBusValueType.UInt64);
        return (ulong)(_l);
    }
    public unsafe double GetDouble()
    {
        EnsureTypeIs(DBusValueType.Double);
        double value;
        *(long*)&value = _l;
        return value;
    }
    public string GetString()
    {
        EnsureTypeIs(DBusValueType.String);
        return (_o as string)!;
    }
    public string GetObjectPath()
    {
        EnsureTypeIs(DBusValueType.ObjectPath);
        return (_o as string)!;
    }
    public string GetSignature()
    {
        EnsureTypeIs(DBusValueType.Signature);
        return (_o as string)!;
    }
    public T? CreateHandle<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    T>() where T : SafeHandle
    {
        EnsureTypeIs(DBusValueType.UnixFd);
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
    public DBusValue GetItem(int i)
    {
        var values = _o as DBusValue[];
        if (_o is null)
        {
            ThrowUnexpectedType([DBusValueType.Array, DBusValueType.Struct], Type);
        }
        return values![i];
    }

    // Valid for Dictionary.
    public KeyValuePair<DBusValue, DBusValue> GetDictionaryEntry(int i)
    {
        var values = _o as KeyValuePair<DBusValue, DBusValue>[];
        if (_o is null)
        {
            ThrowUnexpectedType(DBusValueType.Dictionary, Type);
        }
        return values![i];
    }

    public DBusValueType ArrayItemType
        => DetermineInnerType(DBusValueType.Array, ArrayItemTypeShift);

    public DBusValueType DictionaryKeyType
        => DetermineInnerType(DBusValueType.Dictionary, DictionaryKeyTypeShift);

    public DBusValueType DictionaryValueType
        => DetermineInnerType(DBusValueType.Dictionary, DictionaryValueTypeShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTypeIs(DBusValueType expected)
    {
        DBusValueType actual = Type;
        if (actual != expected)
        {
            ThrowUnexpectedType(expected, actual);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DBusValueType DetermineInnerType(DBusValueType outer, int typeShift)
    {
        DBusValueType type = DetermineType();
        return type == outer ? (DBusValueType)((_l >> typeShift) & 0xff) : DBusValueType.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DBusValueType DetermineType()
    {
        // For most types, we store the DBusValueType in the highest byte of the long.
        // Except for some types, like Int64, for which we store the value allocation free
        // in the long, and use the object field to store the type.
        DBusValueType type = (DBusValueType)(_l >> TypeShift);
        if (_o is not null)
        {
            if (_o.GetType() == typeof(DBusValueType))
            {
                type = (DBusValueType)_o;
            }
        }
        return type;
    }

    private void ThrowUnexpectedType(DBusValueType expected, DBusValueType actual)
        => ThrowUnexpectedType([ expected ], actual);

    private void ThrowUnexpectedType(DBusValueType[] expected, DBusValueType actual)
    {
        throw new InvalidOperationException($"Type {actual} can not be retrieved as {string.Join("/", expected)}.");
    }

    public override string ToString()
        => ToString(includeTypeSuffix: true);

    public string ToString(bool includeTypeSuffix)
    {
        // This is implemented so something user-friendly shows in the debugger.
        // By overriding the ToString method, it will also affect generic types like KeyValueType<TKey, TValue> that call ToString.
        DBusValueType type = Type;
        switch (type)
        {
            case DBusValueType.Byte:
                return $"{GetByte()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.Bool:
                return $"{GetBool()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.Int16:
                return $"{GetInt16()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.UInt16:
                return $"{GetUInt16()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.Int32:
                return $"{GetInt32()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.UInt32:
                return $"{GetUInt32()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.Int64:
                return $"{GetInt64()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.UInt64:
                return $"{GetUInt64()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.Double:
                return $"{GetDouble()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.String:
                return $"{GetString()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.ObjectPath:
                return $"{GetObjectPath()}{TypeSuffix(includeTypeSuffix, type)}";
            case DBusValueType.Signature:
                return $"{GetSignature()}{TypeSuffix(includeTypeSuffix, type)}";

            case DBusValueType.Array:
                return $"[{nameof(DBusValueType.Array)}<{ArrayItemType}>, Count={Count}]";
            case DBusValueType.Struct:
                var values = (_o as DBusValue[]) ?? Array.Empty<DBusValue>();
                return $"({
                            string.Join(", ", values.Select(v => v.ToString(includeTypeSuffix: false)))
                          }){(
                            !includeTypeSuffix ? ""
                                : $" [{nameof(DBusValueType.Struct)}]<{
                                    string.Join(", ", values.Select(v => v.Type))
                                }>]")})";
            case DBusValueType.Dictionary:
                return $"[{nameof(DBusValueType.Dictionary)}<{DictionaryKeyType}, {DictionaryValueType}>, Count={Count}]";
            case DBusValueType.UnixFd:
                return $"[{nameof(DBusValueType.UnixFd)}]";

            case DBusValueType.Invalid:
                return $"[{nameof(DBusValueType.Invalid)}]";
            case DBusValueType.DBusValue: // note: No DBusValue returns this as its Type.
            default:
                return $"[?{Type}?]";
        }
    }

    static string TypeSuffix(bool includeTypeSuffix, DBusValueType type)
        => includeTypeSuffix ? $" [{type}]" : "";
}
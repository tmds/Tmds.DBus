using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public object ReadVariant() => Read<object>();

    public DBusValue ReadVariantAsDBusValue()
    {
        Utf8Span signature = ReadSignature();
        SignatureReader sigReader = new(signature);
        if (!sigReader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
        {
            ThrowInvalidSignature($"Invalid variant signature: {signature.ToString()}");
        }
        return ReadTypeAsDBusValue(type, innerSignature);
    }

    private DBusValue ReadTypeAsDBusValue(DBusType type, ReadOnlySpan<byte> innerSignature)
    {
        SignatureReader sigReader;
        switch (type)
        {
            case DBusType.Byte:
                return new DBusValue(ReadByte());
            case DBusType.Bool:
                return new DBusValue(ReadBool());
            case DBusType.Int16:
                return new DBusValue(ReadInt16());
            case DBusType.UInt16:
                return new DBusValue(ReadUInt16());
            case DBusType.Int32:
                return new DBusValue(ReadInt32());
            case DBusType.UInt32:
                return new DBusValue(ReadUInt32());
            case DBusType.Int64:
                return new DBusValue(ReadInt64());
            case DBusType.UInt64:
                return new DBusValue(ReadUInt64());
            case DBusType.Double:
                return new DBusValue(ReadDouble());
            case DBusType.String:
                return new DBusValue(ReadString());
            case DBusType.ObjectPath:
                return new DBusValue(ReadObjectPath());
            case DBusType.Signature:
                return new DBusValue(ReadSignatureAsSignature());
            case DBusType.UnixFd:
                int idx = (int)ReadUInt32();
                return new DBusValue(_handles, idx);
            case DBusType.Variant:
                return ReadVariantAsDBusValue();
            case DBusType.Array:
                sigReader = new(innerSignature);
                if (!sigReader.TryRead(out type, out innerSignature))
                {
                    ThrowInvalidSignature("Signature is missing array item type.");
                }
                bool isDictionary = type == DBusType.DictEntry;
                if (isDictionary)
                {
                    sigReader = new(innerSignature);
                    DBusType valueType = default;
                    ReadOnlySpan<byte> valueInnerSignature = default;
                    if (!sigReader.TryRead(out DBusType keyType, out ReadOnlySpan<byte> keyInnerSignature) ||
                        !sigReader.TryRead(out valueType, out valueInnerSignature))
                    {
                        ThrowInvalidSignature("Signature is missing dict entry types.");
                    }
                    List<KeyValuePair<DBusValue, DBusValue>> items = new();
                    ArrayEnd arrayEnd = ReadArrayStart(type);
                    while (HasNext(arrayEnd))
                    {
                        AlignStruct();
                        DBusValue key = ReadTypeAsDBusValue(keyType, keyInnerSignature);
                        DBusValue value = ReadTypeAsDBusValue(valueType, valueInnerSignature);
                        items.Add(new KeyValuePair<DBusValue, DBusValue>(key, value));
                    }
                    return new DBusValue(ToDBusValueType(keyType), ToDBusValueType(valueType), items.ToArray());
                }
                else
                {
                    List<DBusValue> items = new();
                    ArrayEnd arrayEnd = ReadArrayStart(type);
                    while (HasNext(arrayEnd))
                    {
                        DBusValue value = ReadTypeAsDBusValue(type, innerSignature);
                        items.Add(value);
                    }
                    return new DBusValue(ToDBusValueType(type), items.ToArray());
                }
            case DBusType.Struct:
                {
                    AlignStruct();
                    sigReader = new(innerSignature);
                    List<DBusValue> items = new();
                    while (sigReader.TryRead(out type, out innerSignature))
                    {
                        DBusValue value = ReadTypeAsDBusValue(type, innerSignature);
                        items.Add(value);
                    }
                    return new DBusValue(items.ToArray());
                }
            case DBusType.DictEntry: // Already handled under DBusType.Array.
            default:
                // note: the SignatureReader maps all unknown types to DBusType.Invalid
                //       so we won't see the actual character that caused it to fail.
                ThrowInvalidSignature($"Unexpected type in signature: {type}.");
                return default;
        }
    }

    private void ThrowInvalidSignature(string message)
    {
        throw new ProtocolException(message);
    }

    private static DBusValueType ToDBusValueType(DBusType type)
        => type switch
        {
            DBusType.Variant => DBusValueType.DBusValue,
            _ => (DBusValueType)type
        };
}

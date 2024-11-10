namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public VariantValue ReadVariantValue()
        => ReadVariantValue(nesting: 0);

    private VariantValue ReadVariantValue(byte nesting)
    {
        Utf8Span signature = ReadSignatureAsSpan();
        SignatureReader sigReader = new(signature);
        if (!sigReader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
        {
            ThrowInvalidSignature($"Invalid variant signature: {signature.ToString()}");
        }
        return ReadTypeAsVariantValue(type, innerSignature, nesting);
    }

    private VariantValue ReadTypeAsVariantValue(DBusType type, ReadOnlySpan<byte> innerSignature, byte nesting)
    {
        SignatureReader sigReader;
        switch (type)
        {
            case DBusType.Byte:
                return new VariantValue(ReadByte(), nesting);
            case DBusType.Bool:
                return new VariantValue(ReadBool(), nesting);
            case DBusType.Int16:
                return new VariantValue(ReadInt16(), nesting);
            case DBusType.UInt16:
                return new VariantValue(ReadUInt16(), nesting);
            case DBusType.Int32:
                return new VariantValue(ReadInt32(), nesting);
            case DBusType.UInt32:
                return new VariantValue(ReadUInt32(), nesting);
            case DBusType.Int64:
                return new VariantValue(ReadInt64(), nesting);
            case DBusType.UInt64:
                return new VariantValue(ReadUInt64(), nesting);
            case DBusType.Double:
                return new VariantValue(ReadDouble(), nesting);
            case DBusType.String:
                return new VariantValue(ReadString(), nesting);
            case DBusType.ObjectPath:
                return new VariantValue(ReadObjectPath(), nesting);
            case DBusType.Signature:
                return new VariantValue(ReadSignatureAsSignature(), nesting);
            case DBusType.UnixFd:
                int idx = (int)ReadUInt32();
                return new VariantValue(_handles, idx, nesting);
            case DBusType.Variant:
                nesting += 1;
                return ReadVariantValue(nesting);
            case DBusType.Array:
                ReadOnlySpan<byte> itemSignature = innerSignature;
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
                    List<KeyValuePair<VariantValue, VariantValue>> items = new();
                    ArrayEnd arrayEnd = ReadArrayStart(type);
                    while (HasNext(arrayEnd))
                    {
                        AlignStruct();
                        VariantValue key = ReadTypeAsVariantValue(keyType, keyInnerSignature, nesting: 0);
                        VariantValue value = valueType == DBusType.Variant
                                                ? ReadVariantValue() // unwrap
                                                : ReadTypeAsVariantValue(valueType, valueInnerSignature, nesting: 0);
                        items.Add(new KeyValuePair<VariantValue, VariantValue>(key, value));
                    }
                    ReadOnlySpan<byte> valueSignature = itemSignature.Slice(2, itemSignature.Length - 3);
                    return new VariantValue(ToVariantValueType(keyType), ToVariantValueType(valueType), VariantValue.GetSignatureObject(items.Count, valueSignature), items.ToArray(), nesting);
                }
                else
                {
                    if (type == DBusType.Byte)
                    {
                        return new VariantValue(ReadArrayOfByte(), nesting);
                    }
                    else if (type == DBusType.Int16)
                    {
                        return new VariantValue(ReadArrayOfInt16(), nesting);
                    }
                    else if (type == DBusType.UInt16)
                    {
                        return new VariantValue(ReadArrayOfUInt16(), nesting);
                    }
                    else if (type == DBusType.Int32)
                    {
                        return new VariantValue(ReadArrayOfInt32(), nesting);
                    }
                    else if (type == DBusType.UInt32)
                    {
                        return new VariantValue(ReadArrayOfUInt32(), nesting);
                    }
                    else if (type == DBusType.Int64)
                    {
                        return new VariantValue(ReadArrayOfInt64(), nesting);
                    }
                    else if (type == DBusType.UInt64)
                    {
                        return new VariantValue(ReadArrayOfUInt64(), nesting);
                    }
                    else if (type == DBusType.Double)
                    {
                        return new VariantValue(ReadArrayOfDouble(), nesting);
                    }
                    else if (type == DBusType.String ||
                             type == DBusType.ObjectPath)
                    {
                        return new VariantValue(ToVariantValueType(type), ReadArrayOfString(), nesting);
                    }
                    else
                    {
                        List<VariantValue> items = new();
                        ArrayEnd arrayEnd = ReadArrayStart(type);
                        while (HasNext(arrayEnd))
                        {
                            VariantValue value = type == DBusType.Variant
                                                    ? ReadVariantValue() // unwrap
                                                    : ReadTypeAsVariantValue(type, innerSignature, nesting: 0);
                            items.Add(value);
                        }
                        return new VariantValue(ToVariantValueType(type), VariantValue.GetSignatureObject(items.Count, itemSignature), items.ToArray(), nesting);
                    }
                }
            case DBusType.Struct:
                {
                    AlignStruct();
                    sigReader = new(innerSignature);
                    List<VariantValue> items = new();
                    long variantMask = 0;
                    int i = 0;
                    while (sigReader.TryRead(out type, out innerSignature))
                    {
                        if (i > VariantValue.MaxStructFields)
                        {
                            VariantValue.ThrowMaxStructFieldsExceeded();
                        }
                        variantMask <<= 1;
                        VariantValue value;
                        if (type == DBusType.Variant)
                        {
                            variantMask |= (1L << i);
                            value = ReadVariantValue(); // unwrap
                        }
                        else
                        {
                            value = ReadTypeAsVariantValue(type, innerSignature, nesting: 0);
                        }
                        items.Add(value);
                        i++;
                    }
                    return new VariantValue(variantMask, items.ToArray(), nesting);
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

    private static VariantValueType ToVariantValueType(DBusType type)
        => (VariantValueType)type;
}

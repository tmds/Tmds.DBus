namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(DBusType.Struct);
        foreach (var item in value)
        {
            WriteStructureStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteArrayEnd(arrayStart);
    }

    public void WriteDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(DBusType.Struct);
        foreach (var item in value)
        {
            WriteStructureStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteArrayEnd(arrayStart);
    }

    public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteArrayStart(DBusType.Struct);
        foreach (var item in value)
        {
            WriteStructureStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteArrayEnd(arrayStart);
    }

    public void WriteVariantDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> value)
        where TKey : notnull
        where TValue : notnull
    {
        WriteDictionarySignature<TKey, TValue>(ref this);
        WriteDictionary(value);
    }

    public void WriteVariantDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] value)
        where TKey : notnull
        where TValue : notnull
    {
        WriteDictionarySignature<TKey, TValue>(ref this);
        WriteDictionary(value);
    }

    public void WriteVariantDictionary<TKey, TValue>(Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        WriteDictionarySignature<TKey, TValue>(ref this);
        WriteDictionary(value);
    }

    // This method writes a Dictionary without using generics at the 'cost' of boxing.
    // private void WriteDictionary(IDictionary value)
    // {
    //     ArrayStart arrayStart = WriteArrayStart(DBusType.Struct);
    //     foreach (System.Collections.DictionaryEntry de in dictionary)
    //     {
    //         WriteStructureStart();
    //         Write(de.Key, asVariant: keyType == typeof(object));
    //         Write(de.Value, asVariant: valueType == typeof(object));
    //     }
    //     WriteArrayEnd(ref arrayStart);
    // }

    private static void WriteDictionarySignature<TKey, TValue>(ref MessageWriter writer) where TKey : notnull where TValue : notnull
    {
        Span<byte> buffer = stackalloc byte[ProtocolConstants.MaxSignatureLength];
        writer.WriteSignature(TypeModel.GetSignature<Dict<TKey, TValue>>(buffer));
    }
}

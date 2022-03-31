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

    sealed class DictionaryTypeWriter<TKey, TValue> : ITypeWriter<IEnumerable<KeyValuePair<TKey, TValue>>>
        where TKey : notnull
        where TValue : notnull
    {
        public void Write(ref MessageWriter writer, IEnumerable<KeyValuePair<TKey, TValue>> value)
        {
            writer.WriteDictionary(value);
        }

        public void WriteVariant(ref MessageWriter writer, object value)
        {
            WriteDictionarySignature<TKey, TValue>(ref writer);
            writer.WriteDictionary((IEnumerable<KeyValuePair<TKey, TValue>>)value);
        }
    }

    public static void AddDictionaryTypeWriter<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    {
        lock (_typeWriters)
        {
            Type keyType = typeof(IEnumerable<KeyValuePair<TKey, TValue>>);
            if (!_typeWriters.ContainsKey(keyType))
            {
                _typeWriters.Add(keyType, new DictionaryTypeWriter<TKey, TValue>());
            }
        }
    }

    private ITypeWriter CreateDictionaryTypeWriter(Type keyType, Type valueType)
    {
        Type writerType = typeof(DictionaryTypeWriter<,>).MakeGenericType(new[] { keyType, valueType });
        return (ITypeWriter)Activator.CreateInstance(writerType)!;
    }

    private static void WriteDictionarySignature<TKey, TValue>(ref MessageWriter writer)
    {
        writer.WriteSignature(TypeModel.GetSignature<IDictionary<TKey, TValue>>());
    }
}

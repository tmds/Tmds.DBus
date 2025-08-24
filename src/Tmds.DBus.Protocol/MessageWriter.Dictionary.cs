namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public ArrayStart WriteDictionaryStart()
        => WriteArrayStart(ProtocolConstants.StructAlignment);

    public void WriteDictionaryEnd(ArrayStart start)
        => WriteArrayEnd(start);

    public void WriteDictionaryEntryStart()
        => WriteStructureStart();

    // Write method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(IEnumerable<KeyValuePair<string, VariantValue>> value)
        => WriteDictionary<string, VariantValue>(value);

    // Write method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(KeyValuePair<string, VariantValue>[] value)
        => WriteDictionary<string, VariantValue>(value);

    // Write method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(Dictionary<string, VariantValue> value)
        => WriteDictionary<string, VariantValue>(value);

    private void WriteDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    private void WriteDictionary<TKey, TValue>(KeyValuePair<TKey, TValue>[] value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }

    internal void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> value)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayStart arrayStart = WriteDictionaryStart();
        foreach (var item in value)
        {
            WriteDictionaryEntryStart();
            Write<TKey>(item.Key);
            Write<TValue>(item.Value);
        }
        WriteDictionaryEnd(arrayStart);
    }
}

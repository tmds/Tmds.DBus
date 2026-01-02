namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    /// <summary>
    /// Writes the start of a dictionary.
    /// </summary>
    /// <returns>An <see cref="ArrayStart"/> token to pass to <see cref="WriteDictionaryEnd"/>.</returns>
    public ArrayStart WriteDictionaryStart()
        => WriteArrayStart(ProtocolConstants.StructAlignment);

    /// <summary>
    /// Writes the end of a dictionary.
    /// </summary>
    /// <param name="start">The <see cref="ArrayStart"/> token returned by <see cref="WriteDictionaryStart"/>.</param>
    public void WriteDictionaryEnd(ArrayStart start)
        => WriteArrayEnd(start);

    /// <summary>
    /// Writes the start of a dictionary entry.
    /// </summary>
    public void WriteDictionaryEntryStart()
        => WriteStructureStart();

    /// <summary>
    /// Writes a dictionary with string keys and variant values.
    /// </summary>
    /// <param name="value">The dictionary entries to write.</param>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(IEnumerable<KeyValuePair<string, VariantValue>> value)
        => WriteDictionary<string, VariantValue>(value);

    /// <summary>
    /// Writes a dictionary with string keys and variant values.
    /// </summary>
    /// <param name="value">The dictionary entries to write.</param>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call WriteDictionary with these types.
    public void WriteDictionary(KeyValuePair<string, VariantValue>[] value)
        => WriteDictionary<string, VariantValue>(value);

    /// <summary>
    /// Writes a dictionary with string keys and variant values.
    /// </summary>
    /// <param name="value">The dictionary to write.</param>
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

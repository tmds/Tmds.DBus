namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    /// <summary>
    /// Reads the start of a dictionary and returns a position for detecting the end.
    /// </summary>
    public ArrayEnd ReadDictionaryStart()
        => ReadArrayStart(ProtocolConstants.StructAlignment);

    /// <summary>
    /// Reads a dictionary with <see cref="string"/> keys and <see cref="VariantValue"/> values.
    /// </summary>
    /// <remarks>This is a helper method for reading the common 'a{sv}' D-Bus type.</remarks>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call ReadDictionary with these types.
    public Dictionary<string, VariantValue> ReadDictionaryOfStringToVariantValue()
        => ReadDictionary<string, VariantValue>();

    private Dictionary<TKey, TValue> ReadDictionary
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
        >
        ()
        where TKey : notnull
        where TValue : notnull
            => ReadDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

    internal Dictionary<TKey, TValue> ReadDictionary
        <
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
        >
        (Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayEnd dictEnd = ReadDictionaryStart();
        while (HasNext(dictEnd))
        {
            var key = Read<TKey>();
            var value = Read<TValue>();
            // Use the indexer to avoid throwing if the key is present multiple times.
            dictionary[key] = value;
        }
        return dictionary;
    }
}

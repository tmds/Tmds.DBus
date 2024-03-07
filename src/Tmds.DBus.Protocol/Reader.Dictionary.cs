namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public ArrayEnd ReadDictionaryStart()
        => ReadArrayStart(DBusType.Struct);

    // Read method for the common 'a{sv}' type.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // It's safe to call ReadDictionary with these types.
    public Dictionary<string, VariantValue> ReadDictionaryOfStringToVariantValue()
        => ReadDictionary<string, VariantValue>();

    [RequiresUnreferencedCode(Strings.UseNonGenericReadDictionary)]
    public Dictionary<TKey, TValue> ReadDictionary
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
        ArrayEnd headersEnd = ReadDictionaryStart();
        while (HasNext(headersEnd))
        {
            var key = Read<TKey>();
            var value = Read<TValue>();
            // Use the indexer to avoid throwing if the key is present multiple times.
            dictionary[key] = value;
        }
        return dictionary;
    }
}

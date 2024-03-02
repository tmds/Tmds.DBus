namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
        => ReadDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

    internal Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : notnull
    {
        ArrayEnd headersEnd = ReadArrayStart(DBusType.Struct);
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

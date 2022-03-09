using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    {
        Dictionary<TKey, TValue> dictionary = new();
        ArrayEnd headersEnd = ReadArrayStart(DBusType.Struct);
        while (HasNext(headersEnd))
        {
            var key = Read<TKey>();
            var value = Read<TValue>();
            dictionary.Add(key, value);
        }
        return dictionary;
    }

    sealed class DictionaryTypeReader<TKey, TValue> : ITypeReader<Dictionary<TKey, TValue>>, ITypeReader<object>
        where TKey : notnull
        where TValue : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public Dictionary<TKey, TValue> Read(ref Reader reader)
        {
            return reader.ReadDictionary<TKey, TValue>();
        }
    }

    public static void AddDictionaryTypeReader<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(Dictionary<TKey, TValue>);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new DictionaryTypeReader<TKey, TValue>());
            }
        }
    }

    private ITypeReader CreateDictionaryTypeReader(Type keyType, Type valueType)
    {
        Type readerType = typeof(DictionaryTypeReader<,>).MakeGenericType(new[] { keyType, valueType });
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }
}

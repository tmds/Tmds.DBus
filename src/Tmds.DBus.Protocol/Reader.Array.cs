using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public T[] ReadArray<T>()
    {
        List<T> items = new();
        ArrayEnd headersEnd = ReadArrayStart(TypeModel.GetTypeAlignment<T>());
        while (HasNext(headersEnd))
        {
            items.Add(Read<T>());
        }
        return items.ToArray();
    }

    private KeyValuePair<TKey, TValue>[] ReadKeyValueArray<TKey, TValue>()
    {
        List<KeyValuePair<TKey, TValue>> items = new();
        ArrayEnd headersEnd = ReadArrayStart(DBusType.Struct);
        while (HasNext(headersEnd))
        {
            TKey key = Read<TKey>();
            TValue value = Read<TValue>();
            items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }
        return items.ToArray();
    }

    sealed class KeyValueArrayTypeReader<TKey, TValue> : ITypeReader<KeyValuePair<TKey, TValue>[]>, ITypeReader<object>
        where TKey : notnull
        where TValue : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public KeyValuePair<TKey, TValue>[] Read(ref Reader reader)
        {
            return reader.ReadKeyValueArray<TKey, TValue>();
        }
    }

    sealed class ArrayTypeReader<T> : ITypeReader<T[]>, ITypeReader<object>
        where T : notnull
    {
        object ITypeReader<object>.Read(ref Reader reader) => Read(ref reader);

        public T[] Read(ref Reader reader)
        {
            return reader.ReadArray<T>();
        }
    }

    public static void AddArrayTypeReader<T>()
        where T : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(T[]);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new ArrayTypeReader<T>());
            }
        }
    }

    public static void AddKeyValueArrayTypeReader<TKey, TValue>()
        where TKey : notnull
        where TValue : notnull
    {
        lock (_typeReaders)
        {
            Type keyType = typeof(KeyValuePair<TKey, TValue>[]);
            if (!_typeReaders.ContainsKey(keyType))
            {
                _typeReaders.Add(keyType, new KeyValueArrayTypeReader<TKey, TValue>());
            }
        }
    }

    private ITypeReader CreateArrayTypeReader(Type elementType)
    {
        Type readerType;
        if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            Type keyType = elementType.GenericTypeArguments[0];
            Type valueType = elementType.GenericTypeArguments[1];
            readerType = typeof(KeyValueArrayTypeReader<,>).MakeGenericType(new[] { keyType, valueType });
        }
        else
        {
            readerType = typeof(ArrayTypeReader<>).MakeGenericType(new[] { elementType });
        }
        return (ITypeReader)Activator.CreateInstance(readerType)!;
    }
}

namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    interface ITypeWriter
    {
        void WriteVariant(ref MessageWriter writer, object value);
    }

    interface ITypeWriter<in T> : ITypeWriter
    {
        void Write(ref MessageWriter writer, T value);
    }

    private static readonly Dictionary<Type, ITypeWriter> _typeWriters = new();

    private void WriteDynamic<T>(T value) where T : notnull
    {
        if (typeof(T) == typeof(object))
        {
            WriteVariant((object)value);
            return;
        }

        var typeWriter = (ITypeWriter<T>)GetTypeWriter(typeof(T));
        typeWriter.Write(ref this, value);
    }

    private ITypeWriter GetTypeWriter(Type type)
    {
        lock (_typeWriters)
        {
            if (_typeWriters.TryGetValue(type, out ITypeWriter? writer))
            {
                return writer;
            }
            writer = CreateWriterForType(type);
            _typeWriters.Add(type, writer);
            return writer;
        }
    }

    private ITypeWriter CreateWriterForType(Type type)
    {
        // Struct (ValueTuple)
        if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple"))
        {
            switch (type.GenericTypeArguments.Length)
            {
                case 1: return CreateValueTupleTypeWriter(type.GenericTypeArguments[0]);
                case 2:
                    return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1]);
                case 3:
                    return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1],
                                                      type.GenericTypeArguments[2]);
                case 4:
                    return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1],
                                                      type.GenericTypeArguments[2],
                                                      type.GenericTypeArguments[3]);
                case 5:
                    return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1],
                                                      type.GenericTypeArguments[2],
                                                      type.GenericTypeArguments[3],
                                                      type.GenericTypeArguments[4]);

                case 6:
                    return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4],
                                                 type.GenericTypeArguments[5]);
                case 7:
                    return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4],
                                                 type.GenericTypeArguments[5],
                                                 type.GenericTypeArguments[6]);
                case 8:
                    switch (type.GenericTypeArguments[7].GenericTypeArguments.Length)
                    {
                        case 1:
                            return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                         type.GenericTypeArguments[1],
                                                         type.GenericTypeArguments[2],
                                                         type.GenericTypeArguments[3],
                                                         type.GenericTypeArguments[4],
                                                         type.GenericTypeArguments[5],
                                                         type.GenericTypeArguments[6],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[0]);
                        case 2:
                            return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                         type.GenericTypeArguments[1],
                                                         type.GenericTypeArguments[2],
                                                         type.GenericTypeArguments[3],
                                                         type.GenericTypeArguments[4],
                                                         type.GenericTypeArguments[5],
                                                         type.GenericTypeArguments[6],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[1]);
                        case 3:
                            return CreateValueTupleTypeWriter(type.GenericTypeArguments[0],
                                                         type.GenericTypeArguments[1],
                                                         type.GenericTypeArguments[2],
                                                         type.GenericTypeArguments[3],
                                                         type.GenericTypeArguments[4],
                                                         type.GenericTypeArguments[5],
                                                         type.GenericTypeArguments[6],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[1],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[2]);
                    }
                    break;
            }
        }
        // Struct (ValueTuple)
        if (type.IsGenericType && type.FullName!.StartsWith("System.Tuple"))
        {
            switch (type.GenericTypeArguments.Length)
            {
                case 1: return CreateTupleTypeWriter(type.GenericTypeArguments[0]);
                case 2:
                    return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1]);
                case 3:
                    return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2]);
                case 4:
                    return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3]);
                case 5:
                    return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4]);
                case 6:
                    return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4],
                                                 type.GenericTypeArguments[5]);
                case 7:
                    return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4],
                                                 type.GenericTypeArguments[5],
                                                 type.GenericTypeArguments[6]);
                case 8:
                    switch (type.GenericTypeArguments[7].GenericTypeArguments.Length)
                    {
                        case 1:
                            return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                         type.GenericTypeArguments[1],
                                                         type.GenericTypeArguments[2],
                                                         type.GenericTypeArguments[3],
                                                         type.GenericTypeArguments[4],
                                                         type.GenericTypeArguments[5],
                                                         type.GenericTypeArguments[6],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[0]);
                        case 2:
                            return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                         type.GenericTypeArguments[1],
                                                         type.GenericTypeArguments[2],
                                                         type.GenericTypeArguments[3],
                                                         type.GenericTypeArguments[4],
                                                         type.GenericTypeArguments[5],
                                                         type.GenericTypeArguments[6],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[1]);
                        case 3:
                            return CreateTupleTypeWriter(type.GenericTypeArguments[0],
                                                         type.GenericTypeArguments[1],
                                                         type.GenericTypeArguments[2],
                                                         type.GenericTypeArguments[3],
                                                         type.GenericTypeArguments[4],
                                                         type.GenericTypeArguments[5],
                                                         type.GenericTypeArguments[6],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[1],
                                                         type.GenericTypeArguments[7].GenericTypeArguments[2]);
                    }
                    break;
            }
        }

        // Array/Dictionary type (IEnumerable<>/IEnumerable<KeyValuePair<,>>)
        Type? extractedType = TypeModel.ExtractGenericInterface(type, typeof(IEnumerable<>));
        if (extractedType != null)
        {
            if (_typeWriters.TryGetValue(extractedType, out ITypeWriter? writer))
            {
                return writer;
            }

            Type elementType = extractedType.GenericTypeArguments[0];
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                Type keyType = elementType.GenericTypeArguments[0];
                Type valueType = elementType.GenericTypeArguments[1];
                writer = CreateDictionaryTypeWriter(keyType, valueType);
            }
            else
            {
                writer = CreateArrayTypeWriter(elementType);
            }

            if (type != extractedType)
            {
                _typeWriters.Add(extractedType, writer);
            }

            return writer;
        }

        ThrowNotSupportedType(type);
        return default!;
    }
}

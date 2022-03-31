using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    interface ITypeReader
    { }

    interface ITypeReader<T> : ITypeReader
    {
        T Read(ref Reader reader);
    }

    private static readonly Dictionary<Type, ITypeReader> _typeReaders = new();

    public object ReadVariant() => Read<object>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T Read<T>()
    {
        Type type = typeof(T);

        if (type == typeof(object))
        {
            Utf8Span signature = ReadSignature();
            type = TypeModel.DetermineVariantType(signature);
        }

        if (type == typeof(byte))
        {
            return (T)(object)ReadByte();
        }
        else if (type == typeof(bool))
        {
            return (T)(object)ReadBool();
        }
        else if (type == typeof(Int16))
        {
            return (T)(object)ReadInt16();
        }
        else if (type == typeof(UInt16))
        {
            return (T)(object)ReadUInt16();
        }
        else if (type == typeof(Int32))
        {
            return (T)(object)ReadInt32();
        }
        else if (type == typeof(UInt32))
        {
            return (T)(object)ReadUInt32();
        }
        else if (type == typeof(Int64))
        {
            return (T)(object)ReadInt64();
        }
        else if (type == typeof(UInt64))
        {
            return (T)(object)ReadUInt64();
        }
        else if (type == typeof(Double))
        {
            return (T)(object)ReadDouble();
        }
        else if (type == typeof(string))
        {
            return (T)(object)ReadString();
        }
        else if (type == typeof(ObjectPath))
        {
            return (T)(object)ReadObjectPath();
        }
        else if (type == typeof(Signature))
        {
            return (T)(object)ReadSignatureAsSignature();
        }
        else
        {
            var typeReader = (ITypeReader<T>)GetTypeReader(type);
            return typeReader.Read(ref this);
        }
    }

    private ITypeReader GetTypeReader(Type type)
    {
        lock (_typeReaders)
        {
            if (_typeReaders.TryGetValue(type, out ITypeReader? reader))
            {
                return reader;
            }
            reader = CreateReaderForType(type);
            _typeReaders.Add(type, reader);
            return reader;
        }
    }

    private ITypeReader CreateReaderForType(Type type)
    {
        // Array
        if (type.IsArray)
        {
            return CreateArrayTypeReader(type.GetElementType()!);
        }

        // Dictionary<.>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type keyType = type.GenericTypeArguments[0];
            Type valueType = type.GenericTypeArguments[1];
            return CreateDictionaryTypeReader(keyType, valueType);
        }

        // Struct (ValueTuple)
        if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple"))
        {
            switch (type.GenericTypeArguments.Length)
            {
                case 1: return CreateValueTupleTypeReader(type.GenericTypeArguments[0]);
                case 2:
                    return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1]);
                case 3:
                    return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1],
                                                      type.GenericTypeArguments[2]);
                case 4:
                    return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1],
                                                      type.GenericTypeArguments[2],
                                                      type.GenericTypeArguments[3]);
                case 5:
                    return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                      type.GenericTypeArguments[1],
                                                      type.GenericTypeArguments[2],
                                                      type.GenericTypeArguments[3],
                                                      type.GenericTypeArguments[4]);

                case 6:
                    return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4],
                                                 type.GenericTypeArguments[5]);
                case 7:
                    return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
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
                            return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                        type.GenericTypeArguments[1],
                                                        type.GenericTypeArguments[2],
                                                        type.GenericTypeArguments[3],
                                                        type.GenericTypeArguments[4],
                                                        type.GenericTypeArguments[5],
                                                        type.GenericTypeArguments[6],
                                                        type.GenericTypeArguments[7].GenericTypeArguments[0]);
                        case 2:
                            return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
                                                        type.GenericTypeArguments[1],
                                                        type.GenericTypeArguments[2],
                                                        type.GenericTypeArguments[3],
                                                        type.GenericTypeArguments[4],
                                                        type.GenericTypeArguments[5],
                                                        type.GenericTypeArguments[6],
                                                        type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                        type.GenericTypeArguments[7].GenericTypeArguments[1]);
                        case 3:
                            return CreateValueTupleTypeReader(type.GenericTypeArguments[0],
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
                case 1: return CreateTupleTypeReader(type.GenericTypeArguments[0]);
                case 2:
                    return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1]);
                case 3:
                    return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2]);
                case 4:
                    return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3]);
                case 5:
                    return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4]);
                case 6:
                    return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                 type.GenericTypeArguments[1],
                                                 type.GenericTypeArguments[2],
                                                 type.GenericTypeArguments[3],
                                                 type.GenericTypeArguments[4],
                                                 type.GenericTypeArguments[5]);
                case 7:
                    return CreateTupleTypeReader(type.GenericTypeArguments[0],
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
                            return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                        type.GenericTypeArguments[1],
                                                        type.GenericTypeArguments[2],
                                                        type.GenericTypeArguments[3],
                                                        type.GenericTypeArguments[4],
                                                        type.GenericTypeArguments[5],
                                                        type.GenericTypeArguments[6],
                                                        type.GenericTypeArguments[7].GenericTypeArguments[0]);
                        case 2:
                            return CreateTupleTypeReader(type.GenericTypeArguments[0],
                                                        type.GenericTypeArguments[1],
                                                        type.GenericTypeArguments[2],
                                                        type.GenericTypeArguments[3],
                                                        type.GenericTypeArguments[4],
                                                        type.GenericTypeArguments[5],
                                                        type.GenericTypeArguments[6],
                                                        type.GenericTypeArguments[7].GenericTypeArguments[0],
                                                        type.GenericTypeArguments[7].GenericTypeArguments[1]);
                        case 3:
                            return CreateTupleTypeReader(type.GenericTypeArguments[0],
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

        ThrowNotSupportedType(type);
        return default!;
    }

    private static void ThrowNotSupportedType(Type type)
    {
        throw new NotSupportedException($"Cannot read type {type.FullName}");
    }
}

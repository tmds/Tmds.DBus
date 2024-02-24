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

    private T ReadDynamic<T>()
    {
        Type type = typeof(T);

        if (type == typeof(object))
        {
            Utf8Span signature = ReadSignature();
            type = DetermineVariantType(signature);

            if (type == typeof(byte))
            {
                return (T)(object)ReadByte();
            }
            else if (type == typeof(bool))
            {
                return (T)(object)ReadBool();
            }
            else if (type == typeof(short))
            {
                return (T)(object)ReadInt16();
            }
            else if (type == typeof(ushort))
            {
                return (T)(object)ReadUInt16();
            }
            else if (type == typeof(int))
            {
                return (T)(object)ReadInt32();
            }
            else if (type == typeof(uint))
            {
                return (T)(object)ReadUInt32();
            }
            else if (type == typeof(long))
            {
                return (T)(object)ReadInt64();
            }
            else if (type == typeof(ulong))
            {
                return (T)(object)ReadUInt64();
            }
            else if (type == typeof(double))
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
            else if (type == typeof(SafeHandle))
            {
                return (T)(object)ReadHandle<CloseSafeHandle>()!;
            }
            else if (type == typeof(VariantValue))
            {
                return (T)(object)ReadVariantValue();
            }
        }

        var typeReader = (ITypeReader<T>)GetTypeReader(type);
        return typeReader.Read(ref this);
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

    public static Type DetermineVariantType(Utf8Span signature)
    {
        Func<DBusType, Type[], Type> map = (dbusType, innerTypes) =>
        {
            switch (dbusType)
            {
                case DBusType.Byte: return typeof(byte);
                case DBusType.Bool: return typeof(bool);
                case DBusType.Int16: return typeof(short);
                case DBusType.UInt16: return typeof(ushort);
                case DBusType.Int32: return typeof(int);
                case DBusType.UInt32: return typeof(uint);
                case DBusType.Int64: return typeof(long);
                case DBusType.UInt64: return typeof(ulong);
                case DBusType.Double: return typeof(double);
                case DBusType.String: return typeof(string);
                case DBusType.ObjectPath: return typeof(ObjectPath);
                case DBusType.Signature: return typeof(Signature);
                case DBusType.UnixFd: return typeof(SafeHandle);
                case DBusType.Array: return innerTypes[0].MakeArrayType();
                case DBusType.DictEntry: return typeof(Dictionary<,>).MakeGenericType(innerTypes);
                case DBusType.Struct:
                    switch (innerTypes.Length)
                    {
                        case 1: return typeof(ValueTuple<>).MakeGenericType(innerTypes);
                        case 2: return typeof(ValueTuple<,>).MakeGenericType(innerTypes);
                        case 3: return typeof(ValueTuple<,,>).MakeGenericType(innerTypes);
                        case 4: return typeof(ValueTuple<,,,>).MakeGenericType(innerTypes);
                        case 5: return typeof(ValueTuple<,,,,>).MakeGenericType(innerTypes);
                        case 6: return typeof(ValueTuple<,,,,,>).MakeGenericType(innerTypes);
                        case 7: return typeof(ValueTuple<,,,,,,>).MakeGenericType(innerTypes);
                        case 8:
                        case 9:
                        case 10:
                            var types = new Type[8];
                            innerTypes.AsSpan(0, 7).CopyTo(types);
                            types[7] = innerTypes.Length switch
                            {
                                8 => typeof(ValueTuple<>).MakeGenericType(new[] { innerTypes[7] }),
                                9 => typeof(ValueTuple<,>).MakeGenericType(new[] { innerTypes[7], innerTypes[8] }),
                                10 => typeof(ValueTuple<,,>).MakeGenericType(new[] { innerTypes[7], innerTypes[8], innerTypes[9] }),
                                _ => null!
                            };
                            return typeof(ValueTuple<,,,,,,,>).MakeGenericType(types);
                    }
                    break;
            }
            return typeof(object);
        };

        // TODO (perf): add caching.
        return SignatureReader.Transform(signature, map);
    }
}

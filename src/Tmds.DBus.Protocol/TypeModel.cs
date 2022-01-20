namespace Tmds.DBus.Protocol;

static class TypeModel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DBusType GetTypeAlignment<T>()
    {
        // TODO: add caching.
        if (typeof(T) == typeof(object))
        {
            return DBusType.Variant;
        }
        else if (typeof(T) == typeof(byte))
        {
            return DBusType.Byte;
        }
        else if (typeof(T) == typeof(bool))
        {
            return DBusType.Bool;
        }
        else if (typeof(T) == typeof(Int16))
        {
            return DBusType.Int16;
        }
        else if (typeof(T) == typeof(UInt16))
        {
            return DBusType.UInt16;
        }
        else if (typeof(T) == typeof(Int32))
        {
            return DBusType.Int32;
        }
        else if (typeof(T) == typeof(UInt32))
        {
            return DBusType.UInt32;
        }
        else if (typeof(T) == typeof(Int64))
        {
            return DBusType.Int64;
        }
        else if (typeof(T) == typeof(UInt64))
        {
            return DBusType.UInt64;
        }
        else if (typeof(T) == typeof(Double))
        {
            return DBusType.Double;
        }
        else if (typeof(T) == typeof(string))
        {
            return DBusType.String;
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return DBusType.ObjectPath;
        }
        else if (typeof(T) == typeof(Signature))
        {
            return DBusType.Signature;
        }
        else if (typeof(T).IsArray)
        {
            return DBusType.Array;
        }
        else if (ExtractGenericInterface(typeof(T), typeof(System.Collections.Generic.IEnumerable<>)) != null)
        {
            return DBusType.Array;
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            return DBusType.UnixFd;
        }
        return DBusType.Struct;
    }

    public static Type? ExtractGenericInterface(Type queryType, Type interfaceType)
    {
        if (IsGenericInstantiation(queryType, interfaceType))
        {
            return queryType;
        }

        return GetGenericInstantiation(queryType, interfaceType);
    }

    public static Type DetermineVariantType(Utf8Span signature)
    {
        // TODO: add caching.
        switch ((DBusType)signature.Span[0])
        {
            case DBusType.Byte:
                return typeof(byte);
            case DBusType.Bool:
                return typeof(bool);
            case DBusType.Int16:
                return typeof(Int16);
            case DBusType.UInt16:
                return typeof(UInt16);
            case DBusType.Int32:
                return typeof(Int32);
            case DBusType.UInt32:
                return typeof(UInt32);
            case DBusType.Int64:
                return typeof(Int64);
            case DBusType.UInt64:
                return typeof(UInt64);
            case DBusType.Double:
                return typeof(double);
            case DBusType.String:
                return typeof(string);
            case DBusType.ObjectPath:
                return typeof(string);
            case DBusType.Signature:
                return typeof(string);
            case DBusType.UnixFd:
                return typeof(SafeHandle);

            case DBusType.Array:
                if ((DBusType)signature.Span[1] == DBusType.DictEntry)
                {
                    Type keyType = DetermineVariantType(signature.Span.Slice(2));
                    Type valueType = DetermineVariantType(signature.Span.Slice(3));
                    return typeof(Dictionary<,>).MakeGenericType(new[] { keyType, valueType });
                }
                return DetermineVariantType(signature.Span.Slice(1)).MakeArrayType();

            case DBusType.Struct:
                ReadOnlySpan<byte> structTypeSignatures = signature.Span.Slice(1, signature.Span.Length - 2);
                int typeCount = SignatureReader.CountTypes(structTypeSignatures);
                SignatureReader reader = new(structTypeSignatures);
                Type[] types = new Type[typeCount];
                for (int i = 0; i < typeCount; i++)
                {
                    types[i] = DetermineVariantType(SignatureReader.ReadSingleType(ref structTypeSignatures));
                }
                switch (typeCount)
                {
                    case 1:
                        return typeof(ValueTuple<>).MakeGenericType(types);
                    case 2:
                        return typeof(ValueTuple<,>).MakeGenericType(types);
                    case 3:
                        return typeof(ValueTuple<,,>).MakeGenericType(types);
                    case 4:
                        return typeof(ValueTuple<,,,>).MakeGenericType(types);
                    case 5:
                        return typeof(ValueTuple<,,,,>).MakeGenericType(types);
                }
                break;
        }
        return typeof(object);
    }

    private static bool IsGenericInstantiation(Type candidate, Type interfaceType)
    {
        return
            candidate.IsGenericType &&
            candidate.GetGenericTypeDefinition() == interfaceType;
    }

    private static Type? GetGenericInstantiation(Type queryType, Type interfaceType)
    {
        Type? bestMatch = null;
        var interfaces = queryType.GetInterfaces();
        foreach (var @interface in interfaces)
        {
            if (IsGenericInstantiation(@interface, interfaceType))
            {
                if (bestMatch == null)
                {
                    bestMatch = @interface;
                }
                else if (StringComparer.Ordinal.Compare(@interface.FullName, bestMatch.FullName) < 0)
                {
                    bestMatch = @interface;
                }
            }
        }

        if (bestMatch != null)
        {
            return bestMatch;
        }

        var baseType = queryType?.BaseType;
        if (baseType == null)
        {
            return null;
        }
        else
        {
            return GetGenericInstantiation(baseType, interfaceType);
        }
    }

    private static int AppendTypeSignature(Type type, Span<byte> signature)
    {
        Type? extractedType;
        if (type == typeof(object))
        {
            signature[0] = (byte)DBusType.Variant;
            return 1;
        }
        else if (type == typeof(byte))
        {
            signature[0] = (byte)DBusType.Byte;
            return 1;
        }
        else if (type == typeof(bool))
        {
            signature[0] = (byte)DBusType.Bool;
            return 1;
        }
        else if (type == typeof(Int16))
        {
            signature[0] = (byte)DBusType.Int16;
            return 1;
        }
        else if (type == typeof(UInt16))
        {
            signature[0] = (byte)DBusType.UInt16;
            return 1;
        }
        else if (type == typeof(Int32))
        {
            signature[0] = (byte)DBusType.Int32;
            return 1;
        }
        else if (type == typeof(UInt32))
        {
            signature[0] = (byte)DBusType.UInt32;
            return 1;
        }
        else if (type == typeof(Int64))
        {
            signature[0] = (byte)DBusType.Int64;
            return 1;
        }
        else if (type == typeof(UInt64))
        {
            signature[0] = (byte)DBusType.UInt64;
            return 1;
        }
        else if (type == typeof(Double))
        {
            signature[0] = (byte)DBusType.Double;
            return 1;
        }
        else if (type == typeof(string))
        {
            signature[0] = (byte)DBusType.String;
            return 1;
        }
        else if (type == typeof(ObjectPath))
        {
            signature[0] = (byte)DBusType.ObjectPath;
            return 1;
        }
        else if (type == typeof(Signature))
        {
            signature[0] = (byte)DBusType.Signature;
            return 1;
        }
        else if (type.IsArray)
        {
            int bytesWritten = 0;
            signature[bytesWritten++] = (byte)DBusType.Array;
            bytesWritten += AppendTypeSignature(type.GetElementType()!, signature.Slice(bytesWritten));
            return bytesWritten;
        }
        else if (type.FullName!.StartsWith("System.ValueTuple"))
        {
            int bytesWritten = 0;
            signature[bytesWritten++] = (byte)'(';
            foreach (var itemType in type.GenericTypeArguments)
            {
                bytesWritten += AppendTypeSignature(itemType, signature.Slice(bytesWritten));
            }
            signature[bytesWritten++] = (byte)')';
            return bytesWritten;
        }
        else if ((extractedType = TypeModel.ExtractGenericInterface(type, typeof(IDictionary<,>))) != null)
        {
            int bytesWritten = 0;
            signature[bytesWritten++] = (byte)'a';
            signature[bytesWritten++] = (byte)'{';
            bytesWritten += AppendTypeSignature(extractedType.GenericTypeArguments[0], signature.Slice(bytesWritten));
            bytesWritten += AppendTypeSignature(extractedType.GenericTypeArguments[1], signature.Slice(bytesWritten));
            signature[bytesWritten++] = (byte)'}';
            return bytesWritten;
        }
        else if (type.IsAssignableTo(typeof(SafeHandle)))
        {
            signature[0] = (byte)DBusType.UnixFd;
            return 1;
        }
        return 0;
    }

    public static ReadOnlySpan<byte> GetSignature<T>() => SignatureCache<T>.Signature;

    static class SignatureCache<T>
    {
        private static readonly byte[] s_signature = GetSignature(typeof(T));

        public static ReadOnlySpan<byte> Signature => s_signature;

        private static byte[] GetSignature(Type type)
        {
            Span<byte> buffer = stackalloc byte[256];
            int bytesWritten = TypeModel.AppendTypeSignature(type, buffer);
            return buffer.Slice(0, bytesWritten).ToArray();
        }
    }
}
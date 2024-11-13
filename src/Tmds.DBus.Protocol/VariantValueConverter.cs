namespace Tmds.DBus.Protocol;

static class VariantValueConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VariantValue ToVariantValue<T>(T value, bool nest = false) where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            return VariantValue.Byte((byte)(object)value);
        }
        else if (typeof(T) == typeof(bool))
        {
            return VariantValue.Bool((bool)(object)value);
        }
        else if (typeof(T) == typeof(short))
        {
            return VariantValue.Int16((short)(object)value);
        }
        else if (typeof(T) == typeof(ushort))
        {
            return VariantValue.UInt16((ushort)(object)value);
        }
        else if (typeof(T) == typeof(int))
        {
            return VariantValue.Int32((int)(object)value);
        }
        else if (typeof(T) == typeof(uint))
        {
           return VariantValue.UInt32((uint)(object)value);
        }
        else if (typeof(T) == typeof(long))
        {
            return VariantValue.Int64((long)(object)value);
        }
        else if (typeof(T) == typeof(ulong))
        {
            return VariantValue.UInt64((ulong)(object)value);
        }
        else if (typeof(T) == typeof(double))
        {
            return VariantValue.Double((double)(object)value);
        }
        else if (typeof(T) == typeof(string))
        {
            return VariantValue.String((string)(object)value);
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            return VariantValue.ObjectPath(((ObjectPath)(object)value));
        }
        else if (typeof(T) == typeof(Signature))
        {
            return VariantValue.Signature(((Signature)(object)value));
        }
        else if (typeof(T) == typeof(VariantValue))
        {
            var vv = (VariantValue)(object)value;
            if (nest)
            {
                vv = VariantValue.Variant(vv);
            }
            return vv;
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            return VariantValue.UnixFd((SafeHandle)(object)value);
        }
        else if (typeof(T).IsAssignableTo(typeof(IVariantValueConvertable)))
        {
            return (value as IVariantValueConvertable)!.AsVariantValue();
        }
        else
        {
            ThrowNotSupportedType(typeof(T));
            return default;
        }
    }

    private static void ThrowNotSupportedType(Type type)
    {
        throw new NotSupportedException($"Cannot convert type {type.FullName}");
    }
}

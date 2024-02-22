using System.Runtime.CompilerServices;

namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Write<T>(T value) where T : notnull
    {
        if (typeof(T) == typeof(byte))
        {
            WriteByte((byte)(object)value);
        }
        else if (typeof(T) == typeof(bool))
        {
            WriteBool((bool)(object)value);
        }
        else if (typeof(T) == typeof(Int16))
        {
            WriteInt16((Int16)(object)value);
        }
        else if (typeof(T) == typeof(UInt16))
        {
            WriteUInt16((UInt16)(object)value);
        }
        else if (typeof(T) == typeof(Int32))
        {
            WriteInt32((Int32)(object)value);
        }
        else if (typeof(T) == typeof(UInt32))
        {
            WriteUInt32((UInt32)(object)value);
        }
        else if (typeof(T) == typeof(Int64))
        {
            WriteInt64((Int64)(object)value);
        }
        else if (typeof(T) == typeof(UInt64))
        {
            WriteUInt64((UInt64)(object)value);
        }
        else if (typeof(T) == typeof(Double))
        {
            WriteDouble((double)(object)value);
        }
        else if (typeof(T) == typeof(string))
        {
            WriteString((string)(object)value);
        }
        else if (typeof(T) == typeof(ObjectPath))
        {
            WriteString(((ObjectPath)(object)value).ToString());
        }
        else if (typeof(T) == typeof(Signature))
        {
            WriteSignature(((Signature)(object)value).ToString());
        }
        else if (typeof(T) == typeof(Variant))
        {
            ((Variant)(object)value).WriteTo(ref this);
        }
        else if (typeof(T).IsAssignableTo(typeof(SafeHandle)))
        {
            WriteHandle((SafeHandle)(object)value);
        }
        else if (typeof(T).IsAssignableTo(typeof(IDBusWritable)))
        {
            (value as IDBusWritable)!.WriteTo(ref this);
        }
        else if (Feature.IsDynamicCodeEnabled)
        {
            WriteDynamic<T>(value);
        }
        else
        {
            ThrowNotSupportedType(typeof(T));
        }
    }

    private static void ThrowNotSupportedType(Type type)
    {
        throw new NotSupportedException($"Cannot write type {type.FullName}");
    }
}

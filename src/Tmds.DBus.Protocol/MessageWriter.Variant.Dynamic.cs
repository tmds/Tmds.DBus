namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteVariant(object value)
    {
        Type type = value.GetType();

        if (type == typeof(byte))
        {
            WriteVariantByte((byte)value);
            return;
        }
        else if (type == typeof(bool))
        {
            WriteVariantBool((bool)value);
            return;
        }
        else if (type == typeof(Int16))
        {
            WriteVariantInt16((Int16)value);
            return;
        }
        else if (type == typeof(UInt16))
        {
            WriteVariantUInt16((UInt16)value);
            return;
        }
        else if (type == typeof(Int32))
        {
            WriteVariantInt32((Int32)value);
            return;
        }
        else if (type == typeof(UInt32))
        {
            WriteVariantUInt32((UInt32)value);
            return;
        }
        else if (type == typeof(Int64))
        {
            WriteVariantInt64((Int64)value);
            return;
        }
        else if (type == typeof(UInt64))
        {
            WriteVariantUInt64((UInt64)value);
            return;
        }
        else if (type == typeof(Double))
        {
            WriteVariantDouble((double)value);
            return;
        }
        else if (type == typeof(string))
        {
            WriteVariantString((string)value);
            return;
        }
        else if (type == typeof(ObjectPath))
        {
            WriteVariantObjectPath(value.ToString()!);
            return;
        }
        else if (type == typeof(Signature))
        {
            WriteVariantSignature(value.ToString()!);
            return;
        }
        else
        {
            var typeWriter = GetTypeWriter(type);
            typeWriter.WriteVariant(ref this, value);
        }
    }
}

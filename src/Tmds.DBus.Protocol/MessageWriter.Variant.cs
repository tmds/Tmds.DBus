namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteVariant(VariantValue value)
    {
        value.WriteVariantTo(ref this);
    }
}

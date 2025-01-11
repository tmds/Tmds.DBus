namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    [Obsolete($"Variant will be removed. Use the {nameof(VariantValue)} type instead.")]
    public void WriteVariant(Variant value)
    {
        value.WriteTo(ref this);
    }

    public void WriteVariant(VariantValue value)
    {
        value.WriteVariantTo(ref this);
    }
}

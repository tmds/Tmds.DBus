namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    /// <summary>
    /// Writes a variant.
    /// </summary>
    /// <param name="value">The variant value to write.</param>
    public void WriteVariant(VariantValue value)
    {
        value.WriteVariantTo(ref this);
    }
}

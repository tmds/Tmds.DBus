namespace Tmds.DBus.Protocol;

/// <summary>
/// Internal interface that enables creating VariantValues from composite types.
/// </summary>
public interface IVariantValueConvertable
{
    /// <summary>
    /// Converts this instance to a VariantValue.
    /// </summary>
    /// <returns>The VariantValue representation of this instance.</returns>
    VariantValue AsVariantValue();
}

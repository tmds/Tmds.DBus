namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    [RequiresUnreferencedCode($"Use '{nameof(ReadVariantValue)}' instead.")]
    public object ReadVariant() => Read<object>();
}

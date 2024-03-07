namespace Tmds.DBus.Protocol;

public sealed class Dict<TKey, TValue> : IDBusWritable
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _dict;

    public Dictionary<TKey, TValue> AsDictionary() => _dict;

    public Variant AsVariant() => Variant.FromDict(this);

    public static implicit operator Variant(Dict<TKey, TValue> value)
        => value.AsVariant();

    public Dict()
    {
        TypeModel.EnsureSupportedVariantType<TKey>();
        TypeModel.EnsureSupportedVariantType<TValue>();
        _dict = new();
    }

    public Dict(Dictionary<TKey, TValue> value)
    {
        TypeModel.EnsureSupportedVariantType<TKey>();
        TypeModel.EnsureSupportedVariantType<TValue>();
        _dict = value ?? throw new ArgumentNullException(nameof(value));
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteDictionary<TKey, TValue>(_dict);
}

namespace Tmds.DBus.Protocol;

public sealed class Array<T> : IDBusWritable
    where T : notnull
{
    private T[] _array;

    public T[] AsArray() => _array;

    public Variant AsVariant() => Variant.FromArray(this);

    public static implicit operator Variant(Array<T> value)
        => value.AsVariant();

    public Array()
    {
        TypeModel.EnsureSupportedVariantType<T>();
        _array = Array.Empty<T>();
    }

    public Array(T[] value)
    {
        TypeModel.EnsureSupportedVariantType<T>();
        _array = value ?? throw new ArgumentNullException(nameof(value));
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteArray<T>(_array);
}

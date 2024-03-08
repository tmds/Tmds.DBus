namespace Tmds.DBus.Protocol;

public sealed class Array<T> : IDBusWritable
    where T : notnull
{
    private readonly T[] _array;

    public Array() :
        this(Array.Empty<T>())
    { }

    public Array(int length) :
        this(new T[length])
    { }

    public Array(T[] value)
    {
        TypeModel.EnsureSupportedVariantType<T>();
        _array = value ?? throw new ArgumentNullException(nameof(value));
    }

    public bool IsEmpty => Length == 0;

    public int Length => _array.Length;

    public ref T this[int index]
        => ref _array[index];

    public Variant AsVariant()
        => Variant.FromArray(this);

    public static implicit operator Variant(Array<T> value)
        => value.AsVariant();

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // this is a supported variant type.
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteArray<T>(_array);
}

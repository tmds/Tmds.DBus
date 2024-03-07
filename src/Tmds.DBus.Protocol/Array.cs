namespace Tmds.DBus.Protocol;

public sealed class Array
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T
    >
    : IDBusReadable, IDBusWritable
    where T : notnull
{
    private T[] _array;

    public T[] AsArray() => _array;

    public Variant AsVariant() => Variant.FromArray(this);

    public static implicit operator Variant(Array<T> value)
        => value.AsVariant();

    public Array()
        => _array = Array.Empty<T>();

    public Array(T[] value)
        => _array = value ?? throw new ArgumentNullException(nameof(value));

    public static implicit operator Array<T>(T[] value)
        => new Array<T>(value);

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // User is expected to use a compatible 'T".
    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteArray<T>(_array);

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026")] // User is expected to use a compatible 'T".
    void IDBusReadable.ReadFrom(ref Reader reader)
        => _array = reader.ReadArray<T>();
}

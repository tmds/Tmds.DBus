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

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteArray<T>(_array);
    void IDBusReadable.ReadFrom(ref Reader reader)
        => _array = reader.ReadArray<T>();
}

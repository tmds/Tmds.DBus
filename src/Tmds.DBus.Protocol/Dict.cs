namespace Tmds.DBus.Protocol;

public sealed class Dict
    <
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TKey,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]TValue
    >
    : IDBusReadable, IDBusWritable
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _dict;

    public Dictionary<TKey, TValue> AsDictionary() => _dict;

    public Dict()
        => _dict = new();

    public Dict(Dictionary<TKey, TValue> value)
        => _dict = value ?? throw new ArgumentNullException(nameof(value));

    public static implicit operator Dict<TKey, TValue>(Dictionary<TKey, TValue> value)
        => new Dict<TKey, TValue>(value);

    void IDBusReadable.ReadFrom(ref Reader reader)
        => reader.ReadDictionary<TKey, TValue>(AsDictionary());

    void IDBusWritable.WriteTo(ref MessageWriter writer)
        => writer.WriteDictionary<TKey, TValue>(_dict);
}

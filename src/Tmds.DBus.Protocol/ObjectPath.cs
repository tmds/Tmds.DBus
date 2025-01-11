namespace Tmds.DBus.Protocol;

public readonly struct ObjectPath
{
    private readonly string _value;

    public ObjectPath(string value)
    {
        _value = value;
        ThrowIfEmpty();
    }

    internal void ThrowIfEmpty()
    {
        if (_value is null || _value.Length == 0)
        {
            ThrowEmptyException();
        }
    }

    private void ThrowEmptyException()
    {
        throw new ArgumentException($"{nameof(ObjectPath)} is empty.");
    }

    public override string ToString() => _value ?? "";

    public static implicit operator string(ObjectPath value) => value._value;

    public static implicit operator ObjectPath(string value) => new ObjectPath(value);

    [Obsolete($"Variant will be removed. Use the {nameof(VariantValue)} type instead.")]
    public Variant AsVariant() => new Variant(this);
}
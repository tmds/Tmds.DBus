namespace Tmds.DBus.Protocol;

public struct Signature
{
    private byte[]? _value;

    internal byte[] Data => _value ?? Array.Empty<byte>();

    [Obsolete("Use the constructor that accepts a ReadOnlySpan.")]
    public Signature(string value)
        => _value = Encoding.UTF8.GetBytes(value);

    public Signature(ReadOnlySpan<byte> value)
        => _value = value.ToArray();

    public override string ToString()
        => Encoding.UTF8.GetString(Data);

    public static implicit operator Signature(ReadOnlySpan<byte> value)
        => new Signature(value);

    public Variant AsVariant() => new Variant(this);
}
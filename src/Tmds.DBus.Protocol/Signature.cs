namespace Tmds.DBus.Protocol;

public struct Signature
{
    private byte[]? _value;

    // note: C# compiler treats these as static data.
    public static ReadOnlySpan<byte> Byte => new byte[] { (byte)'y' };
    public static ReadOnlySpan<byte> Boolean => new byte[] { (byte)'b' };
    public static ReadOnlySpan<byte> Int16 => new byte[] { (byte)'n' };
    public static ReadOnlySpan<byte> UInt16 => new byte[] { (byte)'q' };
    public static ReadOnlySpan<byte> Int32 => new byte[] { (byte)'i' };
    public static ReadOnlySpan<byte> UInt32 => new byte[] { (byte)'u' };
    public static ReadOnlySpan<byte> Int64 => new byte[] { (byte)'x' };
    public static ReadOnlySpan<byte> UInt64 => new byte[] { (byte)'t' };
    public static ReadOnlySpan<byte> Double => new byte[] { (byte)'d' };
    public static ReadOnlySpan<byte> UnixFd => new byte[] { (byte)'h' };
    public static ReadOnlySpan<byte> String => new byte[] { (byte)'s' };
    public static ReadOnlySpan<byte> ObjectPath => new byte[] { (byte)'o' };
    public static ReadOnlySpan<byte> Sig => new byte[] { (byte)'g' }; // Name can not be the same as enclosing type.
    public static ReadOnlySpan<byte> Variant => new byte[] { (byte)'v' };

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
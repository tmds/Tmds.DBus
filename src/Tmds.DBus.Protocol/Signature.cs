namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents a D-Bus type signature.
/// </summary>
/// <remarks>
/// No validation is performed on the signature value.
/// </remarks>
public readonly struct Signature
{
    private readonly byte[]? _value;

    // note: C# compiler treats these as static data.
    internal static ReadOnlySpan<byte> Byte => new byte[] { (byte)'y' };
    internal static ReadOnlySpan<byte> Boolean => new byte[] { (byte)'b' };
    internal static ReadOnlySpan<byte> Int16 => new byte[] { (byte)'n' };
    internal static ReadOnlySpan<byte> UInt16 => new byte[] { (byte)'q' };
    internal static ReadOnlySpan<byte> Int32 => new byte[] { (byte)'i' };
    internal static ReadOnlySpan<byte> UInt32 => new byte[] { (byte)'u' };
    internal static ReadOnlySpan<byte> Int64 => new byte[] { (byte)'x' };
    internal static ReadOnlySpan<byte> UInt64 => new byte[] { (byte)'t' };
    internal static ReadOnlySpan<byte> Double => new byte[] { (byte)'d' };
    internal static ReadOnlySpan<byte> UnixFd => new byte[] { (byte)'h' };
    internal static ReadOnlySpan<byte> String => new byte[] { (byte)'s' };
    internal static ReadOnlySpan<byte> ObjectPath => new byte[] { (byte)'o' };
    internal static ReadOnlySpan<byte> Sig => new byte[] { (byte)'g' }; // Name can not be the same as enclosing type.
    internal static ReadOnlySpan<byte> Variant => new byte[] { (byte)'v' };

    internal byte[] Data => _value ?? Array.Empty<byte>();

    /// <summary>
    /// Initializes a new instance of the Signature struct.
    /// </summary>
    /// <param name="value">The signature bytes.</param>
    /// <remarks>
    /// No validation is performed on the signature value.
    /// </remarks>
    public Signature(ReadOnlySpan<byte> value)
        => _value = value.ToArray();

    /// <summary>
    /// Returns the string representation of the signature.
    /// </summary>
    public override string ToString()
        => Encoding.UTF8.GetString(Data);

    /// <summary>
    /// Implicitly converts a byte span to a Signature.
    /// </summary>
    /// <param name="value">The byte span to convert.</param>
    /// <remarks>
    /// No validation is performed on the signature value.
    /// </remarks>
    public static implicit operator Signature(ReadOnlySpan<byte> value)
        => new Signature(value);
}
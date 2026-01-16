namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents a D-Bus object path.
/// </summary>
/// <remarks>
/// Only validates that the path is not empty. No validation is performed on the object path format.
/// </remarks>
public readonly struct ObjectPath
{
    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the ObjectPath struct.
    /// </summary>
    /// <param name="value">The object path.</param>
    /// <remarks>
    /// Only validates that the path is not empty. No validation is performed on the object path format.
    /// </remarks>
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

    /// <summary>
    /// Returns the string representation of the object path.
    /// </summary>
    public override string ToString() => _value ?? "";

    /// <summary>
    /// Implicitly converts an ObjectPath to a string.
    /// </summary>
    public static implicit operator string(ObjectPath value) => value.ToString();

    /// <summary>
    /// Implicitly converts a string to an ObjectPath.
    /// </summary>
    /// <remarks>
    /// Only validates that the path is not empty. No validation is performed on the object path format.
    /// </remarks>
    public static implicit operator ObjectPath(string value) => new ObjectPath(value);
}
namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when the peer gives an unexpected response.
/// </summary>
[Obsolete("Use DBusUnexpectedValueException instead.")]
public class ProtocolException : DBusUnexpectedValueException
{
    /// <summary>
    /// Initializes a new instance of the ProtocolException class.
    /// </summary>
    /// <param name="message">The error message describing the error .</param>
    public ProtocolException(string message) : base(message)
    { }
}

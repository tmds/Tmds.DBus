namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when an unexpected condition occurs while handling D-Bus messages.
/// </summary>
public class ProtocolException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ProtocolException class.
    /// </summary>
    /// <param name="message">The error message describing the error .</param>
    public ProtocolException(string message) : base(message)
    { }
}

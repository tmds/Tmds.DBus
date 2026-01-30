namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when a received D-Bus message is malformed or the read operations doesn't match the actual message format.
/// </summary>
public class DBusReadException : DBusMessageException
{
    /// <summary>
    /// Initializes a new instance of the DBusReadException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusReadException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusReadException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusReadException(string message, Exception innerException) : base(message, innerException)
    { }
}

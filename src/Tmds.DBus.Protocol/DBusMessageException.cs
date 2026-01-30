namespace Tmds.DBus.Protocol;

/// <summary>
/// Base exception class for exceptions related to a received D-Bus message.
/// </summary>
public class DBusMessageException : DBusExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the DBusMessageException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusMessageException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusMessageException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusMessageException(string message, Exception innerException) : base(message, innerException)
    { }
}

namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when a D-Bus reply message is unexpected or indicates an error.
/// </summary>
public class DBusReplyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DBusReplyException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusReplyException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusReplyException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusReplyException(string message, Exception innerException) : base(message, innerException)
    { }
}

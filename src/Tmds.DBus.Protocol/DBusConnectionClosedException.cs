namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when an operation fails because the connection is closed.
/// </summary>
/// <remarks>
/// The <see cref="Exception.InnerException"/> indicates the reason for the close.
/// </remarks>
public class DBusConnectionClosedException : DBusConnectionException
{
    /// <summary>
    /// Initializes a new instance of the DBusConnectionClosedException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusConnectionClosedException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusConnectionClosedException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusConnectionClosedException(string message, Exception innerException) : base(message, innerException)
    { }
}

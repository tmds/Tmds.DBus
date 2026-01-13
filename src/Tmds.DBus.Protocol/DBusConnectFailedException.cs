namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when a D-Bus connection cannot be established.
/// </summary>
public class DBusConnectFailedException : DBusConnectionException
{
    /// <summary>
    /// Initializes a new instance of the DBusConnectFailedException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusConnectFailedException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusConnectFailedException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusConnectFailedException(string message, Exception innerException) : base(message, innerException)
    { }
}

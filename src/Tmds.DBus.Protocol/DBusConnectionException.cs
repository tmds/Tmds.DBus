namespace Tmds.DBus.Protocol;

/// <summary>
/// Base exception class for D-Bus connection-related exceptions.
/// </summary>
public class DBusConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DBusConnectionException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusConnectionException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusConnectionException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusConnectionException(string message, Exception innerException) : base(message, innerException)
    { }
}

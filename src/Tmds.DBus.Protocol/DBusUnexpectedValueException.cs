namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when a received D-Bus message contains an unexpected value.
/// This indicates the message is valid and parseable, but the content doesn't match expectations.
/// </summary>
public class DBusUnexpectedValueException : DBusMessageException
{
    /// <summary>
    /// Initializes a new instance of the DBusUnexpectedValueException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusUnexpectedValueException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusUnexpectedValueException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusUnexpectedValueException(string message, Exception innerException) : base(message, innerException)
    { }
}

namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when the <see cref="Reader"/> encounters unexpected input.
/// This indicates the message is malformed or the read operation doesn't match the actual message format.
/// </summary>
public class DBusReaderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DBusReaderException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusReaderException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusReaderException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusReaderException(string message, Exception innerException) : base(message, innerException)
    { }
}

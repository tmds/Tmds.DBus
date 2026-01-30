namespace Tmds.DBus.Protocol;

/// <summary>
/// Base exception class for all D-Bus exceptions.
/// </summary>
public class DBusExceptionBase : Exception
{
    /// <summary>
    /// Initializes a new instance of the DBusExceptionBase class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusExceptionBase(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusExceptionBase class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusExceptionBase(string message, Exception innerException) : base(message, innerException)
    { }
}

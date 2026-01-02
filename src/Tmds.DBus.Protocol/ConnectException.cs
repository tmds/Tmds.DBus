namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when a D-Bus connection cannot be established.
/// </summary>
public class ConnectException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ConnectException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConnectException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the ConnectException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public ConnectException(string message, Exception innerException) : base(message, innerException)
    { }
}
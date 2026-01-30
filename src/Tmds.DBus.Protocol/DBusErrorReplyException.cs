namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when receiving an error message reply to a method call.
/// </summary>
public class DBusErrorReplyException : DBusMessageException
{
    /// <summary>
    /// Initializes a new instance of the DBusErrorReplyException class.
    /// </summary>
    /// <param name="errorName">The error name.</param>
    /// <param name="errorMessage">The error message.</param>
    public DBusErrorReplyException(string errorName, string errorMessage) :
        base($"{errorName}: {errorMessage}")
    {
        ErrorName = errorName;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the error name.
    /// </summary>
    public string ErrorName { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorMessage { get; }
}

namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception representing a D-Bus error reply to a method call.
/// </summary>
public class DBusMethodException : Exception
{
    /// <summary>
    /// Initializes a new instance of the DBusMethodException class.
    /// </summary>
    /// <param name="errorName">The error name.</param>
    /// <param name="errorMessage">The error message.</param>
    public DBusMethodException(string errorName, string errorMessage) :
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

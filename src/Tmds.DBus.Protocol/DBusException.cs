namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception representing a D-Bus error reply to a method call.
/// </summary>
[Obsolete("Use DBusErrorReplyException instead.")] // When removing, make this a base of DBusExceptionBase and mark that class as Obsolete.
public class DBusException : DBusErrorReplyException
{
    /// <summary>
    /// Initializes a new instance of the DBusException class.
    /// </summary>
    /// <param name="errorName">The error name.</param>
    /// <param name="errorMessage">The error message.</param>
    public DBusException(string errorName, string errorMessage) :
        base(errorName, errorMessage)
    { }

    /// <summary>
    /// Gets the error name.
    /// </summary>
    public new string ErrorName => base.ErrorName;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public new string ErrorMessage => base.ErrorMessage;
}

namespace Tmds.DBus.Protocol;

/// <summary>
/// The exception that is thrown when the owner of a D-Bus well-known name has changed.
/// </summary>
public sealed class DBusOwnerChangedException : DBusMessageException
{
    /// <summary>
    /// Initializes a new instance of the DBusOwnerChangedException class.
    /// </summary>
    public DBusOwnerChangedException() : base("The owner of the name changed.")
    { }

    /// <summary>
    /// Initializes a new instance of the DBusOwnerChangedException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DBusOwnerChangedException(string message) : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DBusOwnerChangedException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DBusOwnerChangedException(string message, Exception innerException) : base(message, innerException)
    { }
}

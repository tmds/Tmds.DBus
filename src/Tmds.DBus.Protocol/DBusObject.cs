namespace Tmds.DBus.Protocol;

/// <summary>
/// Base class for D-Bus proxy objects.
/// </summary>
public class DBusObject
{
    /// <summary>
    /// Gets the connection to the message bus.
    /// </summary>
    public DBusConnection Connection { get; }

    /// <summary>
    /// Gets the destination bus name.
    /// </summary>
    public string Destination { get; }

    /// <summary>
    /// Gets a DBusService instance for the destination.
    /// </summary>
    public DBusService Remote => new DBusService(Connection, Destination);

    /// <summary>
    /// Gets the object path.
    /// </summary>
    public ObjectPath Path { get; }

    /// <summary>
    /// Initializes a new instance of the DBusObject class.
    /// </summary>
    /// <param name="connection">The connection to the message bus.</param>
    /// <param name="destination">The destination bus name.</param>
    /// <param name="path">The object path.</param>
    protected DBusObject(DBusConnection connection, string destination, ObjectPath path)
        => (Connection, Destination, Path) = (connection, destination, path);
}

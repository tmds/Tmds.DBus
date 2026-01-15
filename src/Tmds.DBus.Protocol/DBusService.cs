namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents a named connection on the message bus.
/// </summary>
/// <remarks>
/// The name may be a well-known bus name (like <c>org.freedesktop.NetworkManager</c>) or a unique connection name assigned by the message bus (like <c>:1.42</c>).
/// </remarks>
public readonly struct DBusService
{
    /// <summary>
    /// Gets the connection to the message bus.
    /// </summary>
    public DBusConnection Connection { get; }

    /// <summary>
    /// Gets the bus name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the DBusService struct.
    /// </summary>
    /// <param name="connection">The connection to the message bus.</param>
    /// <param name="name">The bus name.</param>
    public DBusService(DBusConnection connection, string name)
    {
        Connection = connection;
        Name = name;
    }

    /// <inheritdoc/>
    public override string ToString() => Name;
}

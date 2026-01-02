namespace Tmds.DBus.Protocol;

/// <summary>
/// Provides standard D-Bus introspection XML for common interfaces.
/// </summary>
public static class IntrospectionXml
{
    /// <summary>
    /// Gets the introspection XML for the org.freedesktop.DBus.Properties interface.
    /// </summary>
    public static ReadOnlyMemory<byte> DBusProperties { get; } =
        """
        <interface name="org.freedesktop.DBus.Properties">
          <method name="Get">
            <arg direction="in" type="s"/>
            <arg direction="in" type="s"/>
            <arg direction="out" type="v"/>
          </method>
          <method name="GetAll">
            <arg direction="in" type="s"/>
            <arg direction="out" type="a{sv}"/>
          </method>
          <method name="Set">
            <arg direction="in" type="s"/>
            <arg direction="in" type="s"/>
            <arg direction="in" type="v"/>
          </method>
          <signal name="PropertiesChanged">
            <arg type="s" name="interface_name"/>
            <arg type="a{sv}" name="changed_properties"/>
            <arg type="as" name="invalidated_properties"/>
          </signal>
        </interface>

        """u8.ToArray();

    /// <summary>
    /// Gets the introspection XML for the org.freedesktop.DBus.Introspectable interface.
    /// </summary>
    public static ReadOnlyMemory<byte> DBusIntrospectable { get; } =
        """
        <interface name="org.freedesktop.DBus.Introspectable">
          <method name="Introspect">
            <arg type="s" name="xml_data" direction="out"/>
          </method>
        </interface>

        """u8.ToArray();

    /// <summary>
    /// Gets the introspection XML for the org.freedesktop.DBus.Peer interface.
    /// </summary>
    public static ReadOnlyMemory<byte> DBusPeer { get; } =
        """
        <interface name="org.freedesktop.DBus.Peer">
          <method name="Ping"/>
          <method name="GetMachineId">
            <arg type="s" name="machine_uuid" direction="out"/>
          </method>
        </interface>

        """u8.ToArray();
}
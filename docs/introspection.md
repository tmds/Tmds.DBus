# Introspection Overview
DBus allows protocols to be introspected at runtime. By implementing the `org.freedesktop.DBus.Introspectable` interface and returning an XML string that describes the protocol, you are able to use discover what is available to be used on a given protocol.

For more information on the introspection data format visit the: [Introspection Data Format documentation](https://dbus.freedesktop.org/doc/dbus-specification.html#introspection-format)

## Basic Introspection Implementation

```csharp
using Tmds.DBus.Protocol;

class Program
{
    static async Task Main()
    {
        string peerName = await StartIntrospectServiceAsync();
    }
    
    private async static Task<string> StartIntrospectServiceAsync()
    {
        var connection = new Connection(Address.Session!);

        await connection.ConnectAsync();

        connection.AddMethodHandler(new IntrospectProtocol());
        
        return connection.UniqueName ?? "";
    }
}

class IntrospectProtocol : IMethodHandler
{
    public string Path => "/";
    private readonly string _interface = "org.freedesktop.DBus.Introspectable";
    private readonly string _protocolDefinition;

    public IntrospectProtocol()
    {
        _protocolDefinition = File.ReadAllText("/path/to/your/protocol.xml");
    }

    public bool TryHandleMethod(Connection connection, in Message message)
    {
        if (Encoding.UTF8.GetString(message.Interface.Span) != _interface) { return false; }

        using var writer = connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(message.Serial, message.Sender, "s");
        writer.WriteString(_protocolDefinition);
        var response = writer.CreateMessage();
        connection.TrySendMessage(response);

        return true;
    }
}

```

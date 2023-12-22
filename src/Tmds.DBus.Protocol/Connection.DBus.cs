namespace Tmds.DBus.Protocol;

public partial class Connection
{
    public const string DBusObjectPath = "/org/freedesktop/DBus";
    public const string DBusServiceName = "org.freedesktop.DBus";
    public const string DBusInterface = "org.freedesktop.DBus";

    public Task<string[]> ListServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadArray<string>());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListNames");
            return writer.CreateMessage();
        }
    }

    public async Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules = null)
    {
        DBusConnection connection = await ConnectCoreAsync().ConfigureAwait(false);
        await connection.BecomeMonitorAsync(handler, rules).ConfigureAwait(false);
    }
}
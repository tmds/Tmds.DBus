using NetworkManager.DBus;
using Tmds.DBus.Protocol;
using System;

string? systemBusAddress = DBusAddress.System;
if (systemBusAddress is null)
{
    Console.Write("Can not determine system bus address");
    return 1;
}

DBusConnection connection = new DBusConnection(DBusAddress.System!);
await connection.ConnectAsync();
Console.WriteLine("Connected to system bus.");

var service = new DBusService(connection, "org.freedesktop.NetworkManager");
var networkManager = service.CreateNetworkManager("/org/freedesktop/NetworkManager");

foreach (var devicePath in await networkManager.GetDevicesAsync())
{
    var device = service.CreateDevice(devicePath);
    var interfaceName = await device.GetInterfaceAsync();

    Console.WriteLine($"Subscribing for state changes of '{interfaceName}'.");
    await device.WatchStateChangedAsync(
        (Exception? ex, (DeviceState NewState, DeviceState OldState, uint Reason) change) =>
        {
            if (ex is null)
            {
                Console.WriteLine($"Interface '{interfaceName}' changed from '{change.OldState}' to '{change.NewState}'.");
            }
        });
}

Exception? disconnectReason = await connection.DisconnectedAsync();
if (disconnectReason is not null)
{
    Console.WriteLine("The connection was closed:");
    Console.WriteLine(disconnectReason);
    return 1;
}
return 0;

namespace NetworkManager.DBus
{
    using System.Threading.Tasks;

    enum DeviceState : uint
    {
        Unknown = 0,
        Unmanaged = 10,
        Unavailable = 20,
        Disconnected = 30,
        Prepare = 40,
        Config = 50,
        NeedAuth = 60,
        IpConfig = 70,
        IpCheck = 80,
        Secondaries = 90,
        Activated = 100,
        Deactivating = 110,
        Failed = 120
    }

    partial class Device
    {
        public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (DeviceState NewState, DeviceState OldState, uint Reason)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return Connection.WatchSignalAsync(
                Destination,
                Path,
                DBusInterfaceName,
                "StateChanged",
                (Message m, object? s) =>
                {
                    var reader = m.GetBodyReader();
                    return ((DeviceState)reader.ReadUInt32(), (DeviceState)reader.ReadUInt32(), reader.ReadUInt32());
                },
                handler,
                this,
                emitOnCapturedContext,
                flags);
        }
    }
}

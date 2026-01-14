using Connection = Tmds.DBus.Protocol.Connection;
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

var service = new NetworkManagerService(connection, "org.freedesktop.NetworkManager");
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

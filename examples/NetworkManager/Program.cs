using NetworkManager.DBus;
using Tmds.DBus.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;

string? systemBusAddress = DBusAddress.System;
if (systemBusAddress is null)
{
    Console.Write("Can not determine system bus address");
    return 1;
}

while (true)
{
    try
    {
        using DBusConnection connection = new DBusConnection(systemBusAddress);
        await connection.ConnectAsync();
        Console.WriteLine("Connected to system bus.");

        using var ownerWatcher = await connection.WatchNameOwnerAsync("org.freedesktop.NetworkManager");

        while (true)
        {
            string owner = await ownerWatcher.WaitForOwnerAsync();
            Console.WriteLine($"NetworkManager is running (bus name: {NameOwnerWatcher.GetOwnerBusName(owner)}).");

            var service = new DBusService(connection, owner);
            var networkManager = service.CreateNetworkManager("/org/freedesktop/NetworkManager");

            try
            {
                foreach (var devicePath in await networkManager.GetDevicesAsync())
                {
                    var device = service.CreateDevice(devicePath);
                    var interfaceName = await device.GetInterfaceAsync();

                    Console.WriteLine($"Subscribing for state changes of '{interfaceName}'.");
                    // The observers are automatically disposed when the owner of the name changes because the sender uses the NameOwnerWatcher owner.
                    await device.WatchStateChangedAsync(
                        change =>
                        {
                            Console.WriteLine($"Interface '{interfaceName}' changed from '{(DeviceState)change.OldState}' to '{(DeviceState)change.NewState}'.");
                        });
                }

                // Wait until the owner changes (e.g. NetworkManager restarts) or the connection is lost.
                CancellationToken ownerChanged = ownerWatcher.GetOwnerChangedCancellationToken(owner);
                await Task.Delay(-1, ownerChanged).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                Console.WriteLine("NetworkManager owner changed, re-subscribing...");
            }
            catch (DBusMessageException) when (ownerWatcher.GetCurrentOwner() != owner)
            {
                Console.WriteLine("NetworkManager owner changed during setup, retrying...");
            }
        }
    }
    catch (DBusConnectFailedException ex)
    {
        Console.WriteLine($"Failed to connect to D-Bus: {ex.Message}");
        Console.WriteLine("Reconnecting in 5 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    catch (DBusConnectionClosedException ex)
    {
        Console.WriteLine($"D-Bus connection closed: {ex.Message}");
        Console.WriteLine("Reconnecting...");
    }
}

namespace NetworkManager.DBus
{
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
}

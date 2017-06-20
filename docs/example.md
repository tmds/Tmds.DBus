# Example

We create a small application that writes a message when a network interface changes state.
We use the NetworkManager daemon to detect these changes.

We create a new application:

```
$ dotnet new console -o netmon
$ cd netmon
```

Now we add references to `Tmds.DBus` and `Tmds.DBus.Tool`. in `netmon.csproj`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Tmds.DBus" Version="0.5.0-*" />
    <DotNetCliToolReference Include="Tmds.DBus.Tool" Version="0.1.0-*" />
  </ItemGroup>
</Project>
```

Let's restore to fetch these dependencies:

```
$ dotnet restore
```

Next, we use the `list` command to find out some information about the NetworkManager service:
```
$ dotnet dbus list --bus system services | grep NetworkManager
org.freedesktop.NetworkManager
$ dotnet dbus list --bus system --service org.freedesktop.NetworkManager objects | head -2
/org/freedesktop : org.freedesktop.DBus.ObjectManager
/org/freedesktop/NetworkManager : org.freedesktop.NetworkManager
```

These command show us that the `org.freedesktop.NetworkManager` services is on the `system` bus
and has an entry point object at `/org/freedesktop/NetworkManager` which implements `org.freedesktop.NetworkManager`.

Now we'll invoke the `codegen` command to generate the C# interfaces implemented by the NetworkManager service.

```
$ dotnet dbus codegen --bus system --service org.freedesktop.NetworkManager
```

This generates a `NetworkManager.DBus.cs` file in the local folder.

Now we'll change a `Program.cs`. We connect to the system bus and create an `INetworkManager` proxy object.

```C#
using System;
using Tmds.DBus;
using NetworkManager.DBus;
using System.Threading.Tasks;

namespace dbus_app
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            using (var connection = new Connection(Address.System))
            {
                await connection.ConnectAsync();
                var networkManager = connection.CreateProxy<INetworkManager>("org.freedesktop.NetworkManager", "/org/freedesktop/NetworkManager");
                
                await Task.Run(() =>
                {
                    Console.WriteLine("Press any key to close the application.");
                    Console.Read();
                });
            }
        }
    }
}
```

If we look at the `INetworkManager` interface in `NetworkManager.DBus.cs`, we see it has a `GetDevicesAsync` method.
```C#
Task<ObjectPath[]> GetDevicesAsync();
```

This method is returning an `ObjectPath[]`. Thes paths refer to other objects of the D-Bus service. We can use these
paths and use them with `CreateProxy`. Instead, we'll update the method to reflect it is returning `IDevice` objects.

```C#
Task<IDevice[]> GetDevicesAsync();
```

We can now add the code to iterate over the devices and add a signal handler for the state change:
```C#
foreach (var device in await networkManager.GetDevicesAsync())
{
    var interfaceName = await device.GetInterfaceAsync();
    await device.WatchStateChangedAsync(
        change => Console.WriteLine($"{interfaceName}: {change.oldState} -> {change.newState}")
    );
}
```

When we run our program and change our network interfaces (e.g. turn on/off WiFi) we see the notifications show up:
```
$ dotnet run
Press any key to close the application.
wlp4s0: 100 -> 20
```

If we look up the documentation of the StateChanged signal, we find the meaning of the magical constants: [enum `NMDeviceState`](https://developer.gnome.org/NetworkManager/stable/nm-dbus-types.html#NMDeviceState).

We can model this enumeration in C#:
```C#
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
```

We add the enum to `NetworkManager.DBus.cs` and then update the signature of the `WatchStateChangedAsync` so it
uses `DeviceState` instead of `uint`.

```C#
Task<IDisposable> WatchStateChangedAsync(Action<(DeviceState newState, DeviceState oldState, uint reason)> action);
```

When we run our application again, we see more meaningfull messages.

```
$ dotnet run
Press any key to close the application.
wlp4s0: Activated -> Unavailable
```
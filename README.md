[![NuGet](https://img.shields.io/nuget/v/Tmds.DBus.svg)](https://www.nuget.org/packages/Tmds.DBus)

# Introduction

From, https://www.freedesktop.org/wiki/Software/dbus/

> D-Bus is a message bus system, a simple way for applications to talk to one another. In addition to interprocess
communication, D-Bus helps coordinate process lifecycle; it makes it simple and reliable to code a "single instance"
application or daemon, and to launch applications and daemons on demand when their services are needed.

Higher-level bindings are available for various popular frameworks and languages (Qt, GLib, Java, Python, etc.).
[dbus-sharp](https://github.com/mono/dbus-sharp) (a fork of [ndesk-dbus](http://www.ndesk.org/DBusSharp)) is a C#
implementation which targets Mono and .NET 2.0.

Tmds.DBus builds on top of the protocol implementation of dbus-sharp and provides an API based on the asynchronous programming model introduced in .NET 4.5. The library targets .NET Standard 2.0 which means it runs on .NET Framework 4.6.1 (Windows 7 SP1 and later), .NET Core, and .NET 6. You can get Tmds.DBus from [NuGet](https://www.nuget.org/packages/Tmds.DBus).

# Tmds.DBus.Protocol

The `Tmds.DBus.Protocol` package provides a low-level API for the D-Bus protocol. Unlike the high-level `Tmds.DBus` library, the protocol library can be used with Native AOT compilation.

[affederaffe/Tmds.DBus.SourceGenerator](https://github.com/affederaffe/Tmds.DBus.SourceGenerator) provides a source generator that targets the protocol library.

# Tmds.DBus Example

In this section we build an example console application that writes a message when a network interface changes state.
To detect the state changes we use the NetworkManager daemon's D-Bus service.

The steps include using the `Tmds.DBus.Tool` to generate code and then enhancing the generated code.

We use the dotnet cli to create a new console application:

```bash
$ dotnet new console -o netmon
$ cd netmon
```

Now we add references to `Tmds.DBus` in `netmon.csproj`. If you need to target framework, `netcoreapp2.0`, add `<LangVersion>7.1</LangVersion>` below `<TargetFramework>...` to use `async Task Main` (C# 7.1).

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Tmds.DBus" Version="0.10.1" />
  </ItemGroup>
</Project>
```

Let's `restore` to fetch these dependencies:

```bash
$ dotnet restore
```

Now we'll install the `Tmds.DBus.Tool`.

```bash
$ dotnet tool install -g Tmds.DBus.Tool
```

Next, we use the `list` command to find out some information about the NetworkManager service:

```bash
$ dotnet dbus list services --bus system | grep NetworkManager
org.freedesktop.NetworkManager

$ dotnet dbus list objects --bus system --service org.freedesktop.NetworkManager | head -2
/org/freedesktop : org.freedesktop.DBus.ObjectManager
/org/freedesktop/NetworkManager : org.freedesktop.NetworkManager
```

These command show us that the `org.freedesktop.NetworkManager` service is on the `system` bus
and has an entry point object at `/org/freedesktop/NetworkManager` which implements `org.freedesktop.NetworkManager`.

Now we'll invoke the `codegen` command to generate C# interfaces for the NetworkManager service.

```bash
$ dotnet dbus codegen --bus system --service org.freedesktop.NetworkManager
```

This generates a `NetworkManager.DBus.cs` file in the local folder.

We update `Program.cs` to have an async `Main` and instiantiate an `INetworkManager` proxy object.

```C#
using System;
using Tmds.DBus;
using NetworkManager.DBus;
using System.Threading.Tasks;

namespace netmon
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Monitoring network state changes. Press Ctrl-C to stop.");

            var systemConnection = Connection.System;
            var networkManager = systemConnection.CreateProxy<INetworkManager>("org.freedesktop.NetworkManager",
                                                                               "/org/freedesktop/NetworkManager");

            await Task.Delay(int.MaxValue);
        }
    }
}
```

Note that we are using the static `Connection.System`. `Connection.System` and `Connection.Session` provide a connection to the system bus and session bus. These static members provide a convenient way to share the same `Connection` throughout the application. The connection to the bus is established automatically on first use. Statefull operations (e.g. `Connection.RegisterServiceAsync`) are not allowed. For these use-cases you must create an instance of the
`Connection` and manually connect it.

When we look at the `INetworkManager` interface in `NetworkManager.DBus.cs`, we see it has a `GetDevicesAsync` method.

```C#
Task<ObjectPath[]> GetDevicesAsync();
```

This method is returning `ObjectPath[]`. These paths refer to other objects of the D-Bus service. We can use them with `CreateProxy`. Instead, we'll update the method to reflect it is returning `IDevice` objects.

```C#
Task<IDevice[]> GetDevicesAsync();
```

We will now add the code to iterate over the devices and add a signal handler for the state change:

```C#
foreach (var device in await networkManager.GetDevicesAsync())
{
    var interfaceName = await device.GetInterfaceAsync();
    await device.WatchStateChangedAsync(
        change => Console.WriteLine($"{interfaceName}: {change.oldState} -> {change.newState}")
    );
}
```

When we run our program and change our network interfaces (e.g. turn on/off WiFi) notifications show up:

```bash
$ dotnet run
Monitoring network state changes. Press Ctrl-C to stop.
wlp4s0: 100 -> 20
```

When we look up the documentation of the StateChanged signal, we find the meaning of the magical constants:
[enum `NMDeviceState`](https://developer.gnome.org/NetworkManager/stable/nm-dbus-types.html#NMDeviceState).

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

When we run our application again, we see more meaningful messages.

```bash
$ dotnet run
Monitoring network state changes. Press Ctrl-C to stop.
wlp4s0: Activated -> Unavailable
```

# Further Reading

* [D-Bus](docs/dbus.md): Short overview of D-Bus.
* [Tmds.DBus Modelling](docs/modelling.md): Describes how to model D-Bus types in C# for use with Tmds.DBus.
* [Tmds.DBus.Tool](docs/tool.md): Documentation of dotnet dbus tool.
* [How-to](docs/howto.md): Documents some (advanced) use-cases.
* [Tmds.DBus.Protocol](docs/protocol.md): Documentation of the protocol library.

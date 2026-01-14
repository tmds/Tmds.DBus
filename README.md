[![NuGet](https://img.shields.io/nuget/v/Tmds.DBus.svg)](https://www.nuget.org/packages/Tmds.DBus)

# Introduction

From, https://www.freedesktop.org/wiki/Software/dbus/

> D-Bus is a message bus system, a simple way for applications to talk to one another. In addition to interprocess
communication, D-Bus helps coordinate process lifecycle; it makes it simple and reliable to code a "single instance"
application or daemon, and to launch applications and daemons on demand when their services are needed.

Higher-level bindings are available for various popular frameworks and languages (Qt, GLib, Java, Python, etc.).

This source repository provides two libraries for working with D-Bus.

- `Tmds.DBus` is a library that is based on [dbus-sharp](https://github.com/mono/dbus-sharp) (which is a fork of [ndesk-dbus](http://www.ndesk.org/DBusSharp)). `Tmds.DBus` builds on top of the protocol implementation of dbus-sharp and provides an API based on the asynchronous programming model introduced in .NET 4.5.

- `Tmds.DBus.Protocol` is a library that uses the types introduces in .NET Core 2.1 (like `Span<T>`) that enable writing a low-allocation, high-performance protocol implementation. This library is compatible with NativeAOT/Trimming (introduced in .NET 7).

Both libraries target .NET Standard 2.0 which means it runs on .NET Framework 4.6.1 (Windows 7 SP1 and later), .NET Core, and .NET 6 and higher.

To use `Tmds.DBus.Protocol` with trimming/NativeAOT, use .NET 8 or higher.

# Code generators

- [affederaffe/Tmds.DBus.SourceGenerator](https://github.com/affederaffe/Tmds.DBus.SourceGenerator) provides a source generator that targets the `Tmds.DBus.Protocol` library. This source generator supports generating proxy types (to consume objects provided by other services) as well as handler types (to provide objects to other applications).

- The `Tmds.DBus.Tool` .NET global CLI tool includes a code generator for `Tmds.DBus` and `Tmds.DBus.Protocol`. For the `Tmds.DBus.Protocol` library, the code generator only supports generating proxy types.

# Example

In this section we build an example console application that writes a message when a network interface changes state.
To detect the state changes we use the NetworkManager daemon's D-Bus service.

The steps include using the `Tmds.DBus.Tool` to generate code and then enhancing the generated code.

We use the dotnet cli to create a new console application:

```bash
$ dotnet new console -o netmon
$ cd netmon
```

Now we add references to `Tmds.DBus.Protocol`:
```
$ dotnet add package Tmds.DBus.Protocol
```

Next, we'll install the `Tmds.DBus.Tool`.

```bash
$ dotnet tool update -g Tmds.DBus.Tool
```

We use the `list` command to find out some information about the NetworkManager service:

```bash
$ dotnet dbus list services --bus system | grep NetworkManager
org.freedesktop.NetworkManager

$ dotnet dbus list objects --bus system --service org.freedesktop.NetworkManager | head -2
/org/freedesktop : org.freedesktop.DBus.ObjectManager
/org/freedesktop/NetworkManager : org.freedesktop.NetworkManager
```

These command show us that the `org.freedesktop.NetworkManager` service is on the `system` bus
and has an entry point object at `/org/freedesktop/NetworkManager` which implements `org.freedesktop.NetworkManager`.

Now we'll invoke the `codegen` command to generate C# interfaces for the NetworkManager service. We use the `--protocol-api` argument for targetting the `Tmds.DBus.Protocol` library.

```bash
$ dotnet dbus codegen --protocol-api --bus system --service org.freedesktop.NetworkManager
```

This generates a `NetworkManager.DBus.cs` file in the local folder.

When we try to compile the code using `dotnet build`, the compiler will give us some errors:

```
NetworkManager.DBus.cs(871,35): error CS0111: Type 'NetworkManager' already defines a member called 'GetDevicesAsync' with the same parameter types [/tmp/netmon/netmon.csproj]
NetworkManager.DBus.cs(873,35): error CS0111: Type 'NetworkManager' already defines a member called 'GetAllDevicesAsync' with the same parameter types [/tmp/netmon/netmon.csproj]
NetworkManager.DBus.cs(3723,35): error CS0111: Type 'Wireless' already defines a member called 'GetAccessPointsAsync' with the same parameter types [/tmp/netmon/netmon.csproj]
```

These errors occur because the D-Bus interfaces declare D-Bus methods named `GetXyz` and D-Bus properties which are named `Xyz`. The resulting C# methods that are generated have the same name which causes these errors. Because these methods are two ways to get the same information, we'll fix the problem by commenting out the `GetDevicesAsync`/`GetAllDevicesAsync`/`GetAccessPointsAsync` C# methods that are implemented using properties.

```diff
diff --git a/NetworkManager.DBus.cs b/NetworkManager.DBus.cs
index fab04fd..eb57d16 100644
--- a/NetworkManager.DBus.cs
+++ b/NetworkManager.DBus.cs
@@ -868,10 +868,10 @@ namespace NetworkManager.DBus
                 return writer.CreateMessage();
             }
         }
-        public Task<ObjectPath[]> GetDevicesAsync()
-            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Devices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
-        public Task<ObjectPath[]> GetAllDevicesAsync()
-            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AllDevices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
+        // public Task<ObjectPath[]> GetDevicesAsync()
+        //     => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Devices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
+        // public Task<ObjectPath[]> GetAllDevicesAsync()
+        //     => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AllDevices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
         public Task<ObjectPath[]> GetCheckpointsAsync()
             => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Checkpoints"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
         public Task<bool> GetNetworkingEnabledAsync()
@@ -3720,8 +3720,8 @@ namespace NetworkManager.DBus
             => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Mode"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
         public Task<uint> GetBitrateAsync()
             => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Bitrate"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
-        public Task<ObjectPath[]> GetAccessPointsAsync()
-            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AccessPoints"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
+        // public Task<ObjectPath[]> GetAccessPointsAsync()
+        //     => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AccessPoints"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
         public Task<ObjectPath> GetActiveAccessPointAsync()
             => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ActiveAccessPoint"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
         public Task<uint> GetWirelessCapabilitiesAsync()
```

When we run `dotnet build` again, the compiler errors are gone.

We update `Program.cs` to the following code which uses the `NetworkManager` service to monitor network devices for state changes.

```C#
using Connection = Tmds.DBus.Protocol.Connection;
using NetworkManager.DBus;
using Tmds.DBus.Protocol;

string? systemBusAddress = DBusAddress.System;
if (systemBusAddress is null)
{
    Console.Write("Can not determine system bus address");
    return 1;
}

Connection connection = new Connection(DBusAddress.System!);
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
        (Exception? ex, (uint NewState, uint OldState, uint Reason) change) =>
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
```

When we run our program and change our network interfaces (e.g. turn on/off WiFi) notifications show up:

```bash
$ dotnet run
Connected to system bus.
Subscribing for state changes of 'lo'.
Subscribing for state changes of 'wlp0s20f3'.
Interface 'wlp0s20f3' changed from '100' to '20'.
```

In the documentation of the StateChanged signal, we find the meaning of the magical constants:
[enum `NMDeviceState`](https://developer-old.gnome.org/NetworkManager/stable/nm-dbus-types.html#NMDeviceState).

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

We'll add the enum to `NetworkManager.DBus.cs` and then update `WatchStateChangedAsync` so it uses `DeviceState` instead of `uint` for the state.

```diff
index eb57d16..663ed69 100644
--- a/NetworkManager.DBus.cs
+++ b/NetworkManager.DBus.cs
@@ -2573,8 +2573,8 @@ namespace NetworkManager.DBus
                 return writer.CreateMessage();
             }
         }
-        public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (uint NewState, uint OldState, uint Reason)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
-            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ReadMessage_uuu(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
+        public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (DeviceState NewState, DeviceState OldState, uint Reason)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
+            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ((DeviceState, DeviceState, uint))ReadMessage_uuu(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
         public Task SetUdiAsync(string value)
         {
             return this.Connection.CallMethodAsync(CreateMessage());
@@ -5792,4 +5792,21 @@ namespace NetworkManager.DBus
         public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;
         public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
     }
+
+    enum DeviceState : uint
+    {
+        Unknown = 0,
+        Unmanaged = 10,
+        Unavailable = 20,
+        Disconnected = 30,
+        Prepare = 40,
+        Config = 50,
+        NeedAuth = 60,
+        IpConfig = 70,
+        IpCheck = 80,
+        Secondaries = 90,
+        Activated = 100,
+        Deactivating = 110,
+        Failed = 120
+    }
 }
```

Now, we update `Program.cs` to use `DeviceState`:
```cs
    await device.WatchStateChangedAsync(
        (Exception? ex, (DeviceState NewState, DeviceState OldState, uint Reason) change) =>
        {
            if (ex is null)
            {
                Console.WriteLine($"Interface '{interfaceName}' changed from '{change.OldState}' to '{change.NewState}'.");
            }
        });
```

When we run our application again, we see more meaningful messages.

```bash
$ dotnet run
Connected to system bus.
Subscribing for state changes of 'lo'.
Subscribing for state changes of 'wlp0s20f3'.
Interface 'wlp0s20f3' changed from 'Activated' to 'Unavailable'.
```

The resulting application is compatible with NativeAOT and trimming. To publish it as a NativeAOT application, run:

```bash
$ dotnet publish /p:PublishAot=true
```

# CI Packages

CI NuGet packages are built from the `main` branch and pushed to the https://www.myget.org/F/tmds/api/v3/index feed.

NuGet.Config:
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="tmds" value="https://www.myget.org/F/tmds/api/v3/index.json" />
  </packageSources>
</configuration>
```

To add a package using `dotnet`:

```
dotnet add package --prerelease Tmds.DBus.Protocol
```

This will add the package to your `csproj` file and use the latest available version.

You can change the package `Version` in the `csproj` file to `*-*`. Then it will restore newer versions when they become available on the CI feed.

# Further Reading

* [D-Bus](docs/dbus.md): Short overview of D-Bus.
* [Tmds.DBus Modelling](docs/modelling.md): Describes how to model D-Bus types in C# for use with Tmds.DBus.
* [Tmds.DBus.Tool](docs/tool.md): Documentation of dotnet dbus tool.
* [How-to](docs/howto.md): Documents some (advanced) use-cases.
* [Tmds.DBus.Protocol](docs/protocol.md): Documentation of the protocol library.

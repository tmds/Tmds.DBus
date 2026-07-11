[![NuGet](https://img.shields.io/nuget/v/Tmds.DBus.Protocol.svg)](https://www.nuget.org/packages/Tmds.DBus.Protocol)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Tmds.DBus.Protocol)](https://www.nuget.org/packages/Tmds.DBus.Protocol)
[![GitHub](https://img.shields.io/badge/GitHub-tmds%2FTmds.DBus-blue?logo=github)](https://github.com/tmds/Tmds.DBus)
[![License](https://img.shields.io/github/license/tmds/Tmds.DBus)](https://github.com/tmds/Tmds.DBus/blob/main/COPYING)
![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%206.0%2B-512BD4)

Tmds.DBus provides .NET libraries for communicating over D-Bus.

## What is D-Bus?

From [freedesktop.org](https://www.freedesktop.org/wiki/Software/dbus/):

> D-Bus is a message bus system, a simple way for applications to talk to one another. In addition to interprocess
communication, D-Bus helps coordinate process lifecycle; it makes it simple and reliable to code a "single instance"
application or daemon, and to launch applications and daemons on demand when their services are needed.

D-Bus is widely used on Linux systems. The **system bus** provides access to OS services like NetworkManager, systemd, and Bluetooth. The **session bus** connects applications running in a user's desktop session, enabling features like media player control, notifications, and desktop integration.

A D-Bus service exposes **objects** at specific **paths**. Each object implements one or more **interfaces**, which define **methods** (remote procedure calls), **signals** (event notifications), and **properties** (readable/writable state). Interfaces are described using XML files.

## Tmds.DBus

Tmds.DBus provides two libraries:

- [Tmds.DBus.Protocol](api/Tmds.DBus.Protocol/Tmds.DBus.Protocol.yml): a modern, high-performance D-Bus protocol library. It targets .NET Standard 2.0/2.1 and .NET 6.0+, and is compatible with NativeAOT/trimming (.NET 8+).
- [Tmds.DBus](api/Tmds.DBus/Tmds.DBus.yml): an older library based on [dbus-sharp](https://github.com/mono/dbus-sharp), with async/await support. It targets .NET Standard 2.0 and .NET 6.0+.

`Tmds.DBus.Protocol` has an associated Roslyn source generator `Tmds.DBus.Generator` that creates C# proxy and handler types from D-Bus interface XML files at compile time.

This guide covers `Tmds.DBus.Protocol` and the source generator `Tmds.DBus.Generator`. `Tmds.DBus` is in maintenance mode and should not be used for new projects.

### Sponsoring

Tmds.DBus is open source and free to use under the MIT license. If your organization depends on it, please consider [sponsoring its maintenance](https://github.com/sponsors/tmds).

This isn't a support contract or a license fee — the source stays open and the rules stay simple. Sponsoring is a small, predictable way to help sustain the work that goes into bug fixes, security updates, and new features.

### Contributing and reporting bugs

Found a bug or want to request a feature? Please [open an issue on GitHub](https://github.com/tmds/Tmds.DBus/issues).

We welcome pull requests on [GitHub](https://github.com/tmds/Tmds.DBus)! Unless you're making a trivial change, open an issue to discuss the change before making a pull request. For security vulnerabilities, use [GitHub's private security reporting](https://github.com/tmds/Tmds.DBus/security/advisories/new) instead.

## Connecting to D-Bus

The <xref:Tmds.DBus.Protocol.DBusAddress> class provides the standard bus addresses:

- `DBusAddress.Session` — the per-user session bus (desktop apps, media players, ...).
- `DBusAddress.System` — the system-wide bus (NetworkManager, systemd, ...).

Both return `null` when the corresponding bus is not available.

Create a <xref:Tmds.DBus.Protocol.DBusConnection> with an address from <xref:Tmds.DBus.Protocol.DBusAddress> and call <xref:Tmds.DBus.Protocol.DBusConnection.ConnectAsync>:

```csharp
using Tmds.DBus.Protocol;

using var connection = new DBusConnection(DBusAddress.Session!);
await connection.ConnectAsync();
```

You can pass a <xref:Tmds.DBus.Protocol.DBusConnectionOptions> to configure behavior:

```csharp
var options = new DBusConnectionOptions(DBusAddress.Session!)
{
    AutoConnect = true
};
using var connection = new DBusConnection(options);
```

When <xref:Tmds.DBus.Protocol.DBusConnectionOptions.AutoConnect> is `true`, the connection is established automatically on first use, and <xref:Tmds.DBus.Protocol.DBusConnection.ConnectAsync> does not need to be called. Auto-connect is intended for proxy (consumer) use-cases; service-side features like requesting bus names and sending raw messages are not allowed.

For a shared connection, you can use the static properties <xref:Tmds.DBus.Protocol.DBusConnection.Session> and <xref:Tmds.DBus.Protocol.DBusConnection.System>. These return a shared, auto-connect connection instance.

The <xref:Tmds.DBus.Protocol.DBusConnectionOptions.OnException> callback is invoked when an exception occurs on the connection. Its primary use-case is logging:

```csharp
options.OnException = context => Console.Error.WriteLine($"D-Bus error at {context.Source}: {context.Exception.Message}");
```

<xref:Tmds.DBus.Protocol.DBusConnectionOptions> can be subclassed to override `SetupAsync` and `Teardown`. This lets you customize connection setup — for example, providing a custom `Stream` via <xref:Tmds.DBus.Protocol.DBusConnectionOptions.SetupResult.ConnectionStream>, or controlling file descriptor passing via <xref:Tmds.DBus.Protocol.DBusConnectionOptions.SetupResult.SupportsFdPassing>.

To wait for a connection to close, await <xref:Tmds.DBus.Protocol.DBusConnection.DisconnectedAsync>:

```csharp
Exception? reason = await connection.DisconnectedAsync();
```

It returns `null` on normal disposal, or the `Exception` that caused the disconnect.

To close the connection, call `Dispose`:

```csharp
connection.Dispose();
```

When a connection fails, an exception derived from <xref:Tmds.DBus.Protocol.DBusConnectionException> is thrown:

- <xref:Tmds.DBus.Protocol.DBusConnectFailedException> — thrown when a connection cannot be established.
- <xref:Tmds.DBus.Protocol.DBusConnectionClosedException> — thrown when an operation fails because an established connection was disconnected. The `InnerException` indicates the reason for the close.

## Consuming a D-Bus Service

In this section we build a console application that controls media players using the [MPRIS](https://specifications.freedesktop.org/mpris-spec/latest/) D-Bus interface.

We use `Tmds.DBus.Generator` to automatically create C# types from D-Bus interface XML files. You can find these XML files in system directories like `/usr/share/dbus-1/interfaces/`, in specification repositories, in application source code, or by introspecting running services using the [`dotnet dbus` tool](#the-dotnet-dbus-tool).

The MPRIS Player interface definition is available from the [MPRIS specification repository](https://gitlab.freedesktop.org/mpris/mpris-spec/-/blob/master/spec/org.mpris.MediaPlayer2.Player.xml). Download it and place it in a `dbus-xml` directory.

Create a console application and add the NuGet packages:

```bash
dotnet new console -o MediaPlayerRemote
cd MediaPlayerRemote
dotnet add package Tmds.DBus.Protocol
dotnet add package Tmds.DBus.Generator
```

Configure the project to generate proxy types. Add the XML file as an `AdditionalFiles` element with the `Namespace` and `DBusGeneratorMode` attributes:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Tmds.DBus.Protocol" Version="*" />
    <PackageReference Include="Tmds.DBus.Generator" Version="*" />
  </ItemGroup>
  <ItemGroup>
    <AdditionalFiles Include="dbus-xml/org.mpris.MediaPlayer2.Player.xml" Namespace="Mpris.DBus" DBusGeneratorMode="Proxy" />
  </ItemGroup>
</Project>
```

When the project is built, the source generator creates a `Player` class (derived from the last component of the interface name `org.mpris.MediaPlayer2.Player`) in the `Mpris.DBus` namespace. The class inherits from <xref:Tmds.DBus.Protocol.DBusObject> and contains async methods corresponding to each D-Bus method, signal, and property.

First, update `Program.cs` to connect to the session bus:

```csharp
using Tmds.DBus.Protocol;

using var connection = new DBusConnection(DBusAddress.Session ?? throw new InvalidOperationException("No session bus"));
await connection.ConnectAsync();
Console.WriteLine("Connected to the session bus.");
```

D-Bus services register well-known names on the bus. MPRIS players register names starting with `org.mpris.MediaPlayer2.`. We can find the available media players using that naming convention:

```csharp
const string MediaPlayerService = "org.mpris.MediaPlayer2.";
var services = await connection.ListServicesAsync();
var players = services.Where(s => s.StartsWith(MediaPlayerService, StringComparison.Ordinal));
if (!players.Any())
{
    Console.WriteLine("No media players are running.");
    Console.WriteLine("Start a player like 'vlc', 'rhythmbox', 'spotify', ...");
    return;
}
```

We'll use the first available player:

```csharp
string firstPlayer = players.First();
Console.WriteLine($"Using: {firstPlayer}");
```

On D-Bus, well-known names can change owners when services restart or are replaced. Sometimes an application needs to be aware of these changes in ownership. The <xref:Tmds.DBus.Protocol.NameOwnerWatcher> class enables tracking the current owner of a name to ensure all calls are made against the same owner.

```csharp
NameOwnerWatcher watcher = await connection.WatchNameOwnerAsync(firstPlayer);
firstPlayer = await watcher.WaitForOwnerAsync();
```

The <xref:Tmds.DBus.Protocol.NameOwnerWatcher> provides <xref:Tmds.DBus.Protocol.NameOwnerWatcher.GetCurrentOwner> to check the current owner (returns `null` if unowned) and <xref:Tmds.DBus.Protocol.NameOwnerWatcher.GetOwnerChangedCancellationToken(System.String)> to get a `CancellationToken` that is cancelled when the owner changes. Call `Dispose` to stop watching.

The <xref:Tmds.DBus.Protocol.DBusService> struct represents a named peer on the bus. We'll use it to reference the first player. The source generator creates `CreateXxx` extension methods for `DBusService` for each interface. We can use the `CreatePlayer` method to get a `Player` instance:

```csharp
using Mpris.DBus;

var mpris = new DBusService(connection, firstPlayer);
var player = mpris.CreatePlayer("/org/mpris/MediaPlayer2");
```

Alternatively, the `Player` can be created directly by calling its constructor (`new Player(connection, firstPlayer, "/org/mpris/MediaPlayer2")`).

We can call the D-Bus methods through the proxy instance:

```csharp
await player.PlayAsync();
await player.PauseAsync();
await player.NextAsync();
await player.SeekAsync(5_000_000); // Seek 5 seconds forward (microseconds)
```

For each readable D-Bus property, there is a `GetXxxAsync` method, and for each writable property a `SetXxxAsync` method:

```csharp
double volume = await player.GetVolumeAsync();
await player.SetVolumeAsync(0.8);
```

To get all properties at once, use `GetPropertiesAsync`.

```csharp
PlayerProperties props = await player.GetPropertiesAsync();
Console.WriteLine($"Volume: {props.Volume}");
Console.WriteLine($"Position: {props.Position}");
```

The get accessors throw if the property was not provided by the peer. Alternatively, you can call the `GetNullablePropertiesAsync` method that returns an `INullableXxxProperties` interface where each property is nullable. Unset properties return `null` instead of throwing.

To subscribe to property change notifications use `WatchPropertiesChangedAsync`. The handler receives an `IChangedXxxProperties` object with `HasXxxChanged` properties to check which properties have changed:

```csharp
await player.WatchPropertiesChangedAsync((IChangedPlayerProperties changed) =>
{
    if (changed.HasVolumeChanged)
    {
        double? volume = changed.Volume;
        Console.WriteLine($"Volume changed: {volume}");
    }
});
```

D-Bus property change notifications may include the new value, or may only indicate the property was _invalidated_ (changed but new value not included). When `HasXxxChanged` is `true`, the `Xxx` property may still return `null` if the new value was not included. In that case, fetch it explicitly:

```csharp
await player.WatchPropertiesChangedAsync(async (IChangedPlayerProperties changed) =>
{
    if (changed.HasVolumeChanged)
    {
        double volume = changed.Volume ?? await player.GetVolumeAsync();
        Console.WriteLine($"Volume changed: {volume}");
    }
});
```

The method returns an `IDisposable`. Dispose it to stop the observer:

```csharp
IDisposable observer = await player.WatchPropertiesChangedAsync(...);
// Later:
observer.Dispose();
```

Each D-Bus signal generates a `WatchXxxAsync` method. There are two forms: a simple one that provides the signal arguments directly, and an advanced one that wraps them in a <xref:Tmds.DBus.Protocol.Notification`1> which also signals completions:

```csharp
// Simple: called with signal arguments directly.
IDisposable observer = await player.WatchSeekedAsync((long position) =>
{
    Console.WriteLine($"Seeked to: {position}");
});

// Advanced: receives a Notification<T> that can also indicate completions.
IDisposable observer = await player.WatchSeekedAsync(
    (Notification<long> notification) =>
    {
        if (notification.IsCompletion)
        {
            Console.WriteLine($"Watch ended: {notification.Type}");
        }
        else
        {
            Console.WriteLine($"Seeked to: {notification.Value}");
        }
    }, ObserverFlags.EmitOnConnectionClosed);
```

The <xref:Tmds.DBus.Protocol.ObserverFlags> enum controls which completion notifications are delivered to the `Notification<T>` handler:

- `EmitOnConnectionClosed` — notifies when the connection is closed.
- `EmitOnObserverDispose` — notifies when the observer is disposed.
- `EmitOnOwnerChanged` — notifies when the owner of the matched bus name changes. Only emitted when a `NameOwnerWatcher` is used.
- `EmitOnConnectionFailed` — notifies when the connection fails.
- `EmitOnReaderFailed` — notifies when reading a signal message fails.
- `EmitAll` — enables all of the above.

By default, subscribing to a signal adds a match rule on the bus. Pass `ObserverFlags.NoSubscribe` to skip the match rule — for example for monitoring messages that are received without making a subscription for them.

When `IsCompletion` is true, the `Exception` property on the notification contains an `Exception` instance that is suitable for the completion. It can for example be used with `TaskCompletionSource.TrySetException`.

You can call `Stop` on the notification from within the handler to stop the observer. No further notifications will be delivered and no completion notification is sent for the stop.

Signal and property change handlers may be `async`. Note that async continuations can run in parallel: the library does not wait for one handler invocation to complete before delivering the next notification.

Method calls and signal reads can throw exceptions derived from <xref:Tmds.DBus.Protocol.DBusMessageException>:

- <xref:Tmds.DBus.Protocol.DBusErrorReplyException> — the remote service returned a D-Bus error reply. The `ErrorName` and `ErrorMessage` properties contain the error details.
- <xref:Tmds.DBus.Protocol.DBusOwnerChangedException> — the owner of the well-known name is known to have changed (when using a `NameOwnerWatcher`).
- <xref:Tmds.DBus.Protocol.DBusUnexpectedValueException> — a received message contains a value that doesn't match expectations (e.g. an unexpected type or out-of-range value).

### Complete example

Putting it all together into a media player remote control:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Mpris.DBus;

Console.WriteLine("MediaPlayerRemote Sample");

// Connect to the session bus.
using var connection = new DBusConnection(DBusAddress.Session!);
await connection.ConnectAsync();

// Find all media players by listing all services and finding those
// with the org.mpris.MediaPlayer2. prefix.
const string MediaPlayerService = "org.mpris.MediaPlayer2.";
var services = await connection.ListServicesAsync();
var availablePlayers = services.Where(service => service.StartsWith(MediaPlayerService, StringComparison.Ordinal));
if (!availablePlayers.Any())
{
    Console.WriteLine("No media players are running");
    Console.WriteLine("Start a player like 'vlc', 'rhythmbox', 'spotify', ...");
    return;
}
Console.WriteLine("Available players:");
foreach (var p in availablePlayers)
{
    Console.WriteLine($"* {p.Substring(MediaPlayerService.Length)}");
}

// Use the first available player.
string firstPlayer = availablePlayers.First();
Console.WriteLine($"Using: {firstPlayer}");

// Track the owner so calls are bound to a specific instance.
NameOwnerWatcher watcher = await connection.WatchNameOwnerAsync(firstPlayer);
firstPlayer = await watcher.WaitForOwnerAsync();

var mpris = new DBusService(connection, firstPlayer);
var player = mpris.CreatePlayer("/org/mpris/MediaPlayer2");

string? currentTitle = null;

void UpdateTitle(string title)
{
    if (currentTitle != title)
    {
        currentTitle = title;
        Console.WriteLine($"Current track: {title}");
    }
}

// Watch for track changes.
await player.WatchPropertiesChangedAsync(async (IChangedPlayerProperties props) =>
{
    if (props.HasMetadataChanged)
    {
        try
        {
            Dictionary<string, VariantValue> metadata = props.Metadata ?? await player.GetMetadataAsync();
            UpdateTitle(GetTitle(metadata));
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while handling track change: {e}");
        }
    }
});

// Get and display the current track.
var metadata = await player.GetMetadataAsync();
UpdateTitle(GetTitle(metadata));

// Control the player.
Console.WriteLine();
Console.WriteLine("Controls:");
Console.WriteLine("* P or Left Arrow:  Previous Song");
Console.WriteLine("* N or Right Arrow: Next Song");
Console.WriteLine("* Spacebar:         Play/Pause");
while (true)
{
    var key = await ReadConsoleKeyAsync();
    switch (key)
    {
        case ConsoleKey.P:
        case ConsoleKey.LeftArrow:
            await player.PreviousAsync();
            break;
        case ConsoleKey.N:
        case ConsoleKey.RightArrow:
            await player.NextAsync();
            break;
        case ConsoleKey.Spacebar:
            await player.PlayPauseAsync();
            break;
    }
}

async Task<ConsoleKey> ReadConsoleKeyAsync()
{
    await Task.Yield();
    return Console.ReadKey(true).Key;
}

static string GetTitle(Dictionary<string, VariantValue> metadata)
    => metadata.TryGetValue("xesam:title", out VariantValue value)
       && value.Type == VariantValueType.String
        ? value.GetString()
        : "???";
```

## Exposing a D-Bus Service

In this section we implement a D-Bus service using the source generator's handler mode. We'll create a simplified media player service with playback controls and properties.

Create a console application and add the NuGet packages:

```bash
dotnet new console -o Player
cd Player
dotnet add package Tmds.DBus.Protocol
dotnet add package Tmds.DBus.Generator
```

### Defining the interface

Create a file `dbus-xml/org.example.Player.xml`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<node>
  <interface name="org.example.Player">
    <method name="PlayPause" />
    <method name="Next" />
    <property name="Volume" type="d" access="readwrite" />
    <property name="Title" type="s" access="read" />
    <property name="Status" type="s" access="read" />
  </interface>
</node>
```

This interface defines:
- `PlayPause`: toggles between playing and paused.
- `Next`: skips to the next track.
- `Volume`: read-write property for the playback volume (0.0–1.0).
- `Title`: read-only property with the current track title.
- `Status`: read-only property with the playback state (`"Playing"` or `"Paused"`).

### Configuring the generator

Configure the project to generate handler types. Add the XML file as an `AdditionalFiles` element with the `Namespace` and `DBusGeneratorMode` attributes:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Tmds.DBus.Protocol" Version="*" />
    <PackageReference Include="Tmds.DBus.Generator" Version="*" />
  </ItemGroup>
  <ItemGroup>
    <AdditionalFiles Include="dbus-xml/org.example.Player.xml"
                     Namespace="Example.DBus"
                     DBusGeneratorMode="Handler" />
  </ItemGroup>
</Project>
```

### What the generator produces

The source generator creates:

- **`IPlayerHandler`**: an interface with method stubs you implement (`PlayPauseAsync`, `NextAsync`) and property handlers (`HandleGetPropertyAsync`, `HandleGetAllPropertiesAsync`, `HandleSetPropertyAsync`).
- **`DBusHandler`**: an abstract base class implementing `IPathMethodHandler` that routes incoming D-Bus method calls to the `IPlayerHandler` method implementations. It handles dispatching, introspection, error handling, and `SynchronizationContext` marshalling.
- **`PlayerSignal`**: a static class with extension methods on `DBusConnection` for emitting property change notifications (`EmitPropertyChanged`, `EmitPropertiesChanged`).
- **`IPlayerProperties`**, **`PlayerProperty`**: types for property access. `IPlayerProperties` has `{ get; }` accessors for read-only properties and `{ get; set; }` for read-write properties.

### Implementing the handler

First, create a `Player` class that holds the player state. This class is independent of the D-Bus connection, so its state survives connection restarts. Because `Timer` callbacks run on a thread pool thread, `Next()` may be called concurrently with D-Bus method handlers. A lock protects shared state:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

class Player : INotifyPropertyChanged, IDisposable
{
    private static readonly string[] Playlist = [ "Track 1", "Track 2", "Track 3" ];

    private readonly Timer _timer;
    private int _trackIndex;
    private bool _isPlaying;

    public object SyncRoot { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public Player()
    {
        _timer = new Timer(_ => Next(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose() => _timer.Dispose();

    public double Volume { get; set; } = 0.5;
    public string Title => Playlist[_trackIndex];
    public string Status => _isPlaying ? "Playing" : "Paused";

    public void PlayPause()
    {
        lock (SyncRoot)
        {
            _isPlaying = !_isPlaying;
            _timer.Change(_isPlaying ? TimeSpan.FromSeconds(5) : Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(5));
            OnPropertyChanged(nameof(Status));
        }
    }

    public void Next()
    {
        lock (SyncRoot)
        {
            _trackIndex = (_trackIndex + 1) % Playlist.Length;
            OnPropertyChanged(nameof(Title));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

Next, create a class that extends `DBusHandler` and implements `IPlayerHandler`. It delegates to the `Player` for state and implements `IPlayerProperties` so the property context handlers can read and write values. It subscribes to the `PropertyChanged` event to emit D-Bus property change notifications. The handler locks on `SyncRoot` to ensure consistent reads and writes:

```csharp
using Tmds.DBus.Protocol;
using Example.DBus;

class PlayerHandler : DBusHandler, IPlayerHandler, IPlayerProperties, IDisposable
{
    private readonly Player _player;

    public PlayerHandler(Player player, DBusConnection connection)
        : base(connection, path: "/org/example/Player", handlesChildPaths: false)
    {
        _player = player;
        _player.PropertyChanged += OnPropertyChanged;
    }

    public void Dispose()
    {
        _player.PropertyChanged -= OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Enum.TryParse<PlayerProperty>(e.PropertyName, out var property))
        {
            lock (_player.SyncRoot)
            {
                Connection.EmitPropertyChanged(Path, this, property);
            }
        }
    }

    public ValueTask PlayPauseAsync()
    {
        _player.PlayPause();
        return default;
    }

    public ValueTask NextAsync()
    {
        _player.Next();
        return default;
    }

    public ValueTask HandleGetPropertyAsync(
        IPlayerHandler.GetPropertyContext context)
        => context.Handle(this);

    public ValueTask HandleGetAllPropertiesAsync(
        IPlayerHandler.GetAllPropertiesContext context)
    {
        lock (_player.SyncRoot)
        {
            return context.Handle(this);
        }
    }

    public ValueTask HandleSetPropertyAsync(
        IPlayerHandler.SetPropertyContext context)
        => context.Handle(this);

    string IPlayerProperties.Title => _player.Title;
    string IPlayerProperties.Status => _player.Status;
    double IPlayerProperties.Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0.0, 1.0);
    }
}
```

The handler implements `IPlayerProperties` to expose property values. Read-only properties (`Title`, `Status`) need only a getter; read-write properties (`Volume`) need both a getter and a setter. The `Handle` methods on the property contexts use `IPlayerProperties` to read and write property values, and send the appropriate D-Bus reply. This provides a convenient default implementation — you can also handle properties individually by switching on the `Property` field and calling the reply methods (`ReplyVolume`, `ReplyTitle`, `ReplyStatus`).

### Registering the handler and requesting a name

Register the handler with the connection and request a well-known bus name so clients can discover the service. Because the `Player` state is separate from the handler, the same instance can be reused across connection restarts:

```csharp
using Tmds.DBus.Protocol;
using Example.DBus;

using var player = new Player();

while (true)
{
    using var connection = new DBusConnection(DBusAddress.Session!);
    await connection.ConnectAsync();

    using var handler = new PlayerHandler(player, connection);
    connection.AddMethodHandler(handler);

    // Throws if the name cannot be acquired.
    await connection.RequestNameAsync("org.example.Player", RequestNameOptions.ReplaceExisting);

    Console.WriteLine("Player service is running.");
    Exception? reason = await connection.DisconnectedAsync();
    Console.WriteLine($"Connection lost: {reason}. Reconnecting...");
}
```

### Try the player

After starting the .NET application, you can use [busctl](https://www.freedesktop.org/software/systemd/man/latest/busctl.html) to interact with the running service from a terminal:

```bash
# Call methods.
busctl --user call org.example.Player /org/example/Player org.example.Player PlayPause
busctl --user call org.example.Player /org/example/Player org.example.Player Next

# Read a property.
busctl --user get-property org.example.Player /org/example/Player org.example.Player Status

# Set a property.
busctl --user set-property org.example.Player /org/example/Player org.example.Player Volume d 0.8

# Monitor signals (property changes).
busctl --user monitor org.example.Player
```

You can also introspect the service to see its interfaces:

```bash
busctl --user introspect org.example.Player /org/example/Player
```

### Emitting property change notifications

The generated `PlayerSignal` class provides extension methods on `DBusConnection` for emitting D-Bus property change signals:

```csharp
// Emit a single property change notification.
Connection.EmitPropertyChanged(Path, this, PlayerProperty.Status);

// Emit multiple property changes at once.
Connection.EmitPropertiesChanged(Path, this,
    stackalloc[] { PlayerProperty.Title, PlayerProperty.Status });
```

There is also an `EmitPlayerPropertiesChanged` extension method that takes optional named parameters per property, so you can emit specific values without implementing `IPlayerProperties`:

```csharp
Connection.EmitPlayerPropertiesChanged(Path, status: "Playing", volume: 0.75);
```

### Overriding cross-cutting behavior

`DBusHandler` provides virtual methods you can override:

**`InvokeAsync`**: wraps every method invocation. Override to add logging, metrics, or custom error handling:

```csharp
protected override async ValueTask InvokeAsync(DBusMethod method, MethodContext context)
{
    Console.WriteLine($"Method called: {context.Request.MemberAsString}");
    await base.InvokeAsync(method, context);
}
```

**`HandleException`**: controls how exceptions are translated to D-Bus error replies:

```csharp
protected override void HandleException(MethodContext context, Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    base.HandleException(context, ex);
}
```

By default, `DBusErrorReplyException` is forwarded as-is, `ArgumentException` maps to `org.freedesktop.DBus.Error.InvalidArgs`, and other exceptions map to `org.freedesktop.DBus.Error.Failed`.

### Handling child paths

By default a handler is registered at a single path. Setting `handlesChildPaths: true` in the `DBusHandler` constructor makes the handler respond to its registered path **and all descendant paths**. This is useful to avoid having to implement each D-Bus object instances as a .NET instance.

```csharp
class DeviceManagerHandler : DBusHandler, IDeviceManagerHandler
{
    public DeviceManagerHandler(DBusConnection connection)
        : base(connection, path: "/org/example/devices", handlesChildPaths: true)
    { }

    // All calls to /org/example/devices, /org/example/devices/1, etc. arrive here.
    // Use context.Request.PathAsString to determine which object is being addressed.
}
```

The path routing follows these rules:

- An **exact match** always wins. If a handler is registered at `/org/example/devices/1`, it takes priority over a tree handler at `/org/example/devices`.
- When no exact match exists, the framework walks **up** the path hierarchy and returns the first ancestor handler that has `HandlesChildPaths` set.
- You **cannot** register a child handler under an existing tree handler, and you cannot register a tree handler when child handlers already exist. Attempting either throws an exception.

**Filtering interfaces by path.** Override `SupportsInterface` to control which interfaces are available at each child path. The default returns `true` for all interfaces implemented on the .NET type.

```csharp
protected override bool SupportsInterface(DBusInterface dbusInterface, ReadOnlySpan<char> path)
{
    if (path is "/org/example/devices")
        return dbusInterface == DBusInterface.OrgExampleDeviceManager;
    return dbusInterface == DBusInterface.OrgExampleDevice;
}
```

## Working with Variants

D-Bus has a **variant** type (`v`) that can hold any D-Bus value. Variants appear frequently in D-Bus APIs: properties dictionaries are typically `a{sv}` (dictionary of string to variant), and metadata is often encoded the same way.

To represent variants in a trim-safe, AOT-compatible, and round-trippable way, `Tmds.DBus.Protocol` uses a dedicated <xref:Tmds.DBus.Protocol.VariantValue> struct type.

`VariantValue` avoids copies when possible. Do not modify data used to construct a `VariantValue` until that value has been written. Similarly, data returned by methods like `GetArray` may return the underlying storage; modifying it may affect other users of the object.

### Reading variant values

Check the type and extract the value:

```csharp
VariantValue v = ...;

switch (v)
{
    case { Type: VariantValueType.String }:
        string s = v.GetString();
        break;
    case { Type: VariantValueType.Int32 }:
        int i = v.GetInt32();
        break;
    case { Type: VariantValueType.Double }:
        double d = v.GetDouble();
        break;
    case { Type: VariantValueType.Bool }:
        bool b = v.GetBool();
        break;
    case { ItemType: VariantValueType.Int32 }:
        int[] array = v.GetArray<int>();
        break;
    case { KeyType: VariantValueType.String, ValueType: VariantValueType.VariantValue }:
        Dictionary<string, VariantValue> dict = v.GetDictionary<string, VariantValue>();
        break;
}
```

### Creating variant values

#### Simple types

Simple types have implicit conversions:

```csharp
VariantValue v1 = (byte)1;
VariantValue v2 = "hello";
VariantValue v3 = 42;
VariantValue v4 = 3.14;
VariantValue v5 = true;
VariantValue v6 = new ObjectPath("/org/example");
```

They can also be created using static factory methods. This is useful when the implicit conversion would pick the wrong type (e.g. `1` becomes `Int32`), or if you prefer to be explicit about the types:

```csharp
VariantValue v1 = VariantValue.Byte(1);
VariantValue v2 = VariantValue.String("hello");
VariantValue v3 = VariantValue.Int32(42);
```

#### Composite types

The library includes `Array<T>`, `Dict<TKey, TValue>`, and `Struct<T1, ...>` types that enable creating arbitrary composed types. They support collection initializer syntax and implicitly convert to `VariantValue`. For some composite D-Bus types, the use of these .NET types is optional and they can be created directly using the static `Array` and `Struct` methods on `VariantValue` as described below.

Arrays:
```csharp
VariantValue v = new Array<int>() { 1, 2, 3 };
```

Dictionaries:
```csharp
VariantValue v = new Dict<string, VariantValue>()
{
    { "name", "Alice" },
    { "age", 30 },
};
```

Structs:
```csharp
VariantValue v = Struct.Create("hello", 42);
```

These types can be nested to build complex structures:
```csharp
VariantValue v = Struct.Create((byte)1, Struct.Create("nested", "struct"));
```

`VariantValue` provides factory methods for creating arrays from typed C# arrays or `List<T>`. Supported element types: `byte`, `bool`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `double`, `string`, `ObjectPath`, `Signature`, and `SafeHandle`.

```csharp
VariantValue v = VariantValue.Array(new int[] { 1, 2, 3 });
VariantValue v = VariantValue.Array(new List<string>() { "a", "b" });
```

For arrays of variants (D-Bus type `av`), use `ArrayOfVariant`:
```csharp
VariantValue v = VariantValue.ArrayOfVariant(new VariantValue[] { 1, "hello", true });
```

Structs can also be created using `VariantValue` arguments:
```csharp
VariantValue v = VariantValue.Struct("hello", 42);
```

Nested variants (D-Bus type `v`) wrap another `VariantValue`:
```csharp
VariantValue v = VariantValue.Variant(42);
```

## The `dotnet dbus` Tool

`Tmds.DBus.Tool` is a .NET global tool for exploring D-Bus and generating code. Install it with:

```bash
dotnet tool install -g Tmds.DBus.Tool
```

| Command | Description |
|---------|-------------|
| `dotnet dbus list` | List D-Bus services, objects, or interfaces. Subcommands: `services`, `activatable-services`, `objects`, `interfaces`. Can also list interfaces from XML files. |
| `dotnet dbus codegen` | Generate C# proxy code by introspecting a live service or from XML interface files. Intended for the `Tmds.DBus` library; for `Tmds.DBus.Protocol`, using the Roslyn source generator is recommended. |
| `dotnet dbus monitor` | Watch D-Bus traffic in real time (method calls, returns, errors, signals). |

All commands accept `--bus session|system|<address>` to select the bus (default: `session`). Use `dotnet dbus <command> --help` for the full list of options.

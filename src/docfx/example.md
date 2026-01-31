# Example

In this section we build an example console application that controls media players using the MPRIS D-Bus interface.
The application will list available media players, display the currently playing track, and provide keyboard controls to play/pause and skip tracks.

We'll use the `Tmds.DBus.Protocol.SourceGenerator` to automatically generate C# code from D-Bus XML interface definitions. Alternatively, you can use the [Tmds.DBus.Tool](tool.md) to generate code manually from the command line.

We use the dotnet CLI to create a new console application:

```bash
$ dotnet new console -o MediaPlayerRemote
$ cd MediaPlayerRemote
```

Now we add a reference to `Tmds.DBus.Protocol` and the source generator:

```bash
$ dotnet add package Tmds.DBus.Protocol
$ dotnet add package Tmds.DBus.Protocol.SourceGenerator
```

Next, we need to obtain the D-Bus interface XML file for the MPRIS Player interface. The MPRIS (Media Player Remote Interfacing Specification) interface definitions are available from the [MPRIS specification repository](https://gitlab.freedesktop.org/mpris/mpris-spec/).

For this example, we'll use [`org.mpris.MediaPlayer2.Player.xml`](https://gitlab.freedesktop.org/mpris/mpris-spec/-/blob/master/spec/org.mpris.MediaPlayer2.Player.xml) which provides the playback control interface.

Create a `dbus-xml` directory and place the XML file there.

Now we'll configure the source generator in the project file. Edit `MediaPlayerRemote.csproj` and add the XML file as an `AdditionalFile`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Tmds.DBus.Protocol" Version="*" />
    <PackageReference Include="Tmds.DBus.Protocol.SourceGenerator" Version="*" />
  </ItemGroup>
  <ItemGroup>
    <AdditionalFiles Include="dbus-xml/org.mpris.MediaPlayer2.Player.xml" Namespace="Mpris.DBus" GenerateDBusTypes="true" />
  </ItemGroup>
</Project>
```

When we build the project, the source generator automatically creates C# classes from the XML interface definitions. Unlike the tool-based approach, there's no need for manual code generation or fixing compilation errors.

Now we can update `Program.cs` to implement our media player remote:

```C#
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

// Find all media players by listing all services and finding those with the org.mpris.MediaPlayer2. prefix.
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

var mpris = new DBusService(connection, firstPlayer);
var player = mpris.CreatePlayer("/org/mpris/MediaPlayer2");

// Get and display the current track.
var metadata = await player.GetMetadataAsync();
string currentTrack = GetTitle(metadata);
Console.WriteLine($"Current track: {currentTrack}");

// Watch for track changes.
await player.WatchPropertiesChangedAsync(async (Exception? ex, IPlayerProperties props) =>
{
    if (ex is not null)
    {
        // Watching stopped due to an exception.
        return;
    }

    if (props.HasMetadataChanged)
    {
        try
        {
            Dictionary<string, VariantValue> metadata = props.Metadata ?? await player.GetMetadataAsync();
            string newTrack = GetTitle(metadata);
            Console.WriteLine($"Now playing: {newTrack}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while handling track change: {e}");
        }
    }
});

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
    => metadata.TryGetValue("xesam:title", out VariantValue value) && value.Type == VariantValueType.String ? value.GetString() : "???";
```

When we run the program with a media player already running (like VLC):

```bash
$ dotnet run
MediaPlayerRemote Sample
Available players:
* vlc
Using: org.mpris.MediaPlayer2.vlc
Current track: Bohemian Rhapsody

Controls:
* P or Left Arrow:  Previous Song
* N or Right Arrow: Next Song
* Spacebar:         Play/Pause
```

As you control the media player using the keyboard, the application responds to track changes and displays the new track titles.


The resulting application is compatible with NativeAOT and trimming. To publish it as a NativeAOT application, run:

```bash
$ dotnet publish /p:PublishAot=true
```

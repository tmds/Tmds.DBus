using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Mpris.DBus;

Console.WriteLine("MediaPlayerRemote Sample");

string? CurrentTitle = null;

// Connect to the session bus.
using var connection = new Connection(Address.Session!);
await connection.ConnectAsync();

// Find all players.
const string MediaPlayerService = "org.mpris.MediaPlayer2.";
var services = await connection.ListServicesAsync();
var availablePlayers = services.Where(service => service.StartsWith(MediaPlayerService, StringComparison.Ordinal));
if (!availablePlayers.Any())
{
    Console.WriteLine("No media players are running");
    Console.WriteLine("Start a player like 'vlc', 'xmms2', 'bmp', 'audacious', ...");
    return;
}
Console.WriteLine("Available players:");
foreach (var p in availablePlayers)
{
    Console.WriteLine($"* {p.Substring(MediaPlayerService.Length)}");
}

// Use the first.
string firstPlayer = availablePlayers.First();
Console.WriteLine($"Using: {firstPlayer}");
var playerService = new MprisService(connection,firstPlayer);
var player = playerService.CreatePlayer("/org/mpris/MediaPlayer2");

// Try list tracks (if the player supports it).
try
{
    var trackList = playerService.CreateTrackList("/org/mpris/MediaPlayer2");
    Console.WriteLine("Tracks:");
    var trackIds = await trackList.GetTracksAsync();
    var trackMetadatas = await trackList.GetTracksMetadataAsync(trackIds);
    foreach (var trackMetadata in trackMetadatas)
    {
        Console.WriteLine($"* {GetTitle(trackMetadata)}");
    }
}
catch (DBusException)
{ }

// Subscribe for title changes, and get the current title.
await player.WatchPropertiesChangedAsync(OnPropertiesChanged);
var metadata = await player.GetMetadataAsync();
UpdateCurrentTitle(metadata, initial: true);

// Control the player.
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
    await Task.Yield(); // Don't block the continuation.
    return Console.ReadKey(true).Key; // block a ThreadPool thread instead.
}

string GetTitle(Dictionary<string, object> metadata)
    => (metadata.TryGetValue("xesam:title", out object? value) ? value.ToString() : null) ?? "???";

void UpdateCurrentTitle(Dictionary<string, object> metadata, bool initial = false)
{
    if (initial && CurrentTitle is not null)
    {
        return;
    };
    var title = GetTitle(metadata);
    if (CurrentTitle != title)
    {
        Console.WriteLine($"Current track: {title}");
        CurrentTitle = title;
    }
}

async void OnPropertiesChanged(Exception? ex, Player player, PropertyChanges<PlayerProperties> changes)
{
    if (ex is not null)
    {
        return;
    }

    try
    {
        Dictionary<string, object>? metadata = null;
        if (changes.HasChanged(nameof(PlayerProperties.Metadata)))
        {
            metadata = changes.Properties.Metadata;
        }
        else if (changes.IsInvalidated(nameof(PlayerProperties.Metadata)))
        {
            metadata = await player.GetMetadataAsync();
        }
        if (metadata is not null)
        {
            UpdateCurrentTitle(metadata);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error while handling player properties changed: {e}");
    }
}

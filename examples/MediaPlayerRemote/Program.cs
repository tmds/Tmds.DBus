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

var mpris = new DBusService(connection, firstPlayer);

Player player = new Player(mpris);
foreach (var track in await player.GetTracksAsync())
{
    Console.WriteLine($"* {track}");
}

// Print the title that is playing.
await player.WatchTitleAsync(title => Console.WriteLine($"Current track: {title}"));

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

class Player
{
    private readonly DBusService _mprisService;
    private readonly Mpris.DBus.Player _player;

    public Player(DBusService mprisService)
    {
        _mprisService = mprisService;
        _player = mprisService.CreatePlayer("/org/mpris/MediaPlayer2");
    }

    public async Task<IDisposable> WatchTitleAsync(Action<string> action, bool emitOnCapturedContext = false)
    {
        string? CurrentTitle = null;

        // Subscribe for changes.
        IDisposable watcher = await _player.WatchPropertiesChangedAsync(OnPropertiesChanged, emitOnCapturedContext).ConfigureAwait(emitOnCapturedContext);

        // Get the current title.
        var metadata = await _player.GetMetadataAsync().ConfigureAwait(emitOnCapturedContext);
        UpdateCurrentTitle(metadata, initial: true);

        return watcher;

        async void OnPropertiesChanged(Exception? ex, PlayerProperties properties)
        {
            if (ex is not null)
            {
                return;
            }

            try
            {
                Dictionary<string, VariantValue>? metadata = null;
                if (properties.IsSet(PlayerProperty.Metadata))
                {
                    metadata = properties.Metadata;
                }
                else if (properties.IsInvalidated(PlayerProperty.Metadata))
                {
                    metadata = await _player.GetMetadataAsync();
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

        void UpdateCurrentTitle(Dictionary<string, VariantValue> metadata, bool initial = false)
        {
            if (initial && CurrentTitle is not null)
            {
                // Property changed occurs before we fetched the initial value.
                return;
            };
            var newTitle = GetTitle(metadata);
            if (CurrentTitle != newTitle)
            {
                CurrentTitle = newTitle;
                action(newTitle);
            }
        }
    }

    public async Task<string[]> GetTracksAsync()
    {
        try
        {
            var trackList = _mprisService.CreateTrackList("/org/mpris/MediaPlayer2");
            Console.WriteLine("Tracks:");
            var trackIds = await trackList.GetTracksAsync();
            var trackMetadatas = await trackList.GetTracksMetadataAsync(trackIds);
            List<string> titles = new();
            foreach (var trackMetadata in trackMetadatas)
            {
                titles.Add(GetTitle(trackMetadata));
            }
            return titles.ToArray();
        }
        catch (DBusErrorReplyException)
        {
            // Listing tracks not supported by player.
            return Array.Empty<string>();
        }
    }

    private static string GetTitle(Dictionary<string, VariantValue> metadata)
        => (metadata.TryGetValue("xesam:title", out VariantValue value) ? value.GetString() : null) ?? "???";

    public Task PreviousAsync() => _player.PreviousAsync();

    public Task NextAsync() => _player.NextAsync();

    public Task PlayPauseAsync() => _player.PlayPauseAsync();
}
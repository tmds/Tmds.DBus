using System;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Mpris.DBus;
using System.Collections.Generic;

Console.WriteLine("MediaPlayerRemote Sample");

string? CurrentTitle = null;
Player? player = null;

Task.Run(async () =>
{
    using (var connection = new Connection(Address.Session!))
    {
        const string MediaPlayerService = "org.mpris.MediaPlayer2.";

        await connection.ConnectAsync();
        var services = await connection.ListServicesAsync();
        var players = services.Where(service => service.StartsWith(MediaPlayerService, StringComparison.Ordinal));

        if (!players.Any())
        {
            Console.WriteLine("No media players are running");
            Console.WriteLine("Start a player like 'vlc', 'xmms2', 'bmp', 'audacious', ...");
            return;
        }
        Console.WriteLine("Available players:");
        foreach (var p in players)
        {
            Console.WriteLine($"* {p.Substring(MediaPlayerService.Length)}");
        }

        var playerService = new MprisService(connection, players.First());
        Console.WriteLine($"Using: {playerService}");
        var mediaPlayer = playerService.CreateMediaPlayer2("/org/mpris/MediaPlayer2");
        player = playerService.CreatePlayer("/org/mpris/MediaPlayer2");

        Console.WriteLine("Tracks:");
        var trackList = playerService.CreateTrackList("/org/mpris/MediaPlayer2");
        var trackIds = await trackList.GetTracksAsync();
        var trackMetadatas = await trackList.GetTracksMetadataAsync(trackIds);
        foreach (var trackMetadata in trackMetadatas)
        {
            Console.WriteLine($"* {GetTitle(trackMetadata)}");
        }

        await player.WatchPropertiesChangedAsync(OnPropertiesChanged);
        var metadata = await player.GetMetadataAsync();
        UpdateCurrentTitle(metadata, initial: true);

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
    }
}).Wait();

async Task<ConsoleKey> ReadConsoleKeyAsync()
{
    await Task.Yield();
    return Console.ReadKey(true).Key;
}

string GetTitle(Dictionary<string, object> metadata)
{
    if (metadata.ContainsKey("xesam:title"))
    {
        return metadata["xesam:title"] as string ?? "???";
    }
    else
    {
        return "???";
    }
}

void UpdateCurrentTitle(Dictionary<string, object> metadata, bool initial = false)
{
    if (initial && (CurrentTitle != null))
    {
        return;
    };
    var title = GetTitle(metadata);
    if (CurrentTitle == title)
    {
        return;
    }
    Console.WriteLine($"Current track: {title}");
    CurrentTitle = title;
}

async void OnPropertiesChanged(Exception? ex, PropertyChanges<PlayerProperties> changes)
{
    if (ex is not null)
    {
        return;
    }

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

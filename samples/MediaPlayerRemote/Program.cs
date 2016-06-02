using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

#pragma warning disable 0649 // Field is never assigned to, and will always have its default value

namespace MediaPlayerRemote
{
    class TrackListProperties
    {
        public ObjectPath[] Tracks;
    }

    [DBusInterface("org.mpris.MediaPlayer2.TrackList")]
    public interface ITrackList : IDBusObject
    {
        Task<IDictionary<string, object>[]> GetTracksMetadataAsync(ObjectPath[] trackIds, CancellationToken cancellationToken = default(CancellationToken));

        Task<IDictionary<string, object>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<T> GetAsync<T>(string prop, CancellationToken cancellationToken = default(CancellationToken));
        Task SetAsync(string prop, object val, CancellationToken cancellationToken = default(CancellationToken));
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, CancellationToken cancellationToken = default(CancellationToken));
    }

    class PlayerProperties
    {
        public long Position;
        public string PlaybackStatus;
        public IDictionary<string, object> Metadata;
    }

    [DBusInterface("org.mpris.MediaPlayer2.Player")]
    public interface IPlayer : IDBusObject
    {
        Task PlayPauseAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task NextAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task PreviousAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<IDictionary<string, object>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<T> GetAsync<T>(string prop, CancellationToken cancellationToken = default(CancellationToken));
        Task SetAsync(string prop, object val, CancellationToken cancellationToken = default(CancellationToken));
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, CancellationToken cancellationToken = default(CancellationToken));
    }

    [DBusInterface("org.mpris.MediaPlayer2")]
    public interface IMediaPlayer : IPlayer, ITrackList
    {
        Task QuitAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public class Program
    {
        private static string s_mediaPlayerService = "org.mpris.MediaPlayer2.";
        private static ObjectPath s_mediaPlayerPath = new ObjectPath("/org/mpris/MediaPlayer2");

        public static void Main(string[] args)
        {
            Console.WriteLine("MediaPlayerRemote Sample");

            Task.Run(async () =>
            {
                using (var connection = new Connection(Address.Session))
                {
                    await connection.ConnectAsync();
                    var services = await connection.ListServicesAsync();
                    var players = services.Where(service => service.StartsWith(s_mediaPlayerService));

                    if (!players.Any())
                    {
                        Console.WriteLine("No media players are running");
                        Console.WriteLine("Start a player like 'vlc', 'xmms2', 'bmp', 'audacious', ...");
                        return;
                    }
                    Console.WriteLine("Available players:");
                    foreach (var p in players)
                    {
                        Console.WriteLine($"* {p.Substring(s_mediaPlayerService.Length)}");
                    }

                    var playerService = players.First();
                    Console.WriteLine($"Using: {playerService}");
                    var mediaPlayer = connection.CreateProxy<IMediaPlayer>(playerService, s_mediaPlayerPath);
                    var player = mediaPlayer as IPlayer;
                    var trackList = mediaPlayer as ITrackList;

                    Console.WriteLine("Tracks:");
                    var trackIds = await trackList.GetAsync<ObjectPath[]>(nameof(TrackListProperties.Tracks));
                    var trackMetadatas = await trackList.GetTracksMetadataAsync(trackIds);
                    foreach (var trackMetadata in trackMetadatas)
                    {
                        Console.WriteLine($"* {GetTitle(trackMetadata)}");
                    }

                    await player.WatchPropertiesAsync(OnPropertiesChanged);
                    var metadata = await player.GetAsync<IDictionary<string, object>>(nameof(PlayerProperties.Metadata));
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
        }

        private static async Task<ConsoleKey> ReadConsoleKeyAsync()
        {
            await Task.Yield();
            return Console.ReadKey(true).Key;
        }

        private static void OnPropertiesChanged(PropertyChanges changes)
        {
            var metadata = changes.GetValue<IDictionary<string, object>>(nameof(PlayerProperties.Metadata));
            if (metadata != null)
            {
                UpdateCurrentTitle(metadata);
            }
        }

        private static string s_currentTitle;

        private static void UpdateCurrentTitle(IDictionary<string, object> metadata, bool initial = false)
        {
            if (initial && (s_currentTitle != null))
            {
                return;
            };
            var title = GetTitle(metadata);
            if (s_currentTitle == title)
            {
                return;
            }
            Console.WriteLine($"Current track: {title}");
            s_currentTitle = title;
        }

        private static string GetTitle(IDictionary<string, object> metadata)
        {
            if (metadata.ContainsKey("xesam:title"))
            {
                return metadata["xesam:title"] as string;
            }
            else
            {
                return "???";
            }
        }
    }
}

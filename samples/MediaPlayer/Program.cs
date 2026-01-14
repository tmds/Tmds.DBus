using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

var connection = new DBusConnection(DBusAddress.Session!);
await connection.ConnectAsync();

var mediaPlayer = new DBusMediaPlayer(connection);
string playerName = await mediaPlayer.AddToDBusAsync();
Console.WriteLine($"Media player is available via DBus as '{playerName}'.");

await connection.DisconnectedAsync();
Console.WriteLine("Connection lost. Closing application.");

class DBusMediaPlayer
{
    private const string ObjectPath = "/org/mpris/MediaPlayer2";
    private const string ServiceNamePrefix = "org.mpris.MediaPlayer2";

    private readonly DBusConnection _connection;
    private readonly PathHandler _pathHandler;
    private readonly MediaPlayerHandler _mediaPlayerHandler;
    private readonly MediaPlayerPlayerHandler _playerHandler;
    private bool _emitSignals;

    // The SourceGenerator generates properties which are abstract when writable and non-abstract when not writable.
    // For the non-abstract properties, use the handler property as a backing field.
    // For the abstract properties, introduce a backing field in this class.
    // DBus types are non-nullable, so the properties implemented here are non-nullable as well.

    private bool _fullscreen;
    private bool Fullscreen
    {
        get => _fullscreen;
        set
        {
            _fullscreen = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "Fullscreen", value);
        }
    }

    private string _loopStatus = "None";
    private string LoopStatus
    {
        get => _loopStatus;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _loopStatus = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "LoopStatus", value);
        }
    }

    private double _rate = 1.0;
    private double Rate
    {
        get => _rate;
        set
        {
            _rate = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "Rate", value);
        }
    }

    private bool _shuffle;
    private bool Shuffle
    {
        get => _shuffle;
        set
        {
            _shuffle = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "Shuffle", value);
        }
    }

    private double _volume = 1.0;
    private double Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "Volume", value);
        }
    }

    private string Identity
    {
        get => _mediaPlayerHandler.Identity ?? "";
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _mediaPlayerHandler.Identity = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "Identity", value);
        }
    }

    private bool CanQuit
    {
        get => _mediaPlayerHandler.CanQuit;
        set
        {
            _mediaPlayerHandler.CanQuit = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "CanQuit", value);
        }
    }

    private bool CanRaise
    {
        get => _mediaPlayerHandler.CanRaise;
        set
        {
            _mediaPlayerHandler.CanRaise = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "CanRaise", value);
        }
    }

    private bool HasTrackList
    {
        get => _mediaPlayerHandler.HasTrackList;
        set
        {
            _mediaPlayerHandler.HasTrackList = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "HasTrackList", value);
        }
    }

    private string[] SupportedUriSchemes
    {
        get => _mediaPlayerHandler.SupportedUriSchemes as string[] ?? [];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfAnyElementIsNull(value);
            _mediaPlayerHandler.SupportedUriSchemes = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "SupportedUriSchemes", VariantValue.Array(value));
        }
    }

    private string[] SupportedMimeTypes
    {
        get => _mediaPlayerHandler.SupportedMimeTypes as string[] ?? [];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfAnyElementIsNull(value);
            _mediaPlayerHandler.SupportedMimeTypes = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "SupportedMimeTypes", VariantValue.Array(value));
        }
    }

    private bool CanSetFullscreen
    {
        get => _mediaPlayerHandler.CanSetFullscreen;
        set
        {
            _mediaPlayerHandler.CanSetFullscreen = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "CanSetFullscreen", value);
        }
    }

    private string DesktopEntry
    {
        get => _mediaPlayerHandler.DesktopEntry ?? "";
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _mediaPlayerHandler.DesktopEntry = value;
            EmitPropertyChanged(_mediaPlayerHandler.InterfaceName, "DesktopEntry", value);
        }
    }

    private string PlaybackStatus
    {
        get => _playerHandler.PlaybackStatus ?? "";
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _playerHandler.PlaybackStatus = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "PlaybackStatus", value);
        }
    }

    private double MinimumRate
    {
        get => _playerHandler.MinimumRate;
        set
        {
            _playerHandler.MinimumRate = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "MinimumRate", value);
        }
    }

    private double MaximumRate
    {
        get => _playerHandler.MaximumRate;
        set
        {
            _playerHandler.MaximumRate = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "MaximumRate", value);
        }
    }

    private bool CanGoNext
    {
        get => _playerHandler.CanGoNext;
        set
        {
            _playerHandler.CanGoNext = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "CanGoNext", value);
        }
    }

    private bool CanGoPrevious
    {
        get => _playerHandler.CanGoPrevious;
        set
        {
            _playerHandler.CanGoPrevious = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "CanGoPrevious", value);
        }
    }

    private bool CanPlay
    {
        get => _playerHandler.CanPlay;
        set
        {
            _playerHandler.CanPlay = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "CanPlay", value);
        }
    }

    private bool CanPause
    {
        get => _playerHandler.CanPause;
        set
        {
            _playerHandler.CanPause = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "CanPause", value);
        }
    }

    private bool CanSeek
    {
        get => _playerHandler.CanSeek;
        set
        {
            _playerHandler.CanSeek = value;
            EmitPropertyChanged(_playerHandler.InterfaceName, "CanSeek", value);
        }
    }

    private bool CanControl
    {
        get => _playerHandler.CanControl;
        set
        {
            _playerHandler.CanControl = value;
            // note: no PropertiesChanged signal is emitted for CanControl.
        }
    }

    private Dictionary<string, VariantValue> Metadata
    {
        get => _playerHandler.Metadata ?? throw new InvalidOperationException($"{nameof(Metadata)} should be initialized");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _playerHandler.Metadata = value;
            var dict = new Dict<string, VariantValue>(value);
            EmitPropertyChanged(_playerHandler.InterfaceName, "Metadata", dict);
        }
    }

    private long Position
    {
        get => _playerHandler.Position;
        set
        {
            _playerHandler.Position = value;
            // note: no PropertiesChanged signal is emitted for Position.
        }
    }

    public DBusMediaPlayer(DBusConnection connection)
    {
        _connection = connection;
        _pathHandler = new PathHandler(ObjectPath);
        _mediaPlayerHandler = new MediaPlayerHandler(this) { PathHandler = _pathHandler };
        _playerHandler = new MediaPlayerPlayerHandler(this) { PathHandler = _pathHandler };
        _pathHandler.Add(_mediaPlayerHandler);
        _pathHandler.Add(_playerHandler);

        Identity = "DBus Media Player Example";
        CanQuit = true;
        CanRaise = false;
        CanSetFullscreen = false;
        HasTrackList = false;
        SupportedUriSchemes = ["file"];
        SupportedMimeTypes = ["audio/mpeg", "audio/ogg"];
        PlaybackStatus = "Stopped";
        MinimumRate = 1.0;
        MaximumRate = 1.0;
        CanGoNext = true;
        CanGoPrevious = true;
        CanPlay = true;
        CanPause = true;
        CanSeek = true;
        CanControl = true;
        Position = 0;
        Metadata = new Dictionary<string, VariantValue>
        {
            ["xesam:title"] = "Example Song Title"
        };
    }

    public async Task<string> AddToDBusAsync()
    {
        _connection.AddMethodHandler(_pathHandler);
        _emitSignals = true;
        string name = $"{ServiceNamePrefix}.DBusExample.instance{Environment.ProcessId}";
        await _connection.RequestNameAsync($"{ServiceNamePrefix}.DBusExample.instance{Environment.ProcessId}");
        return name;
    }

    public void Raise()
    {
        Console.WriteLine("Raise requested");
    }

    public void Quit()
    {
        Console.WriteLine("Quit requested");
        Environment.Exit(0);
    }

    public void Next()
    {
        Console.WriteLine("Next requested");
    }

    public void Previous()
    {
        Console.WriteLine("Previous requested");
    }

    public string Pause()
    {
        Console.WriteLine("Pause requested");
        return "Paused";
    }

    public string PlayPause()
    {
        Console.WriteLine("PlayPause requested");
        return PlaybackStatus == "Playing" ? "Paused" : "Playing";
    }

    public string Stop()
    {
        Console.WriteLine("Stop requested");
        return "Stopped";
    }

    public string Play()
    {
        Console.WriteLine("Play requested");
        return "Playing";
    }

    public void Seek(long offset)
    {
        Console.WriteLine($"Seek requested: offset={offset}");
    }

    public void SetPosition(ObjectPath trackId, long position)
    {
        Console.WriteLine($"SetPosition requested: trackId={trackId}, position={position}");
    }

    public void OpenUri(string uri)
    {
        Console.WriteLine($"OpenUri requested: {uri}");
    }

    private void EmitPropertyChanged(string interfaceName, string name, VariantValue value)
    {
        if (!_emitSignals)
        {
            return;
        }
        MessageWriter writer = _connection.GetMessageWriter();
        writer.WriteSignalHeader(null, ObjectPath, "org.freedesktop.DBus.Properties", "PropertiesChanged", "sa{sv}as");
        writer.WriteString(interfaceName);
        writer.WriteDictionary([KeyValuePair.Create(name, value)]);
        writer.WriteArray(Array.Empty<string>());
        _connection.TrySendMessage(writer.CreateMessage());
        writer.Dispose();
    }

    private static void ThrowIfAnyElementIsNull(string?[] value)
    {
        if (Array.IndexOf(value, null) != -1)
        {
            throw new ArgumentException("Array contains null elements.", nameof(value));
        }
    }

    sealed class MediaPlayerHandler(DBusMediaPlayer player) : OrgMprisMediaPlayer2Handler
    {
        private readonly DBusMediaPlayer _player = player;

        public override Connection Connection => _player._connection.AsConnection();

        public override bool Fullscreen
        {
            get => _player.Fullscreen;
            set => _player.Fullscreen = value;
        }

        protected override ValueTask OnRaiseAsync(Message request)
        {
            _player.Raise();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnQuitAsync(Message request)
        {
            _player.Quit();
            return ValueTask.CompletedTask;
        }
    }

    sealed class MediaPlayerPlayerHandler(DBusMediaPlayer player) : OrgMprisMediaPlayer2PlayerHandler
    {
        private readonly DBusMediaPlayer _player = player;

        public override Connection Connection => _player._connection.AsConnection();

        public override string? LoopStatus
        {
            get => _player.LoopStatus;
            set => _player.LoopStatus = value!;
        }

        public override double Rate
        {
            get => _player.Rate;
            set => _player.Rate = value;
        }

        public override bool Shuffle
        {
            get => _player.Shuffle;
            set => _player.Shuffle = value;
        }

        public override double Volume
        {
            get => _player.Volume;
            set => _player.Volume = value;
        }

        protected override ValueTask OnNextAsync(Message request)
        {
            _player.Next();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnPreviousAsync(Message request)
        {
            _player.Previous();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnPauseAsync(Message request)
        {
            PlaybackStatus = _player.Pause();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnPlayPauseAsync(Message request)
        {
            PlaybackStatus = _player.PlayPause();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnStopAsync(Message request)
        {
            PlaybackStatus = _player.Stop();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnPlayAsync(Message request)
        {
            PlaybackStatus = _player.Play();
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnSeekAsync(Message request, long offset)
        {
            _player.Seek(offset);
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnSetPositionAsync(Message request, ObjectPath trackId, long position)
        {
            _player.SetPosition(trackId, position);
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnOpenUriAsync(Message request, string uri)
        {
            _player.OpenUri(uri);
            return ValueTask.CompletedTask;
        }
    }
}
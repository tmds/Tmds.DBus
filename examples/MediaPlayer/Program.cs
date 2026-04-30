using System;
using Tmds.DBus.Protocol;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mpris.DBus;

while (true)
{
    var connection = new DBusConnection(DBusAddress.Session!);
    await connection.ConnectAsync();

    var mediaPlayer = new DBusMediaPlayer(connection);
    string playerName = await mediaPlayer.AddToDBusAsync();
    Console.WriteLine($"Media player is available via DBus as '{playerName}'.");

    Exception? disconnectReason = await connection.DisconnectedAsync();
    Console.WriteLine("Connection lost:");
    Console.WriteLine(disconnectReason);
    Console.WriteLine();
    Console.WriteLine("Reconnecting...");
}

class DBusMediaPlayer : DBusHandler,
    IMediaPlayer2Handler, IMediaPlayer2Properties,
    IPlayerHandler, IPlayerProperties
{
    private const string ObjectPath = "/org/mpris/MediaPlayer2";
    private const string ServiceNamePrefix = "org.mpris.MediaPlayer2";

    // IMediaPlayer2Properties - read-only properties
    public bool CanQuit { get; set; } = true;
    public bool CanSetFullscreen { get; set; }
    public bool CanRaise { get; set; }
    public bool HasTrackList { get; set; }
    public string Identity { get; set; } = "DBus Media Player Example";
    public string DesktopEntry { get; set; } = "";
    public string[] SupportedUriSchemes { get; set; } = ["file"];
    public string[] SupportedMimeTypes { get; set; } = ["audio/mpeg", "audio/ogg"];

    // IMediaPlayer2Properties - writable property with change notification
    private bool _fullscreen;
    public bool Fullscreen
    {
        get => _fullscreen;
        set
        {
            _fullscreen = value;
            Connection.EmitPropertyChanged(ObjectPath, this, MediaPlayer2Property.Fullscreen);
        }
    }

    // IPlayerProperties - read-only properties
    public Dictionary<string, VariantValue> Metadata { get; set; } = new()
    {
        ["xesam:title"] = "Example Song Title"
    };
    public long Position { get; set; }
    public double MinimumRate { get; set; } = 1.0;
    public double MaximumRate { get; set; } = 1.0;
    public bool CanGoNext { get; set; } = true;
    public bool CanGoPrevious { get; set; } = true;
    public bool CanPlay { get; set; } = true;
    public bool CanPause { get; set; } = true;
    public bool CanSeek { get; set; } = true;
    public bool CanControl { get; set; } = true;

    // IPlayerProperties - writable properties with change notification
    private string _playbackStatus = "Stopped";
    public string PlaybackStatus
    {
        get => _playbackStatus;
        set
        {
            _playbackStatus = value;
            Connection.EmitPropertyChanged(ObjectPath, this, PlayerProperty.PlaybackStatus);
        }
    }

    private string _loopStatus = "None";
    public string LoopStatus
    {
        get => _loopStatus;
        set
        {
            _loopStatus = value;
            Connection.EmitPropertyChanged(ObjectPath, this, PlayerProperty.LoopStatus);
        }
    }

    private double _rate = 1.0;
    public double Rate
    {
        get => _rate;
        set
        {
            _rate = value;
            Connection.EmitPropertyChanged(ObjectPath, this, PlayerProperty.Rate);
        }
    }

    private bool _shuffle;
    public bool Shuffle
    {
        get => _shuffle;
        set
        {
            _shuffle = value;
            Connection.EmitPropertyChanged(ObjectPath, this, PlayerProperty.Shuffle);
        }
    }

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            Connection.EmitPropertyChanged(ObjectPath, this, PlayerProperty.Volume);
        }
    }

    public DBusMediaPlayer(DBusConnection connection)
        : base(connection, ObjectPath, handlesChildPaths: false)
    {
    }

    public async Task<string> AddToDBusAsync()
    {
        Connection.AddMethodHandler(this);
        string name = $"{ServiceNamePrefix}.DBusExample.instance{Environment.ProcessId}";
        await Connection.RequestNameAsync(name);
        return name;
    }

    // IMediaPlayer2Handler methods
    ValueTask IMediaPlayer2Handler.RaiseAsync()
    {
        Console.WriteLine("Raise requested");
        return default;
    }

    ValueTask IMediaPlayer2Handler.QuitAsync()
    {
        Console.WriteLine("Quit requested");
        Environment.Exit(0);
        return default;
    }

    // IPlayerHandler methods
    ValueTask IPlayerHandler.NextAsync()
    {
        Console.WriteLine("Next requested");
        return default;
    }

    ValueTask IPlayerHandler.PreviousAsync()
    {
        Console.WriteLine("Previous requested");
        return default;
    }

    ValueTask IPlayerHandler.PauseAsync()
    {
        Console.WriteLine("Pause requested");
        PlaybackStatus = "Paused";
        return default;
    }

    ValueTask IPlayerHandler.PlayPauseAsync()
    {
        Console.WriteLine("PlayPause requested");
        PlaybackStatus = PlaybackStatus == "Playing" ? "Paused" : "Playing";
        return default;
    }

    ValueTask IPlayerHandler.StopAsync()
    {
        Console.WriteLine("Stop requested");
        PlaybackStatus = "Stopped";
        return default;
    }

    ValueTask IPlayerHandler.PlayAsync()
    {
        Console.WriteLine("Play requested");
        PlaybackStatus = "Playing";
        return default;
    }

    ValueTask IPlayerHandler.SeekAsync(long offset)
    {
        Console.WriteLine($"Seek requested: offset={offset}");
        return default;
    }

    ValueTask IPlayerHandler.SetPositionAsync(ObjectPath trackId, long position)
    {
        Console.WriteLine($"SetPosition requested: trackId={trackId}, position={position}");
        return default;
    }

    ValueTask IPlayerHandler.OpenUriAsync(string uri)
    {
        Console.WriteLine($"OpenUri requested: {uri}");
        return default;
    }

    // IMediaPlayer2Handler property methods
    ValueTask IMediaPlayer2Handler.HandleGetPropertyAsync(IMediaPlayer2Handler.GetPropertyContext context)
        => context.Handle(this);

    ValueTask IMediaPlayer2Handler.HandleGetAllPropertiesAsync(IMediaPlayer2Handler.GetAllPropertiesContext context)
        => context.Handle(this);

    ValueTask IMediaPlayer2Handler.HandleSetPropertyAsync(IMediaPlayer2Handler.SetPropertyContext context)
        => context.Handle(this);

    // IPlayerHandler property methods
    ValueTask IPlayerHandler.HandleGetPropertyAsync(IPlayerHandler.GetPropertyContext context)
        => context.Handle(this);

    ValueTask IPlayerHandler.HandleGetAllPropertiesAsync(IPlayerHandler.GetAllPropertiesContext context)
        => context.Handle(this);

    ValueTask IPlayerHandler.HandleSetPropertyAsync(IPlayerHandler.SetPropertyContext context)
        => context.Handle(this);
}

namespace Mpris.DBus
{
    using System;
    using Tmds.DBus.Protocol;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    record PlayerProperties
    {
        public string PlaybackStatus { get; set; } = default!;
        public string LoopStatus { get; set; } = default!;
        public double Rate { get; set; } = default!;
        public bool Shuffle { get; set; } = default!;
        public Dictionary<string, VariantValue> Metadata { get; set; } = default!;
        public double Volume { get; set; } = default!;
        public long Position { get; set; } = default!;
        public double MinimumRate { get; set; } = default!;
        public double MaximumRate { get; set; } = default!;
        public bool CanGoNext { get; set; } = default!;
        public bool CanGoPrevious { get; set; } = default!;
        public bool CanPlay { get; set; } = default!;
        public bool CanPause { get; set; } = default!;
        public bool CanSeek { get; set; } = default!;
        public bool CanControl { get; set; } = default!;
    }
    partial class Player : MprisObject
    {
        private const string __Interface = "org.mpris.MediaPlayer2.Player";
        public Player(MprisService service, ObjectPath path) : base(service, path)
        { }
        public Task NextAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Next");
                return writer.CreateMessage();
            }
        }
        public Task PreviousAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Previous");
                return writer.CreateMessage();
            }
        }
        public Task PauseAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Pause");
                return writer.CreateMessage();
            }
        }
        public Task PlayPauseAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "PlayPause");
                return writer.CreateMessage();
            }
        }
        public Task StopAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Stop");
                return writer.CreateMessage();
            }
        }
        public Task PlayAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Play");
                return writer.CreateMessage();
            }
        }
        public Task SeekAsync(long offset)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "x",
                    member: "Seek");
                writer.WriteInt64(offset);
                return writer.CreateMessage();
            }
        }
        public Task SetPositionAsync(ObjectPath trackId, long position)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ox",
                    member: "SetPosition");
                writer.WriteObjectPath(trackId);
                writer.WriteInt64(position);
                return writer.CreateMessage();
            }
        }
        public Task OpenUriAsync(string uri)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "s",
                    member: "OpenUri");
                writer.WriteString(uri);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchSeekedAsync(Action<Exception?, long> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Seeked", (Message m, object? s) => ReadMessage_x(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
        public Task SetLoopStatusAsync(string value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("LoopStatus");
                writer.WriteSignature("s");
                writer.WriteString(value);
                return writer.CreateMessage();
            }
        }
        public Task SetRateAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Rate");
                writer.WriteSignature("d");
                writer.WriteDouble(value);
                return writer.CreateMessage();
            }
        }
        public Task SetShuffleAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Shuffle");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetVolumeAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Volume");
                writer.WriteSignature("d");
                writer.WriteDouble(value);
                return writer.CreateMessage();
            }
        }
        public Task<string> GetPlaybackStatusAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PlaybackStatus"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<string> GetLoopStatusAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "LoopStatus"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<double> GetRateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Rate"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<bool> GetShuffleAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Shuffle"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<Dictionary<string, VariantValue>> GetMetadataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Metadata"), (Message m, object? s) => ReadMessage_v_aesv(m, (MprisObject)s!), this);
        public Task<double> GetVolumeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Volume"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<long> GetPositionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Position"), (Message m, object? s) => ReadMessage_v_x(m, (MprisObject)s!), this);
        public Task<double> GetMinimumRateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MinimumRate"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<double> GetMaximumRateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MaximumRate"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<bool> GetCanGoNextAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanGoNext"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanGoPreviousAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanGoPrevious"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanPlayAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanPlay"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanPauseAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanPause"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanSeekAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanSeek"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanControlAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanControl"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<PlayerProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (MprisObject)s!), this);
            static PlayerProperties ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PlayerProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<PlayerProperties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<PlayerProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(arrayEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "PlaybackStatus": invalidated.Add("PlaybackStatus"); break;
                        case "LoopStatus": invalidated.Add("LoopStatus"); break;
                        case "Rate": invalidated.Add("Rate"); break;
                        case "Shuffle": invalidated.Add("Shuffle"); break;
                        case "Metadata": invalidated.Add("Metadata"); break;
                        case "Volume": invalidated.Add("Volume"); break;
                        case "Position": invalidated.Add("Position"); break;
                        case "MinimumRate": invalidated.Add("MinimumRate"); break;
                        case "MaximumRate": invalidated.Add("MaximumRate"); break;
                        case "CanGoNext": invalidated.Add("CanGoNext"); break;
                        case "CanGoPrevious": invalidated.Add("CanGoPrevious"); break;
                        case "CanPlay": invalidated.Add("CanPlay"); break;
                        case "CanPause": invalidated.Add("CanPause"); break;
                        case "CanSeek": invalidated.Add("CanSeek"); break;
                        case "CanControl": invalidated.Add("CanControl"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static PlayerProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new PlayerProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "PlaybackStatus":
                        reader.ReadSignature("s"u8);
                        props.PlaybackStatus = reader.ReadString();
                        changedList?.Add("PlaybackStatus");
                        break;
                    case "LoopStatus":
                        reader.ReadSignature("s"u8);
                        props.LoopStatus = reader.ReadString();
                        changedList?.Add("LoopStatus");
                        break;
                    case "Rate":
                        reader.ReadSignature("d"u8);
                        props.Rate = reader.ReadDouble();
                        changedList?.Add("Rate");
                        break;
                    case "Shuffle":
                        reader.ReadSignature("b"u8);
                        props.Shuffle = reader.ReadBool();
                        changedList?.Add("Shuffle");
                        break;
                    case "Metadata":
                        reader.ReadSignature("a{sv}"u8);
                        props.Metadata = reader.ReadDictionaryOfStringToVariantValue();
                        changedList?.Add("Metadata");
                        break;
                    case "Volume":
                        reader.ReadSignature("d"u8);
                        props.Volume = reader.ReadDouble();
                        changedList?.Add("Volume");
                        break;
                    case "Position":
                        reader.ReadSignature("x"u8);
                        props.Position = reader.ReadInt64();
                        changedList?.Add("Position");
                        break;
                    case "MinimumRate":
                        reader.ReadSignature("d"u8);
                        props.MinimumRate = reader.ReadDouble();
                        changedList?.Add("MinimumRate");
                        break;
                    case "MaximumRate":
                        reader.ReadSignature("d"u8);
                        props.MaximumRate = reader.ReadDouble();
                        changedList?.Add("MaximumRate");
                        break;
                    case "CanGoNext":
                        reader.ReadSignature("b"u8);
                        props.CanGoNext = reader.ReadBool();
                        changedList?.Add("CanGoNext");
                        break;
                    case "CanGoPrevious":
                        reader.ReadSignature("b"u8);
                        props.CanGoPrevious = reader.ReadBool();
                        changedList?.Add("CanGoPrevious");
                        break;
                    case "CanPlay":
                        reader.ReadSignature("b"u8);
                        props.CanPlay = reader.ReadBool();
                        changedList?.Add("CanPlay");
                        break;
                    case "CanPause":
                        reader.ReadSignature("b"u8);
                        props.CanPause = reader.ReadBool();
                        changedList?.Add("CanPause");
                        break;
                    case "CanSeek":
                        reader.ReadSignature("b"u8);
                        props.CanSeek = reader.ReadBool();
                        changedList?.Add("CanSeek");
                        break;
                    case "CanControl":
                        reader.ReadSignature("b"u8);
                        props.CanControl = reader.ReadBool();
                        changedList?.Add("CanControl");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record PlaylistsProperties
    {
        public uint PlaylistCount { get; set; } = default!;
        public string[] Orderings { get; set; } = default!;
        public (bool, (ObjectPath, string, string)) ActivePlaylist { get; set; } = default!;
    }
    partial class Playlists : MprisObject
    {
        private const string __Interface = "org.mpris.MediaPlayer2.Playlists";
        public Playlists(MprisService service, ObjectPath path) : base(service, path)
        { }
        public Task ActivatePlaylistAsync(ObjectPath playlistId)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "o",
                    member: "ActivatePlaylist");
                writer.WriteObjectPath(playlistId);
                return writer.CreateMessage();
            }
        }
        public Task<(ObjectPath, string, string)[]> GetPlaylistsAsync(uint index, uint maxCount, string order, bool reverseOrder)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_arossz(m, (MprisObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "uusb",
                    member: "GetPlaylists");
                writer.WriteUInt32(index);
                writer.WriteUInt32(maxCount);
                writer.WriteString(order);
                writer.WriteBool(reverseOrder);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchPlaylistChangedAsync(Action<Exception?, (ObjectPath, string, string)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "PlaylistChanged", (Message m, object? s) => ReadMessage_rossz(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
        public Task<uint> GetPlaylistCountAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PlaylistCount"), (Message m, object? s) => ReadMessage_v_u(m, (MprisObject)s!), this);
        public Task<string[]> GetOrderingsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Orderings"), (Message m, object? s) => ReadMessage_v_as(m, (MprisObject)s!), this);
        public Task<(bool, (ObjectPath, string, string))> GetActivePlaylistAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ActivePlaylist"), (Message m, object? s) => ReadMessage_v_rbrosszz(m, (MprisObject)s!), this);
        public Task<PlaylistsProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (MprisObject)s!), this);
            static PlaylistsProperties ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PlaylistsProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<PlaylistsProperties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<PlaylistsProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(arrayEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "PlaylistCount": invalidated.Add("PlaylistCount"); break;
                        case "Orderings": invalidated.Add("Orderings"); break;
                        case "ActivePlaylist": invalidated.Add("ActivePlaylist"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static PlaylistsProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new PlaylistsProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "PlaylistCount":
                        reader.ReadSignature("u"u8);
                        props.PlaylistCount = reader.ReadUInt32();
                        changedList?.Add("PlaylistCount");
                        break;
                    case "Orderings":
                        reader.ReadSignature("as"u8);
                        props.Orderings = reader.ReadArrayOfString();
                        changedList?.Add("Orderings");
                        break;
                    case "ActivePlaylist":
                        reader.ReadSignature("(b(oss))"u8);
                        props.ActivePlaylist = ReadType_rbrosszz(ref reader);
                        changedList?.Add("ActivePlaylist");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record TrackListProperties
    {
        public ObjectPath[] Tracks { get; set; } = default!;
        public bool CanEditTracks { get; set; } = default!;
    }
    partial class TrackList : MprisObject
    {
        private const string __Interface = "org.mpris.MediaPlayer2.TrackList";
        public TrackList(MprisService service, ObjectPath path) : base(service, path)
        { }
        public Task<Dictionary<string, VariantValue>[]> GetTracksMetadataAsync(ObjectPath[] trackIds)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aaesv(m, (MprisObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ao",
                    member: "GetTracksMetadata");
                writer.WriteArray(trackIds);
                return writer.CreateMessage();
            }
        }
        public Task AddTrackAsync(string uri, ObjectPath afterTrack, bool setAsCurrent)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "sob",
                    member: "AddTrack");
                writer.WriteString(uri);
                writer.WriteObjectPath(afterTrack);
                writer.WriteBool(setAsCurrent);
                return writer.CreateMessage();
            }
        }
        public Task RemoveTrackAsync(ObjectPath trackId)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "o",
                    member: "RemoveTrack");
                writer.WriteObjectPath(trackId);
                return writer.CreateMessage();
            }
        }
        public Task GoToAsync(ObjectPath trackId)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "o",
                    member: "GoTo");
                writer.WriteObjectPath(trackId);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchTrackListReplacedAsync(Action<Exception?, (ObjectPath[] Tracks, ObjectPath CurrentTrack)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "TrackListReplaced", (Message m, object? s) => ReadMessage_aoo(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchTrackAddedAsync(Action<Exception?, (Dictionary<string, VariantValue> Metadata, ObjectPath AfterTrack)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "TrackAdded", (Message m, object? s) => ReadMessage_aesvo(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchTrackRemovedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "TrackRemoved", (Message m, object? s) => ReadMessage_o(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchTrackMetadataChangedAsync(Action<Exception?, (ObjectPath TrackId, Dictionary<string, VariantValue> Metadata)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "TrackMetadataChanged", (Message m, object? s) => ReadMessage_oaesv(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
        public Task<ObjectPath[]> GetTracksAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Tracks"), (Message m, object? s) => ReadMessage_v_ao(m, (MprisObject)s!), this);
        public Task<bool> GetCanEditTracksAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanEditTracks"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<TrackListProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (MprisObject)s!), this);
            static TrackListProperties ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<TrackListProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<TrackListProperties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<TrackListProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(arrayEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "Tracks": invalidated.Add("Tracks"); break;
                        case "CanEditTracks": invalidated.Add("CanEditTracks"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static TrackListProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new TrackListProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Tracks":
                        reader.ReadSignature("ao"u8);
                        props.Tracks = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Tracks");
                        break;
                    case "CanEditTracks":
                        reader.ReadSignature("b"u8);
                        props.CanEditTracks = reader.ReadBool();
                        changedList?.Add("CanEditTracks");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record MediaPlayer2Properties
    {
        public bool CanQuit { get; set; } = default!;
        public bool Fullscreen { get; set; } = default!;
        public bool CanSetFullscreen { get; set; } = default!;
        public bool CanRaise { get; set; } = default!;
        public bool HasTrackList { get; set; } = default!;
        public string Identity { get; set; } = default!;
        public string DesktopEntry { get; set; } = default!;
        public string[] SupportedUriSchemes { get; set; } = default!;
        public string[] SupportedMimeTypes { get; set; } = default!;
    }
    partial class MediaPlayer2 : MprisObject
    {
        private const string __Interface = "org.mpris.MediaPlayer2";
        public MediaPlayer2(MprisService service, ObjectPath path) : base(service, path)
        { }
        public Task RaiseAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Raise");
                return writer.CreateMessage();
            }
        }
        public Task QuitAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Quit");
                return writer.CreateMessage();
            }
        }
        public Task SetFullscreenAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Fullscreen");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task<bool> GetCanQuitAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanQuit"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetFullscreenAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Fullscreen"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanSetFullscreenAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanSetFullscreen"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanRaiseAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanRaise"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetHasTrackListAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HasTrackList"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<string> GetIdentityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Identity"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<string> GetDesktopEntryAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DesktopEntry"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<string[]> GetSupportedUriSchemesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedUriSchemes"), (Message m, object? s) => ReadMessage_v_as(m, (MprisObject)s!), this);
        public Task<string[]> GetSupportedMimeTypesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedMimeTypes"), (Message m, object? s) => ReadMessage_v_as(m, (MprisObject)s!), this);
        public Task<MediaPlayer2Properties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (MprisObject)s!), this);
            static MediaPlayer2Properties ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<MediaPlayer2Properties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<MediaPlayer2Properties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<MediaPlayer2Properties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(arrayEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "CanQuit": invalidated.Add("CanQuit"); break;
                        case "Fullscreen": invalidated.Add("Fullscreen"); break;
                        case "CanSetFullscreen": invalidated.Add("CanSetFullscreen"); break;
                        case "CanRaise": invalidated.Add("CanRaise"); break;
                        case "HasTrackList": invalidated.Add("HasTrackList"); break;
                        case "Identity": invalidated.Add("Identity"); break;
                        case "DesktopEntry": invalidated.Add("DesktopEntry"); break;
                        case "SupportedUriSchemes": invalidated.Add("SupportedUriSchemes"); break;
                        case "SupportedMimeTypes": invalidated.Add("SupportedMimeTypes"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static MediaPlayer2Properties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new MediaPlayer2Properties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "CanQuit":
                        reader.ReadSignature("b"u8);
                        props.CanQuit = reader.ReadBool();
                        changedList?.Add("CanQuit");
                        break;
                    case "Fullscreen":
                        reader.ReadSignature("b"u8);
                        props.Fullscreen = reader.ReadBool();
                        changedList?.Add("Fullscreen");
                        break;
                    case "CanSetFullscreen":
                        reader.ReadSignature("b"u8);
                        props.CanSetFullscreen = reader.ReadBool();
                        changedList?.Add("CanSetFullscreen");
                        break;
                    case "CanRaise":
                        reader.ReadSignature("b"u8);
                        props.CanRaise = reader.ReadBool();
                        changedList?.Add("CanRaise");
                        break;
                    case "HasTrackList":
                        reader.ReadSignature("b"u8);
                        props.HasTrackList = reader.ReadBool();
                        changedList?.Add("HasTrackList");
                        break;
                    case "Identity":
                        reader.ReadSignature("s"u8);
                        props.Identity = reader.ReadString();
                        changedList?.Add("Identity");
                        break;
                    case "DesktopEntry":
                        reader.ReadSignature("s"u8);
                        props.DesktopEntry = reader.ReadString();
                        changedList?.Add("DesktopEntry");
                        break;
                    case "SupportedUriSchemes":
                        reader.ReadSignature("as"u8);
                        props.SupportedUriSchemes = reader.ReadArrayOfString();
                        changedList?.Add("SupportedUriSchemes");
                        break;
                    case "SupportedMimeTypes":
                        reader.ReadSignature("as"u8);
                        props.SupportedMimeTypes = reader.ReadArrayOfString();
                        changedList?.Add("SupportedMimeTypes");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    partial class MprisService
    {
        public Tmds.DBus.Protocol.Connection Connection { get; }
        public string Destination { get; }
        public MprisService(Tmds.DBus.Protocol.Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);
        public Player CreatePlayer(ObjectPath path) => new Player(this, path);
        public Playlists CreatePlaylists(ObjectPath path) => new Playlists(this, path);
        public TrackList CreateTrackList(ObjectPath path) => new TrackList(this, path);
        public MediaPlayer2 CreateMediaPlayer2(ObjectPath path) => new MediaPlayer2(this, path);
    }
    class MprisObject
    {
        public MprisService Service { get; }
        public ObjectPath Path { get; }
        protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;
        protected MprisObject(MprisService service, ObjectPath path)
            => (Service, Path) = (service, path);
        protected MessageBuffer CreateGetPropertyMessage(string @interface, string property)
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "ss",
                member: "Get");
            writer.WriteString(@interface);
            writer.WriteString(property);
            return writer.CreateMessage();
        }
        protected MessageBuffer CreateGetAllPropertiesMessage(string @interface)
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "s",
                member: "GetAll");
            writer.WriteString(@interface);
            return writer.CreateMessage();
        }
        protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext, ObserverFlags flags)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = Service.Destination,
                Path = Path,
                Interface = "org.freedesktop.DBus.Properties",
                Member = "PropertiesChanged",
                Arg0 = @interface
            };
            return this.Connection.AddMatchAsync(rule, reader,
                                                    (Exception? ex, PropertyChanges<TProperties> changes, object? rs, object? hs) => ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),
                                                    this, handler, emitOnCapturedContext, flags);
        }
        public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext, ObserverFlags flags)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return this.Connection.AddMatchAsync(rule, reader,
                                                    (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
                                                    this, handler, emitOnCapturedContext, flags);
        }
        public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext, ObserverFlags flags)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal,
                Interface = @interface
            };
            return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,
                                                            (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext, flags);
        }
        protected static long ReadMessage_x(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadInt64();
        }
        protected static string ReadMessage_v_s(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("s"u8);
            return reader.ReadString();
        }
        protected static double ReadMessage_v_d(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("d"u8);
            return reader.ReadDouble();
        }
        protected static bool ReadMessage_v_b(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("b"u8);
            return reader.ReadBool();
        }
        protected static Dictionary<string, VariantValue> ReadMessage_v_aesv(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("a{sv}"u8);
            return reader.ReadDictionaryOfStringToVariantValue();
        }
        protected static long ReadMessage_v_x(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("x"u8);
            return reader.ReadInt64();
        }
        protected static (ObjectPath, string, string)[] ReadMessage_arossz(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_arossz(ref reader);
        }
        protected static (ObjectPath, string, string) ReadMessage_rossz(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_rossz(ref reader);
        }
        protected static uint ReadMessage_v_u(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("u"u8);
            return reader.ReadUInt32();
        }
        protected static string[] ReadMessage_v_as(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("as"u8);
            return reader.ReadArrayOfString();
        }
        protected static (bool, (ObjectPath, string, string)) ReadMessage_v_rbrosszz(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("(b(oss))"u8);
            return ReadType_rbrosszz(ref reader);
        }
        protected static Dictionary<string, VariantValue>[] ReadMessage_aaesv(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_aaesv(ref reader);
        }
        protected static (ObjectPath[], ObjectPath) ReadMessage_aoo(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadArrayOfObjectPath();
            var arg1 = reader.ReadObjectPath();
            return (arg0, arg1);
        }
        protected static (Dictionary<string, VariantValue>, ObjectPath) ReadMessage_aesvo(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadDictionaryOfStringToVariantValue();
            var arg1 = reader.ReadObjectPath();
            return (arg0, arg1);
        }
        protected static ObjectPath ReadMessage_o(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadObjectPath();
        }
        protected static (ObjectPath, Dictionary<string, VariantValue>) ReadMessage_oaesv(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadDictionaryOfStringToVariantValue();
            return (arg0, arg1);
        }
        protected static ObjectPath[] ReadMessage_v_ao(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("ao"u8);
            return reader.ReadArrayOfObjectPath();
        }
        protected static (bool, (ObjectPath, string, string)) ReadType_rbrosszz(ref Reader reader)
        {
            return (reader.ReadBool(), ReadType_rossz(ref reader));
        }
        protected static (ObjectPath, string, string) ReadType_rossz(ref Reader reader)
        {
            return (reader.ReadObjectPath(), reader.ReadString(), reader.ReadString());
        }
        protected static (ObjectPath, string, string)[] ReadType_arossz(ref Reader reader)
        {
            List<(ObjectPath, string, string)> list = new();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                list.Add(ReadType_rossz(ref reader));
            }
            return list.ToArray();
        }
        protected static Dictionary<string, VariantValue>[] ReadType_aaesv(ref Reader reader)
        {
            List<Dictionary<string, VariantValue>> list = new();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Array);
            while (reader.HasNext(arrayEnd))
            {
                list.Add(reader.ReadDictionaryOfStringToVariantValue());
            }
            return list.ToArray();
        }
    }
    class PropertyChanges<TProperties>
    {
        public PropertyChanges(TProperties properties, string[] invalidated, string[] changed)
        	=> (Properties, Invalidated, Changed) = (properties, invalidated, changed);
        public TProperties Properties { get; }
        public string[] Invalidated { get; }
        public string[] Changed { get; }
        public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;
        public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
    }
}

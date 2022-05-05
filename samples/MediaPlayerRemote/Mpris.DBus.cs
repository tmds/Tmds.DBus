namespace Mpris.DBus
{
    using System;
    using Tmds.DBus.Protocol;
    using SafeHandle = System.Runtime.InteropServices.SafeHandle;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    record MediaPlayer2Properties
    {
        public string Identity { get; set; } = default!;
        public string DesktopEntry { get; set; } = default!;
        public string[] SupportedMimeTypes { get; set; } = default!;
        public string[] SupportedUriSchemes { get; set; } = default!;
        public bool HasTrackList { get; set; } = default!;
        public bool CanQuit { get; set; } = default!;
        public bool CanSetFullscreen { get; set; } = default!;
        public bool Fullscreen { get; set; } = default!;
        public bool CanRaise { get; set; } = default!;
    }
    partial class MediaPlayer2 : MprisObject
    {
        private const string __Interface = "org.mpris.MediaPlayer2";
        public MediaPlayer2(MprisService service, ObjectPath path) : base(service, path)
        { }
        public Task QuitAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Quit");
                return writer.CreateMessage();
            }
        }
        public Task RaiseAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Raise");
                return writer.CreateMessage();
            }
        }
        public Task SetIdentityAsync(string value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Identity");
                writer.WriteSignature("s");
                writer.WriteString(value);
                return writer.CreateMessage();
            }
        }
        public Task SetDesktopEntryAsync(string value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("DesktopEntry");
                writer.WriteSignature("s");
                writer.WriteString(value);
                return writer.CreateMessage();
            }
        }
        public Task SetSupportedMimeTypesAsync(string[] value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("SupportedMimeTypes");
                writer.WriteSignature("as");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }
        public Task SetSupportedUriSchemesAsync(string[] value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("SupportedUriSchemes");
                writer.WriteSignature("as");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }
        public Task SetHasTrackListAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("HasTrackList");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanQuitAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanQuit");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanSetFullscreenAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanSetFullscreen");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetFullscreenAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
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
        public Task SetCanRaiseAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanRaise");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task<string> GetIdentityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Identity"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<string> GetDesktopEntryAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DesktopEntry"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<string[]> GetSupportedMimeTypesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedMimeTypes"), (Message m, object? s) => ReadMessage_v_as(m, (MprisObject)s!), this);
        public Task<string[]> GetSupportedUriSchemesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedUriSchemes"), (Message m, object? s) => ReadMessage_v_as(m, (MprisObject)s!), this);
        public Task<bool> GetHasTrackListAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HasTrackList"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanQuitAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanQuit"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanSetFullscreenAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanSetFullscreen"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetFullscreenAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Fullscreen"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanRaiseAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanRaise"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<MediaPlayer2Properties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (MprisObject)s!), this);
            static MediaPlayer2Properties ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<MediaPlayer2Properties>> handler, bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext);
            static PropertyChanges<MediaPlayer2Properties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new(), invalidated = new();
                return new PropertyChanges<MediaPlayer2Properties>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(headersEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "Identity": invalidated.Add("Identity"); break;
                        case "DesktopEntry": invalidated.Add("DesktopEntry"); break;
                        case "SupportedMimeTypes": invalidated.Add("SupportedMimeTypes"); break;
                        case "SupportedUriSchemes": invalidated.Add("SupportedUriSchemes"); break;
                        case "HasTrackList": invalidated.Add("HasTrackList"); break;
                        case "CanQuit": invalidated.Add("CanQuit"); break;
                        case "CanSetFullscreen": invalidated.Add("CanSetFullscreen"); break;
                        case "Fullscreen": invalidated.Add("Fullscreen"); break;
                        case "CanRaise": invalidated.Add("CanRaise"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static MediaPlayer2Properties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new MediaPlayer2Properties();
            ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Identity":
                        reader.ReadSignature("s");
                        props.Identity = reader.ReadString();
                        changedList?.Add("Identity");
                        break;
                    case "DesktopEntry":
                        reader.ReadSignature("s");
                        props.DesktopEntry = reader.ReadString();
                        changedList?.Add("DesktopEntry");
                        break;
                    case "SupportedMimeTypes":
                        reader.ReadSignature("as");
                        props.SupportedMimeTypes = reader.ReadArray<string>();
                        changedList?.Add("SupportedMimeTypes");
                        break;
                    case "SupportedUriSchemes":
                        reader.ReadSignature("as");
                        props.SupportedUriSchemes = reader.ReadArray<string>();
                        changedList?.Add("SupportedUriSchemes");
                        break;
                    case "HasTrackList":
                        reader.ReadSignature("b");
                        props.HasTrackList = reader.ReadBool();
                        changedList?.Add("HasTrackList");
                        break;
                    case "CanQuit":
                        reader.ReadSignature("b");
                        props.CanQuit = reader.ReadBool();
                        changedList?.Add("CanQuit");
                        break;
                    case "CanSetFullscreen":
                        reader.ReadSignature("b");
                        props.CanSetFullscreen = reader.ReadBool();
                        changedList?.Add("CanSetFullscreen");
                        break;
                    case "Fullscreen":
                        reader.ReadSignature("b");
                        props.Fullscreen = reader.ReadBool();
                        changedList?.Add("Fullscreen");
                        break;
                    case "CanRaise":
                        reader.ReadSignature("b");
                        props.CanRaise = reader.ReadBool();
                        changedList?.Add("CanRaise");
                        break;
                    default:
                        reader.ReadVariant();
                        break;
                }
            }
            return props;
        }
    }
    record PlayerProperties
    {
        public Dictionary<string, object> Metadata { get; set; } = default!;
        public string PlaybackStatus { get; set; } = default!;
        public string LoopStatus { get; set; } = default!;
        public double Volume { get; set; } = default!;
        public double Shuffle { get; set; } = default!;
        public int Position { get; set; } = default!;
        public double Rate { get; set; } = default!;
        public double MinimumRate { get; set; } = default!;
        public double MaximumRate { get; set; } = default!;
        public bool CanControl { get; set; } = default!;
        public bool CanPlay { get; set; } = default!;
        public bool CanPause { get; set; } = default!;
        public bool CanSeek { get; set; } = default!;
    }
    partial class Player : MprisObject
    {
        private const string __Interface = "org.mpris.MediaPlayer2.Player";
        public Player(MprisService service, ObjectPath path) : base(service, path)
        { }
        public Task PreviousAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Previous");
                return writer.CreateMessage();
            }
        }
        public Task NextAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Next");
                return writer.CreateMessage();
            }
        }
        public Task StopAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
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
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Play");
                return writer.CreateMessage();
            }
        }
        public Task PauseAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
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
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "PlayPause");
                return writer.CreateMessage();
            }
        }
        public Task SeekAsync(long a0)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "x",
                    member: "Seek");
                writer.WriteInt64(a0);
                return writer.CreateMessage();
            }
        }
        public Task OpenUriAsync(string a0)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "s",
                    member: "OpenUri");
                writer.WriteString(a0);
                return writer.CreateMessage();
            }
        }
        public Task SetPositionAsync(ObjectPath a0, long a1)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ox",
                    member: "SetPosition");
                writer.WriteObjectPath(a0);
                writer.WriteInt64(a1);
                return writer.CreateMessage();
            }
        }
        public Task SetMetadataAsync(Dictionary<string, object> value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Metadata");
                writer.WriteSignature("a{sv}");
                writer.WriteDictionary(value);
                return writer.CreateMessage();
            }
        }
        public Task SetPlaybackStatusAsync(string value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("PlaybackStatus");
                writer.WriteSignature("s");
                writer.WriteString(value);
                return writer.CreateMessage();
            }
        }
        public Task SetLoopStatusAsync(string value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
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
        public Task SetVolumeAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
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
        public Task SetShuffleAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Shuffle");
                writer.WriteSignature("d");
                writer.WriteDouble(value);
                return writer.CreateMessage();
            }
        }
        public Task SetPositionAsync(int value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Position");
                writer.WriteSignature("i");
                writer.WriteInt32(value);
                return writer.CreateMessage();
            }
        }
        public Task SetRateAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
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
        public Task SetMinimumRateAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("MinimumRate");
                writer.WriteSignature("d");
                writer.WriteDouble(value);
                return writer.CreateMessage();
            }
        }
        public Task SetMaximumRateAsync(double value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("MaximumRate");
                writer.WriteSignature("d");
                writer.WriteDouble(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanControlAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanControl");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanPlayAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanPlay");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanPauseAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanPause");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanSeekAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanSeek");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task<Dictionary<string, object>> GetMetadataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Metadata"), (Message m, object? s) => ReadMessage_v_aesv(m, (MprisObject)s!), this);
        public Task<string> GetPlaybackStatusAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PlaybackStatus"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<string> GetLoopStatusAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "LoopStatus"), (Message m, object? s) => ReadMessage_v_s(m, (MprisObject)s!), this);
        public Task<double> GetVolumeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Volume"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<double> GetShuffleAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Shuffle"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<int> GetPositionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Position"), (Message m, object? s) => ReadMessage_v_i(m, (MprisObject)s!), this);
        public Task<double> GetRateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Rate"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<double> GetMinimumRateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MinimumRate"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<double> GetMaximumRateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MaximumRate"), (Message m, object? s) => ReadMessage_v_d(m, (MprisObject)s!), this);
        public Task<bool> GetCanControlAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanControl"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanPlayAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanPlay"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanPauseAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanPause"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<bool> GetCanSeekAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanSeek"), (Message m, object? s) => ReadMessage_v_b(m, (MprisObject)s!), this);
        public Task<PlayerProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (MprisObject)s!), this);
            static PlayerProperties ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PlayerProperties>> handler, bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext);
            static PropertyChanges<PlayerProperties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new(), invalidated = new();
                return new PropertyChanges<PlayerProperties>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(headersEnd))
                {
                    invalidated ??= new();
                    var property = reader.ReadString();
                    switch (property)
                    {
                        case "Metadata": invalidated.Add("Metadata"); break;
                        case "PlaybackStatus": invalidated.Add("PlaybackStatus"); break;
                        case "LoopStatus": invalidated.Add("LoopStatus"); break;
                        case "Volume": invalidated.Add("Volume"); break;
                        case "Shuffle": invalidated.Add("Shuffle"); break;
                        case "Position": invalidated.Add("Position"); break;
                        case "Rate": invalidated.Add("Rate"); break;
                        case "MinimumRate": invalidated.Add("MinimumRate"); break;
                        case "MaximumRate": invalidated.Add("MaximumRate"); break;
                        case "CanControl": invalidated.Add("CanControl"); break;
                        case "CanPlay": invalidated.Add("CanPlay"); break;
                        case "CanPause": invalidated.Add("CanPause"); break;
                        case "CanSeek": invalidated.Add("CanSeek"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static PlayerProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new PlayerProperties();
            ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Metadata":
                        reader.ReadSignature("a{sv}");
                        props.Metadata = reader.ReadDictionary<string, object>();
                        changedList?.Add("Metadata");
                        break;
                    case "PlaybackStatus":
                        reader.ReadSignature("s");
                        props.PlaybackStatus = reader.ReadString();
                        changedList?.Add("PlaybackStatus");
                        break;
                    case "LoopStatus":
                        reader.ReadSignature("s");
                        props.LoopStatus = reader.ReadString();
                        changedList?.Add("LoopStatus");
                        break;
                    case "Volume":
                        reader.ReadSignature("d");
                        props.Volume = reader.ReadDouble();
                        changedList?.Add("Volume");
                        break;
                    case "Shuffle":
                        reader.ReadSignature("d");
                        props.Shuffle = reader.ReadDouble();
                        changedList?.Add("Shuffle");
                        break;
                    case "Position":
                        reader.ReadSignature("i");
                        props.Position = reader.ReadInt32();
                        changedList?.Add("Position");
                        break;
                    case "Rate":
                        reader.ReadSignature("d");
                        props.Rate = reader.ReadDouble();
                        changedList?.Add("Rate");
                        break;
                    case "MinimumRate":
                        reader.ReadSignature("d");
                        props.MinimumRate = reader.ReadDouble();
                        changedList?.Add("MinimumRate");
                        break;
                    case "MaximumRate":
                        reader.ReadSignature("d");
                        props.MaximumRate = reader.ReadDouble();
                        changedList?.Add("MaximumRate");
                        break;
                    case "CanControl":
                        reader.ReadSignature("b");
                        props.CanControl = reader.ReadBool();
                        changedList?.Add("CanControl");
                        break;
                    case "CanPlay":
                        reader.ReadSignature("b");
                        props.CanPlay = reader.ReadBool();
                        changedList?.Add("CanPlay");
                        break;
                    case "CanPause":
                        reader.ReadSignature("b");
                        props.CanPause = reader.ReadBool();
                        changedList?.Add("CanPause");
                        break;
                    case "CanSeek":
                        reader.ReadSignature("b");
                        props.CanSeek = reader.ReadBool();
                        changedList?.Add("CanSeek");
                        break;
                    default:
                        reader.ReadVariant();
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
        public Task<Dictionary<string, object>[]> GetTracksMetadataAsync(ObjectPath[] a0)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aaesv(m, (MprisObject)s!), this);
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ao",
                    member: "GetTracksMetadata");
                writer.WriteArray(a0);
                return writer.CreateMessage();
            }
        }
        public Task AddTrackAsync(string a0, ObjectPath a1, bool a2)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "sob",
                    member: "AddTrack");
                writer.WriteString(a0);
                writer.WriteObjectPath(a1);
                writer.WriteBool(a2);
                return writer.CreateMessage();
            }
        }
        public Task RemoveTrackAsync(ObjectPath a0)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "o",
                    member: "RemoveTrack");
                writer.WriteObjectPath(a0);
                return writer.CreateMessage();
            }
        }
        public Task GoToAsync(ObjectPath a0)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "o",
                    member: "GoTo");
                writer.WriteObjectPath(a0);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchTrackListReplacedAsync(Action<Exception?, (ObjectPath[] A0, ObjectPath A1)> handler, bool emitOnCapturedContext = true)
            => base.WatchSignalAsync(Service.Destination, Path, "TrackListReplaced", (Message m, object? s) => ReadMessage_aoo(m, (MprisObject)s!), handler, emitOnCapturedContext);
        public ValueTask<IDisposable> WatchTrackAddedAsync(Action<Exception?, (Dictionary<string, object> A0, ObjectPath A1)> handler, bool emitOnCapturedContext = true)
            => base.WatchSignalAsync(Service.Destination, Path, "TrackAdded", (Message m, object? s) => ReadMessage_aesvo(m, (MprisObject)s!), handler, emitOnCapturedContext);
        public ValueTask<IDisposable> WatchTrackRemovedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true)
            => base.WatchSignalAsync(Service.Destination, Path, "TrackRemoved", (Message m, object? s) => ReadMessage_o(m, (MprisObject)s!), handler, emitOnCapturedContext);
        public ValueTask<IDisposable> WatchTrackMetadataChangedAsync(Action<Exception?, (ObjectPath A0, Dictionary<string, object> A1)> handler, bool emitOnCapturedContext = true)
            => base.WatchSignalAsync(Service.Destination, Path, "TrackMetadataChanged", (Message m, object? s) => ReadMessage_oaesv(m, (MprisObject)s!), handler, emitOnCapturedContext);
        public Task SetTracksAsync(ObjectPath[] value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("Tracks");
                writer.WriteSignature("ao");
                writer.WriteArray(value);
                return writer.CreateMessage();
            }
        }
        public Task SetCanEditTracksAsync(bool value)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                using var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: "org.freedesktop.DBus.Properties",
                    signature: "ssv",
                    member: "Set");
                writer.WriteString(__Interface);
                writer.WriteString("CanEditTracks");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
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
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<TrackListProperties>> handler, bool emitOnCapturedContext = true)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (MprisObject)s!), handler, emitOnCapturedContext);
            static PropertyChanges<TrackListProperties> ReadMessage(Message message, MprisObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new(), invalidated = new();
                return new PropertyChanges<TrackListProperties>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));
            }
            static string[] ReadInvalidated(ref Reader reader)
            {
                List<string>? invalidated = null;
                ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.String);
                while (reader.HasNext(headersEnd))
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
            ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(headersEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Tracks":
                        reader.ReadSignature("ao");
                        props.Tracks = reader.ReadArray<ObjectPath>();
                        changedList?.Add("Tracks");
                        break;
                    case "CanEditTracks":
                        reader.ReadSignature("b");
                        props.CanEditTracks = reader.ReadBool();
                        changedList?.Add("CanEditTracks");
                        break;
                    default:
                        reader.ReadVariant();
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
        public MediaPlayer2 CreateMediaPlayer2(string path) => new MediaPlayer2(this, path);
        public Player CreatePlayer(string path) => new Player(this, path);
        public TrackList CreateTrackList(string path) => new TrackList(this, path);
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
            using var writer = this.Connection.GetMessageWriter();
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
            using var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: "org.freedesktop.DBus.Properties",
                signature: "s",
                member: "GetAll");
            writer.WriteString(@interface);
            return writer.CreateMessage();
        }
        protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext)
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
                                                    this, handler, emitOnCapturedContext);
        }
        public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal
            };
            return this.Connection.AddMatchAsync(rule, reader,
                                                    (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
                                                    this, handler, emitOnCapturedContext);
        }
        public ValueTask<IDisposable> WatchSignalAsync(string sender, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext)
        {
            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = sender,
                Path = path,
                Member = signal
            };
            return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,
                                                            (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
        }
        protected static string ReadMessage_v_s(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("s");
            return reader.ReadString();
        }
        protected static string[] ReadMessage_v_as(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("as");
            return reader.ReadArray<string>();
        }
        protected static bool ReadMessage_v_b(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("b");
            return reader.ReadBool();
        }
        protected static Dictionary<string, object> ReadMessage_v_aesv(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("a{sv}");
            return reader.ReadDictionary<string, object>();
        }
        protected static double ReadMessage_v_d(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("d");
            return reader.ReadDouble();
        }
        protected static int ReadMessage_v_i(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("i");
            return reader.ReadInt32();
        }
        protected static Dictionary<string, object>[] ReadMessage_aaesv(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadArray<Dictionary<string, object>>();
        }
        protected static (ObjectPath[], ObjectPath) ReadMessage_aoo(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadArray<ObjectPath>();
            var arg1 = reader.ReadObjectPath();
            return (arg0, arg1);
        }
        protected static (Dictionary<string, object>, ObjectPath) ReadMessage_aesvo(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadDictionary<string, object>();
            var arg1 = reader.ReadObjectPath();
            return (arg0, arg1);
        }
        protected static ObjectPath ReadMessage_o(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadObjectPath();
        }
        protected static (ObjectPath, Dictionary<string, object>) ReadMessage_oaesv(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadDictionary<string, object>();
            return (arg0, arg1);
        }
        protected static ObjectPath[] ReadMessage_v_ao(Message message, MprisObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("ao");
            return reader.ReadArray<ObjectPath>();
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

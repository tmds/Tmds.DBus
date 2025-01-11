namespace NetworkManager.DBus
{
    using System;
    using Tmds.DBus.Protocol;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    partial class ObjectManager : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.DBus.ObjectManager";
        public ObjectManager(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>>> GetManagedObjectsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aeoaesaesv(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetManagedObjects");
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchInterfacesAddedAsync(Action<Exception?, (ObjectPath ObjectPath, Dictionary<string, Dictionary<string, VariantValue>> InterfacesAndProperties)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "InterfacesAdded", (Message m, object? s) => ReadMessage_oaesaesv(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchInterfacesRemovedAsync(Action<Exception?, (ObjectPath ObjectPath, string[] Interfaces)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "InterfacesRemoved", (Message m, object? s) => ReadMessage_oas(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
    }
    record NetworkManagerProperties
    {
        public ObjectPath[] Devices { get; set; } = default!;
        public ObjectPath[] AllDevices { get; set; } = default!;
        public ObjectPath[] Checkpoints { get; set; } = default!;
        public bool NetworkingEnabled { get; set; } = default!;
        public bool WirelessEnabled { get; set; } = default!;
        public bool WirelessHardwareEnabled { get; set; } = default!;
        public bool WwanEnabled { get; set; } = default!;
        public bool WwanHardwareEnabled { get; set; } = default!;
        public bool WimaxEnabled { get; set; } = default!;
        public bool WimaxHardwareEnabled { get; set; } = default!;
        public uint RadioFlags { get; set; } = default!;
        public ObjectPath[] ActiveConnections { get; set; } = default!;
        public ObjectPath PrimaryConnection { get; set; } = default!;
        public string PrimaryConnectionType { get; set; } = default!;
        public uint Metered { get; set; } = default!;
        public ObjectPath ActivatingConnection { get; set; } = default!;
        public bool Startup { get; set; } = default!;
        public string Version { get; set; } = default!;
        public uint[] VersionInfo { get; set; } = default!;
        public uint[] Capabilities { get; set; } = default!;
        public uint State { get; set; } = default!;
        public uint Connectivity { get; set; } = default!;
        public bool ConnectivityCheckAvailable { get; set; } = default!;
        public bool ConnectivityCheckEnabled { get; set; } = default!;
        public string ConnectivityCheckUri { get; set; } = default!;
        public Dictionary<string, VariantValue> GlobalDnsConfiguration { get; set; } = default!;
    }
    partial class NetworkManager : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager";
        public NetworkManager(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task ReloadAsync(uint flags)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "u",
                    member: "Reload");
                writer.WriteUInt32(flags);
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath[]> GetDevicesAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ao(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetDevices");
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath[]> GetAllDevicesAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ao(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetAllDevices");
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath> GetDeviceByIpIfaceAsync(string iface)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "s",
                    member: "GetDeviceByIpIface");
                writer.WriteString(iface);
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath> ActivateConnectionAsync(ObjectPath connection, ObjectPath device, ObjectPath specificObject)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ooo",
                    member: "ActivateConnection");
                writer.WriteObjectPath(connection);
                writer.WriteObjectPath(device);
                writer.WriteObjectPath(specificObject);
                return writer.CreateMessage();
            }
        }
        public Task<(ObjectPath Path, ObjectPath ActiveConnection)> AddAndActivateConnectionAsync(Dictionary<string, Dictionary<string, VariantValue>> connection, ObjectPath device, ObjectPath specificObject)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_oo(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}oo",
                    member: "AddAndActivateConnection");
                WriteType_aesaesv(ref writer, connection);
                writer.WriteObjectPath(device);
                writer.WriteObjectPath(specificObject);
                return writer.CreateMessage();
            }
        }
        public Task<(ObjectPath Path, ObjectPath ActiveConnection, Dictionary<string, VariantValue> Result)> AddAndActivateConnection2Async(Dictionary<string, Dictionary<string, VariantValue>> connection, ObjectPath device, ObjectPath specificObject, Dictionary<string, VariantValue> options)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ooaesv(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}ooa{sv}",
                    member: "AddAndActivateConnection2");
                WriteType_aesaesv(ref writer, connection);
                writer.WriteObjectPath(device);
                writer.WriteObjectPath(specificObject);
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }
        public Task DeactivateConnectionAsync(ObjectPath activeConnection)
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
                    member: "DeactivateConnection");
                writer.WriteObjectPath(activeConnection);
                return writer.CreateMessage();
            }
        }
        public Task SleepAsync(bool sleep)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "b",
                    member: "Sleep");
                writer.WriteBool(sleep);
                return writer.CreateMessage();
            }
        }
        public Task EnableAsync(bool enable)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "b",
                    member: "Enable");
                writer.WriteBool(enable);
                return writer.CreateMessage();
            }
        }
        public Task<Dictionary<string, string>> GetPermissionsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aess(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetPermissions");
                return writer.CreateMessage();
            }
        }
        public Task SetLoggingAsync(string level, string domains)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ss",
                    member: "SetLogging");
                writer.WriteString(level);
                writer.WriteString(domains);
                return writer.CreateMessage();
            }
        }
        public Task<(string Level, string Domains)> GetLoggingAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ss(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetLogging");
                return writer.CreateMessage();
            }
        }
        public Task<uint> CheckConnectivityAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_u(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "CheckConnectivity");
                return writer.CreateMessage();
            }
        }
        public Task<uint> StateAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_u(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "state");
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath> CheckpointCreateAsync(ObjectPath[] devices, uint rollbackTimeout, uint flags)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "aouu",
                    member: "CheckpointCreate");
                writer.WriteArray(devices);
                writer.WriteUInt32(rollbackTimeout);
                writer.WriteUInt32(flags);
                return writer.CreateMessage();
            }
        }
        public Task CheckpointDestroyAsync(ObjectPath checkpoint)
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
                    member: "CheckpointDestroy");
                writer.WriteObjectPath(checkpoint);
                return writer.CreateMessage();
            }
        }
        public Task<Dictionary<string, uint>> CheckpointRollbackAsync(ObjectPath checkpoint)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesu(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "o",
                    member: "CheckpointRollback");
                writer.WriteObjectPath(checkpoint);
                return writer.CreateMessage();
            }
        }
        public Task CheckpointAdjustRollbackTimeoutAsync(ObjectPath checkpoint, uint addTimeout)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "ou",
                    member: "CheckpointAdjustRollbackTimeout");
                writer.WriteObjectPath(checkpoint);
                writer.WriteUInt32(addTimeout);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchCheckPermissionsAsync(Action<Exception?> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "CheckPermissions", handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, uint> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ReadMessage_u(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchDeviceAddedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "DeviceAdded", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchDeviceRemovedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "DeviceRemoved", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public Task SetWirelessEnabledAsync(bool value)
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
                writer.WriteString("WirelessEnabled");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetWwanEnabledAsync(bool value)
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
                writer.WriteString("WwanEnabled");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetWimaxEnabledAsync(bool value)
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
                writer.WriteString("WimaxEnabled");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetConnectivityCheckEnabledAsync(bool value)
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
                writer.WriteString("ConnectivityCheckEnabled");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetGlobalDnsConfigurationAsync(Dictionary<string, VariantValue> value)
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
                writer.WriteString("GlobalDnsConfiguration");
                writer.WriteSignature("a{sv}");
                writer.WriteDictionary(value);
                return writer.CreateMessage();
            }
        }
        //public Task<ObjectPath[]> GetDevicesAsync()
        //    => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Devices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        //public Task<ObjectPath[]> GetAllDevicesAsync()
        //    => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AllDevices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath[]> GetCheckpointsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Checkpoints"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetNetworkingEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "NetworkingEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetWirelessEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WirelessEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetWirelessHardwareEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WirelessHardwareEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetWwanEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WwanEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetWwanHardwareEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WwanHardwareEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetWimaxEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WimaxEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetWimaxHardwareEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WimaxHardwareEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetRadioFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RadioFlags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath[]> GetActiveConnectionsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ActiveConnections"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetPrimaryConnectionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PrimaryConnection"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<string> GetPrimaryConnectionTypeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PrimaryConnectionType"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetMeteredAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Metered"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetActivatingConnectionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ActivatingConnection"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetStartupAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Startup"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<string> GetVersionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Version"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint[]> GetVersionInfoAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "VersionInfo"), (Message m, object? s) => ReadMessage_v_au(m, (NetworkManagerObject)s!), this);
        public Task<uint[]> GetCapabilitiesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Capabilities"), (Message m, object? s) => ReadMessage_v_au(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetStateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "State"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetConnectivityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Connectivity"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetConnectivityCheckAvailableAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ConnectivityCheckAvailable"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetConnectivityCheckEnabledAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ConnectivityCheckEnabled"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<string> GetConnectivityCheckUriAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ConnectivityCheckUri"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>> GetGlobalDnsConfigurationAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "GlobalDnsConfiguration"), (Message m, object? s) => ReadMessage_v_aesv(m, (NetworkManagerObject)s!), this);
        public Task<NetworkManagerProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static NetworkManagerProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<NetworkManagerProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<NetworkManagerProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<NetworkManagerProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Devices": invalidated.Add("Devices"); break;
                        case "AllDevices": invalidated.Add("AllDevices"); break;
                        case "Checkpoints": invalidated.Add("Checkpoints"); break;
                        case "NetworkingEnabled": invalidated.Add("NetworkingEnabled"); break;
                        case "WirelessEnabled": invalidated.Add("WirelessEnabled"); break;
                        case "WirelessHardwareEnabled": invalidated.Add("WirelessHardwareEnabled"); break;
                        case "WwanEnabled": invalidated.Add("WwanEnabled"); break;
                        case "WwanHardwareEnabled": invalidated.Add("WwanHardwareEnabled"); break;
                        case "WimaxEnabled": invalidated.Add("WimaxEnabled"); break;
                        case "WimaxHardwareEnabled": invalidated.Add("WimaxHardwareEnabled"); break;
                        case "RadioFlags": invalidated.Add("RadioFlags"); break;
                        case "ActiveConnections": invalidated.Add("ActiveConnections"); break;
                        case "PrimaryConnection": invalidated.Add("PrimaryConnection"); break;
                        case "PrimaryConnectionType": invalidated.Add("PrimaryConnectionType"); break;
                        case "Metered": invalidated.Add("Metered"); break;
                        case "ActivatingConnection": invalidated.Add("ActivatingConnection"); break;
                        case "Startup": invalidated.Add("Startup"); break;
                        case "Version": invalidated.Add("Version"); break;
                        case "VersionInfo": invalidated.Add("VersionInfo"); break;
                        case "Capabilities": invalidated.Add("Capabilities"); break;
                        case "State": invalidated.Add("State"); break;
                        case "Connectivity": invalidated.Add("Connectivity"); break;
                        case "ConnectivityCheckAvailable": invalidated.Add("ConnectivityCheckAvailable"); break;
                        case "ConnectivityCheckEnabled": invalidated.Add("ConnectivityCheckEnabled"); break;
                        case "ConnectivityCheckUri": invalidated.Add("ConnectivityCheckUri"); break;
                        case "GlobalDnsConfiguration": invalidated.Add("GlobalDnsConfiguration"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static NetworkManagerProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new NetworkManagerProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Devices":
                        reader.ReadSignature("ao"u8);
                        props.Devices = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Devices");
                        break;
                    case "AllDevices":
                        reader.ReadSignature("ao"u8);
                        props.AllDevices = reader.ReadArrayOfObjectPath();
                        changedList?.Add("AllDevices");
                        break;
                    case "Checkpoints":
                        reader.ReadSignature("ao"u8);
                        props.Checkpoints = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Checkpoints");
                        break;
                    case "NetworkingEnabled":
                        reader.ReadSignature("b"u8);
                        props.NetworkingEnabled = reader.ReadBool();
                        changedList?.Add("NetworkingEnabled");
                        break;
                    case "WirelessEnabled":
                        reader.ReadSignature("b"u8);
                        props.WirelessEnabled = reader.ReadBool();
                        changedList?.Add("WirelessEnabled");
                        break;
                    case "WirelessHardwareEnabled":
                        reader.ReadSignature("b"u8);
                        props.WirelessHardwareEnabled = reader.ReadBool();
                        changedList?.Add("WirelessHardwareEnabled");
                        break;
                    case "WwanEnabled":
                        reader.ReadSignature("b"u8);
                        props.WwanEnabled = reader.ReadBool();
                        changedList?.Add("WwanEnabled");
                        break;
                    case "WwanHardwareEnabled":
                        reader.ReadSignature("b"u8);
                        props.WwanHardwareEnabled = reader.ReadBool();
                        changedList?.Add("WwanHardwareEnabled");
                        break;
                    case "WimaxEnabled":
                        reader.ReadSignature("b"u8);
                        props.WimaxEnabled = reader.ReadBool();
                        changedList?.Add("WimaxEnabled");
                        break;
                    case "WimaxHardwareEnabled":
                        reader.ReadSignature("b"u8);
                        props.WimaxHardwareEnabled = reader.ReadBool();
                        changedList?.Add("WimaxHardwareEnabled");
                        break;
                    case "RadioFlags":
                        reader.ReadSignature("u"u8);
                        props.RadioFlags = reader.ReadUInt32();
                        changedList?.Add("RadioFlags");
                        break;
                    case "ActiveConnections":
                        reader.ReadSignature("ao"u8);
                        props.ActiveConnections = reader.ReadArrayOfObjectPath();
                        changedList?.Add("ActiveConnections");
                        break;
                    case "PrimaryConnection":
                        reader.ReadSignature("o"u8);
                        props.PrimaryConnection = reader.ReadObjectPath();
                        changedList?.Add("PrimaryConnection");
                        break;
                    case "PrimaryConnectionType":
                        reader.ReadSignature("s"u8);
                        props.PrimaryConnectionType = reader.ReadString();
                        changedList?.Add("PrimaryConnectionType");
                        break;
                    case "Metered":
                        reader.ReadSignature("u"u8);
                        props.Metered = reader.ReadUInt32();
                        changedList?.Add("Metered");
                        break;
                    case "ActivatingConnection":
                        reader.ReadSignature("o"u8);
                        props.ActivatingConnection = reader.ReadObjectPath();
                        changedList?.Add("ActivatingConnection");
                        break;
                    case "Startup":
                        reader.ReadSignature("b"u8);
                        props.Startup = reader.ReadBool();
                        changedList?.Add("Startup");
                        break;
                    case "Version":
                        reader.ReadSignature("s"u8);
                        props.Version = reader.ReadString();
                        changedList?.Add("Version");
                        break;
                    case "VersionInfo":
                        reader.ReadSignature("au"u8);
                        props.VersionInfo = reader.ReadArrayOfUInt32();
                        changedList?.Add("VersionInfo");
                        break;
                    case "Capabilities":
                        reader.ReadSignature("au"u8);
                        props.Capabilities = reader.ReadArrayOfUInt32();
                        changedList?.Add("Capabilities");
                        break;
                    case "State":
                        reader.ReadSignature("u"u8);
                        props.State = reader.ReadUInt32();
                        changedList?.Add("State");
                        break;
                    case "Connectivity":
                        reader.ReadSignature("u"u8);
                        props.Connectivity = reader.ReadUInt32();
                        changedList?.Add("Connectivity");
                        break;
                    case "ConnectivityCheckAvailable":
                        reader.ReadSignature("b"u8);
                        props.ConnectivityCheckAvailable = reader.ReadBool();
                        changedList?.Add("ConnectivityCheckAvailable");
                        break;
                    case "ConnectivityCheckEnabled":
                        reader.ReadSignature("b"u8);
                        props.ConnectivityCheckEnabled = reader.ReadBool();
                        changedList?.Add("ConnectivityCheckEnabled");
                        break;
                    case "ConnectivityCheckUri":
                        reader.ReadSignature("s"u8);
                        props.ConnectivityCheckUri = reader.ReadString();
                        changedList?.Add("ConnectivityCheckUri");
                        break;
                    case "GlobalDnsConfiguration":
                        reader.ReadSignature("a{sv}"u8);
                        props.GlobalDnsConfiguration = reader.ReadDictionaryOfStringToVariantValue();
                        changedList?.Add("GlobalDnsConfiguration");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record ActiveProperties
    {
        public ObjectPath Connection { get; set; } = default!;
        public ObjectPath SpecificObject { get; set; } = default!;
        public string Id { get; set; } = default!;
        public string Uuid { get; set; } = default!;
        public string Type { get; set; } = default!;
        public ObjectPath[] Devices { get; set; } = default!;
        public uint State { get; set; } = default!;
        public uint StateFlags { get; set; } = default!;
        public bool Default { get; set; } = default!;
        public ObjectPath Ip4Config { get; set; } = default!;
        public ObjectPath Dhcp4Config { get; set; } = default!;
        public bool Default6 { get; set; } = default!;
        public ObjectPath Ip6Config { get; set; } = default!;
        public ObjectPath Dhcp6Config { get; set; } = default!;
        public bool Vpn { get; set; } = default!;
        public ObjectPath Controller { get; set; } = default!;
        public ObjectPath Master { get; set; } = default!;
    }
    partial class Active : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Connection.Active";
        public Active(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (uint State, uint Reason)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ReadMessage_uu(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public Task<ObjectPath> GetConnectionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Connection"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetSpecificObjectAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SpecificObject"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<string> GetIdAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Id"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetUuidAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Uuid"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetTypeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Type"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath[]> GetDevicesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Devices"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetStateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "State"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetStateFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "StateFlags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetDefaultAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Default"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetIp4ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip4Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetDhcp4ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Dhcp4Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetDefault6Async()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Default6"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetIp6ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip6Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetDhcp6ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Dhcp6Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetVpnAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Vpn"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetControllerAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Controller"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetMasterAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Master"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ActiveProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static ActiveProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ActiveProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<ActiveProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<ActiveProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Connection": invalidated.Add("Connection"); break;
                        case "SpecificObject": invalidated.Add("SpecificObject"); break;
                        case "Id": invalidated.Add("Id"); break;
                        case "Uuid": invalidated.Add("Uuid"); break;
                        case "Type": invalidated.Add("Type"); break;
                        case "Devices": invalidated.Add("Devices"); break;
                        case "State": invalidated.Add("State"); break;
                        case "StateFlags": invalidated.Add("StateFlags"); break;
                        case "Default": invalidated.Add("Default"); break;
                        case "Ip4Config": invalidated.Add("Ip4Config"); break;
                        case "Dhcp4Config": invalidated.Add("Dhcp4Config"); break;
                        case "Default6": invalidated.Add("Default6"); break;
                        case "Ip6Config": invalidated.Add("Ip6Config"); break;
                        case "Dhcp6Config": invalidated.Add("Dhcp6Config"); break;
                        case "Vpn": invalidated.Add("Vpn"); break;
                        case "Controller": invalidated.Add("Controller"); break;
                        case "Master": invalidated.Add("Master"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static ActiveProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new ActiveProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Connection":
                        reader.ReadSignature("o"u8);
                        props.Connection = reader.ReadObjectPath();
                        changedList?.Add("Connection");
                        break;
                    case "SpecificObject":
                        reader.ReadSignature("o"u8);
                        props.SpecificObject = reader.ReadObjectPath();
                        changedList?.Add("SpecificObject");
                        break;
                    case "Id":
                        reader.ReadSignature("s"u8);
                        props.Id = reader.ReadString();
                        changedList?.Add("Id");
                        break;
                    case "Uuid":
                        reader.ReadSignature("s"u8);
                        props.Uuid = reader.ReadString();
                        changedList?.Add("Uuid");
                        break;
                    case "Type":
                        reader.ReadSignature("s"u8);
                        props.Type = reader.ReadString();
                        changedList?.Add("Type");
                        break;
                    case "Devices":
                        reader.ReadSignature("ao"u8);
                        props.Devices = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Devices");
                        break;
                    case "State":
                        reader.ReadSignature("u"u8);
                        props.State = reader.ReadUInt32();
                        changedList?.Add("State");
                        break;
                    case "StateFlags":
                        reader.ReadSignature("u"u8);
                        props.StateFlags = reader.ReadUInt32();
                        changedList?.Add("StateFlags");
                        break;
                    case "Default":
                        reader.ReadSignature("b"u8);
                        props.Default = reader.ReadBool();
                        changedList?.Add("Default");
                        break;
                    case "Ip4Config":
                        reader.ReadSignature("o"u8);
                        props.Ip4Config = reader.ReadObjectPath();
                        changedList?.Add("Ip4Config");
                        break;
                    case "Dhcp4Config":
                        reader.ReadSignature("o"u8);
                        props.Dhcp4Config = reader.ReadObjectPath();
                        changedList?.Add("Dhcp4Config");
                        break;
                    case "Default6":
                        reader.ReadSignature("b"u8);
                        props.Default6 = reader.ReadBool();
                        changedList?.Add("Default6");
                        break;
                    case "Ip6Config":
                        reader.ReadSignature("o"u8);
                        props.Ip6Config = reader.ReadObjectPath();
                        changedList?.Add("Ip6Config");
                        break;
                    case "Dhcp6Config":
                        reader.ReadSignature("o"u8);
                        props.Dhcp6Config = reader.ReadObjectPath();
                        changedList?.Add("Dhcp6Config");
                        break;
                    case "Vpn":
                        reader.ReadSignature("b"u8);
                        props.Vpn = reader.ReadBool();
                        changedList?.Add("Vpn");
                        break;
                    case "Controller":
                        reader.ReadSignature("o"u8);
                        props.Controller = reader.ReadObjectPath();
                        changedList?.Add("Controller");
                        break;
                    case "Master":
                        reader.ReadSignature("o"u8);
                        props.Master = reader.ReadObjectPath();
                        changedList?.Add("Master");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record IP4ConfigProperties
    {
        public uint[][] Addresses { get; set; } = default!;
        public Dictionary<string, VariantValue>[] AddressData { get; set; } = default!;
        public string Gateway { get; set; } = default!;
        public uint[][] Routes { get; set; } = default!;
        public Dictionary<string, VariantValue>[] RouteData { get; set; } = default!;
        public Dictionary<string, VariantValue>[] NameserverData { get; set; } = default!;
        public uint[] Nameservers { get; set; } = default!;
        public string[] Domains { get; set; } = default!;
        public string[] Searches { get; set; } = default!;
        public string[] DnsOptions { get; set; } = default!;
        public int DnsPriority { get; set; } = default!;
        public string[] WinsServerData { get; set; } = default!;
        public uint[] WinsServers { get; set; } = default!;
    }
    partial class IP4Config : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.IP4Config";
        public IP4Config(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<uint[][]> GetAddressesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Addresses"), (Message m, object? s) => ReadMessage_v_aau(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetAddressDataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AddressData"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<string> GetGatewayAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Gateway"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint[][]> GetRoutesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Routes"), (Message m, object? s) => ReadMessage_v_aau(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetRouteDataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RouteData"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetNameserverDataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "NameserverData"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<uint[]> GetNameserversAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Nameservers"), (Message m, object? s) => ReadMessage_v_au(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetDomainsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Domains"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetSearchesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Searches"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetDnsOptionsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DnsOptions"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<int> GetDnsPriorityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DnsPriority"), (Message m, object? s) => ReadMessage_v_i(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetWinsServerDataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WinsServerData"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<uint[]> GetWinsServersAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WinsServers"), (Message m, object? s) => ReadMessage_v_au(m, (NetworkManagerObject)s!), this);
        public Task<IP4ConfigProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static IP4ConfigProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<IP4ConfigProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<IP4ConfigProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<IP4ConfigProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Addresses": invalidated.Add("Addresses"); break;
                        case "AddressData": invalidated.Add("AddressData"); break;
                        case "Gateway": invalidated.Add("Gateway"); break;
                        case "Routes": invalidated.Add("Routes"); break;
                        case "RouteData": invalidated.Add("RouteData"); break;
                        case "NameserverData": invalidated.Add("NameserverData"); break;
                        case "Nameservers": invalidated.Add("Nameservers"); break;
                        case "Domains": invalidated.Add("Domains"); break;
                        case "Searches": invalidated.Add("Searches"); break;
                        case "DnsOptions": invalidated.Add("DnsOptions"); break;
                        case "DnsPriority": invalidated.Add("DnsPriority"); break;
                        case "WinsServerData": invalidated.Add("WinsServerData"); break;
                        case "WinsServers": invalidated.Add("WinsServers"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static IP4ConfigProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new IP4ConfigProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Addresses":
                        reader.ReadSignature("aau"u8);
                        props.Addresses = ReadType_aau(ref reader);
                        changedList?.Add("Addresses");
                        break;
                    case "AddressData":
                        reader.ReadSignature("aa{sv}"u8);
                        props.AddressData = ReadType_aaesv(ref reader);
                        changedList?.Add("AddressData");
                        break;
                    case "Gateway":
                        reader.ReadSignature("s"u8);
                        props.Gateway = reader.ReadString();
                        changedList?.Add("Gateway");
                        break;
                    case "Routes":
                        reader.ReadSignature("aau"u8);
                        props.Routes = ReadType_aau(ref reader);
                        changedList?.Add("Routes");
                        break;
                    case "RouteData":
                        reader.ReadSignature("aa{sv}"u8);
                        props.RouteData = ReadType_aaesv(ref reader);
                        changedList?.Add("RouteData");
                        break;
                    case "NameserverData":
                        reader.ReadSignature("aa{sv}"u8);
                        props.NameserverData = ReadType_aaesv(ref reader);
                        changedList?.Add("NameserverData");
                        break;
                    case "Nameservers":
                        reader.ReadSignature("au"u8);
                        props.Nameservers = reader.ReadArrayOfUInt32();
                        changedList?.Add("Nameservers");
                        break;
                    case "Domains":
                        reader.ReadSignature("as"u8);
                        props.Domains = reader.ReadArrayOfString();
                        changedList?.Add("Domains");
                        break;
                    case "Searches":
                        reader.ReadSignature("as"u8);
                        props.Searches = reader.ReadArrayOfString();
                        changedList?.Add("Searches");
                        break;
                    case "DnsOptions":
                        reader.ReadSignature("as"u8);
                        props.DnsOptions = reader.ReadArrayOfString();
                        changedList?.Add("DnsOptions");
                        break;
                    case "DnsPriority":
                        reader.ReadSignature("i"u8);
                        props.DnsPriority = reader.ReadInt32();
                        changedList?.Add("DnsPriority");
                        break;
                    case "WinsServerData":
                        reader.ReadSignature("as"u8);
                        props.WinsServerData = reader.ReadArrayOfString();
                        changedList?.Add("WinsServerData");
                        break;
                    case "WinsServers":
                        reader.ReadSignature("au"u8);
                        props.WinsServers = reader.ReadArrayOfUInt32();
                        changedList?.Add("WinsServers");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record StatisticsProperties
    {
        public uint RefreshRateMs { get; set; } = default!;
        public ulong TxBytes { get; set; } = default!;
        public ulong RxBytes { get; set; } = default!;
    }
    partial class Statistics : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Device.Statistics";
        public Statistics(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task SetRefreshRateMsAsync(uint value)
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
                writer.WriteString("RefreshRateMs");
                writer.WriteSignature("u");
                writer.WriteUInt32(value);
                return writer.CreateMessage();
            }
        }
        public Task<uint> GetRefreshRateMsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RefreshRateMs"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<ulong> GetTxBytesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "TxBytes"), (Message m, object? s) => ReadMessage_v_t(m, (NetworkManagerObject)s!), this);
        public Task<ulong> GetRxBytesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RxBytes"), (Message m, object? s) => ReadMessage_v_t(m, (NetworkManagerObject)s!), this);
        public Task<StatisticsProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static StatisticsProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<StatisticsProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<StatisticsProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<StatisticsProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "RefreshRateMs": invalidated.Add("RefreshRateMs"); break;
                        case "TxBytes": invalidated.Add("TxBytes"); break;
                        case "RxBytes": invalidated.Add("RxBytes"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static StatisticsProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new StatisticsProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "RefreshRateMs":
                        reader.ReadSignature("u"u8);
                        props.RefreshRateMs = reader.ReadUInt32();
                        changedList?.Add("RefreshRateMs");
                        break;
                    case "TxBytes":
                        reader.ReadSignature("t"u8);
                        props.TxBytes = reader.ReadUInt64();
                        changedList?.Add("TxBytes");
                        break;
                    case "RxBytes":
                        reader.ReadSignature("t"u8);
                        props.RxBytes = reader.ReadUInt64();
                        changedList?.Add("RxBytes");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record DeviceProperties
    {
        public string Udi { get; set; } = default!;
        public string Path { get; set; } = default!;
        public string Interface { get; set; } = default!;
        public string IpInterface { get; set; } = default!;
        public string Driver { get; set; } = default!;
        public string DriverVersion { get; set; } = default!;
        public string FirmwareVersion { get; set; } = default!;
        public uint Capabilities { get; set; } = default!;
        public uint Ip4Address { get; set; } = default!;
        public uint State { get; set; } = default!;
        public (uint, uint) StateReason { get; set; } = default!;
        public ObjectPath ActiveConnection { get; set; } = default!;
        public ObjectPath Ip4Config { get; set; } = default!;
        public ObjectPath Dhcp4Config { get; set; } = default!;
        public ObjectPath Ip6Config { get; set; } = default!;
        public ObjectPath Dhcp6Config { get; set; } = default!;
        public bool Managed { get; set; } = default!;
        public bool Autoconnect { get; set; } = default!;
        public bool FirmwareMissing { get; set; } = default!;
        public bool NmPluginMissing { get; set; } = default!;
        public uint DeviceType { get; set; } = default!;
        public ObjectPath[] AvailableConnections { get; set; } = default!;
        public string PhysicalPortId { get; set; } = default!;
        public uint Mtu { get; set; } = default!;
        public uint Metered { get; set; } = default!;
        public Dictionary<string, VariantValue>[] LldpNeighbors { get; set; } = default!;
        public bool Real { get; set; } = default!;
        public uint Ip4Connectivity { get; set; } = default!;
        public uint Ip6Connectivity { get; set; } = default!;
        public uint InterfaceFlags { get; set; } = default!;
        public string HwAddress { get; set; } = default!;
        public ObjectPath[] Ports { get; set; } = default!;
    }
    partial class Device : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Device";
        public Device(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task ReapplyAsync(Dictionary<string, Dictionary<string, VariantValue>> connection, ulong versionId, uint flags)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}tu",
                    member: "Reapply");
                WriteType_aesaesv(ref writer, connection);
                writer.WriteUInt64(versionId);
                writer.WriteUInt32(flags);
                return writer.CreateMessage();
            }
        }
        public Task<(Dictionary<string, Dictionary<string, VariantValue>> Connection, ulong VersionId)> GetAppliedConnectionAsync(uint flags)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesaesvt(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "u",
                    member: "GetAppliedConnection");
                writer.WriteUInt32(flags);
                return writer.CreateMessage();
            }
        }
        public Task DisconnectAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Disconnect");
                return writer.CreateMessage();
            }
        }
        public Task DeleteAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Delete");
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (DeviceState NewState, DeviceState OldState, uint Reason)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ((DeviceState, DeviceState, uint))ReadMessage_uuu(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public Task SetManagedAsync(bool value)
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
                writer.WriteString("Managed");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task SetAutoconnectAsync(bool value)
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
                writer.WriteString("Autoconnect");
                writer.WriteSignature("b");
                writer.WriteBool(value);
                return writer.CreateMessage();
            }
        }
        public Task<string> GetUdiAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Udi"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetPathAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Path"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetInterfaceAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Interface"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetIpInterfaceAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "IpInterface"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetDriverAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Driver"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetDriverVersionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DriverVersion"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetFirmwareVersionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "FirmwareVersion"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetCapabilitiesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Capabilities"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetIp4AddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip4Address"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetStateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "State"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<(uint, uint)> GetStateReasonAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "StateReason"), (Message m, object? s) => ReadMessage_v_ruuz(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetActiveConnectionAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ActiveConnection"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetIp4ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip4Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetDhcp4ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Dhcp4Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetIp6ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip6Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetDhcp6ConfigAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Dhcp6Config"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetManagedAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Managed"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetAutoconnectAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Autoconnect"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetFirmwareMissingAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "FirmwareMissing"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetNmPluginMissingAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "NmPluginMissing"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetDeviceTypeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DeviceType"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath[]> GetAvailableConnectionsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AvailableConnections"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<string> GetPhysicalPortIdAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PhysicalPortId"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetMtuAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Mtu"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetMeteredAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Metered"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetLldpNeighborsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "LldpNeighbors"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetRealAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Real"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetIp4ConnectivityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip4Connectivity"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetIp6ConnectivityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ip6Connectivity"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetInterfaceFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "InterfaceFlags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<string> GetHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath[]> GetPortsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ports"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<DeviceProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static DeviceProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DeviceProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<DeviceProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<DeviceProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Udi": invalidated.Add("Udi"); break;
                        case "Path": invalidated.Add("Path"); break;
                        case "Interface": invalidated.Add("Interface"); break;
                        case "IpInterface": invalidated.Add("IpInterface"); break;
                        case "Driver": invalidated.Add("Driver"); break;
                        case "DriverVersion": invalidated.Add("DriverVersion"); break;
                        case "FirmwareVersion": invalidated.Add("FirmwareVersion"); break;
                        case "Capabilities": invalidated.Add("Capabilities"); break;
                        case "Ip4Address": invalidated.Add("Ip4Address"); break;
                        case "State": invalidated.Add("State"); break;
                        case "StateReason": invalidated.Add("StateReason"); break;
                        case "ActiveConnection": invalidated.Add("ActiveConnection"); break;
                        case "Ip4Config": invalidated.Add("Ip4Config"); break;
                        case "Dhcp4Config": invalidated.Add("Dhcp4Config"); break;
                        case "Ip6Config": invalidated.Add("Ip6Config"); break;
                        case "Dhcp6Config": invalidated.Add("Dhcp6Config"); break;
                        case "Managed": invalidated.Add("Managed"); break;
                        case "Autoconnect": invalidated.Add("Autoconnect"); break;
                        case "FirmwareMissing": invalidated.Add("FirmwareMissing"); break;
                        case "NmPluginMissing": invalidated.Add("NmPluginMissing"); break;
                        case "DeviceType": invalidated.Add("DeviceType"); break;
                        case "AvailableConnections": invalidated.Add("AvailableConnections"); break;
                        case "PhysicalPortId": invalidated.Add("PhysicalPortId"); break;
                        case "Mtu": invalidated.Add("Mtu"); break;
                        case "Metered": invalidated.Add("Metered"); break;
                        case "LldpNeighbors": invalidated.Add("LldpNeighbors"); break;
                        case "Real": invalidated.Add("Real"); break;
                        case "Ip4Connectivity": invalidated.Add("Ip4Connectivity"); break;
                        case "Ip6Connectivity": invalidated.Add("Ip6Connectivity"); break;
                        case "InterfaceFlags": invalidated.Add("InterfaceFlags"); break;
                        case "HwAddress": invalidated.Add("HwAddress"); break;
                        case "Ports": invalidated.Add("Ports"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static DeviceProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new DeviceProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Udi":
                        reader.ReadSignature("s"u8);
                        props.Udi = reader.ReadString();
                        changedList?.Add("Udi");
                        break;
                    case "Path":
                        reader.ReadSignature("s"u8);
                        props.Path = reader.ReadString();
                        changedList?.Add("Path");
                        break;
                    case "Interface":
                        reader.ReadSignature("s"u8);
                        props.Interface = reader.ReadString();
                        changedList?.Add("Interface");
                        break;
                    case "IpInterface":
                        reader.ReadSignature("s"u8);
                        props.IpInterface = reader.ReadString();
                        changedList?.Add("IpInterface");
                        break;
                    case "Driver":
                        reader.ReadSignature("s"u8);
                        props.Driver = reader.ReadString();
                        changedList?.Add("Driver");
                        break;
                    case "DriverVersion":
                        reader.ReadSignature("s"u8);
                        props.DriverVersion = reader.ReadString();
                        changedList?.Add("DriverVersion");
                        break;
                    case "FirmwareVersion":
                        reader.ReadSignature("s"u8);
                        props.FirmwareVersion = reader.ReadString();
                        changedList?.Add("FirmwareVersion");
                        break;
                    case "Capabilities":
                        reader.ReadSignature("u"u8);
                        props.Capabilities = reader.ReadUInt32();
                        changedList?.Add("Capabilities");
                        break;
                    case "Ip4Address":
                        reader.ReadSignature("u"u8);
                        props.Ip4Address = reader.ReadUInt32();
                        changedList?.Add("Ip4Address");
                        break;
                    case "State":
                        reader.ReadSignature("u"u8);
                        props.State = reader.ReadUInt32();
                        changedList?.Add("State");
                        break;
                    case "StateReason":
                        reader.ReadSignature("(uu)"u8);
                        props.StateReason = ReadType_ruuz(ref reader);
                        changedList?.Add("StateReason");
                        break;
                    case "ActiveConnection":
                        reader.ReadSignature("o"u8);
                        props.ActiveConnection = reader.ReadObjectPath();
                        changedList?.Add("ActiveConnection");
                        break;
                    case "Ip4Config":
                        reader.ReadSignature("o"u8);
                        props.Ip4Config = reader.ReadObjectPath();
                        changedList?.Add("Ip4Config");
                        break;
                    case "Dhcp4Config":
                        reader.ReadSignature("o"u8);
                        props.Dhcp4Config = reader.ReadObjectPath();
                        changedList?.Add("Dhcp4Config");
                        break;
                    case "Ip6Config":
                        reader.ReadSignature("o"u8);
                        props.Ip6Config = reader.ReadObjectPath();
                        changedList?.Add("Ip6Config");
                        break;
                    case "Dhcp6Config":
                        reader.ReadSignature("o"u8);
                        props.Dhcp6Config = reader.ReadObjectPath();
                        changedList?.Add("Dhcp6Config");
                        break;
                    case "Managed":
                        reader.ReadSignature("b"u8);
                        props.Managed = reader.ReadBool();
                        changedList?.Add("Managed");
                        break;
                    case "Autoconnect":
                        reader.ReadSignature("b"u8);
                        props.Autoconnect = reader.ReadBool();
                        changedList?.Add("Autoconnect");
                        break;
                    case "FirmwareMissing":
                        reader.ReadSignature("b"u8);
                        props.FirmwareMissing = reader.ReadBool();
                        changedList?.Add("FirmwareMissing");
                        break;
                    case "NmPluginMissing":
                        reader.ReadSignature("b"u8);
                        props.NmPluginMissing = reader.ReadBool();
                        changedList?.Add("NmPluginMissing");
                        break;
                    case "DeviceType":
                        reader.ReadSignature("u"u8);
                        props.DeviceType = reader.ReadUInt32();
                        changedList?.Add("DeviceType");
                        break;
                    case "AvailableConnections":
                        reader.ReadSignature("ao"u8);
                        props.AvailableConnections = reader.ReadArrayOfObjectPath();
                        changedList?.Add("AvailableConnections");
                        break;
                    case "PhysicalPortId":
                        reader.ReadSignature("s"u8);
                        props.PhysicalPortId = reader.ReadString();
                        changedList?.Add("PhysicalPortId");
                        break;
                    case "Mtu":
                        reader.ReadSignature("u"u8);
                        props.Mtu = reader.ReadUInt32();
                        changedList?.Add("Mtu");
                        break;
                    case "Metered":
                        reader.ReadSignature("u"u8);
                        props.Metered = reader.ReadUInt32();
                        changedList?.Add("Metered");
                        break;
                    case "LldpNeighbors":
                        reader.ReadSignature("aa{sv}"u8);
                        props.LldpNeighbors = ReadType_aaesv(ref reader);
                        changedList?.Add("LldpNeighbors");
                        break;
                    case "Real":
                        reader.ReadSignature("b"u8);
                        props.Real = reader.ReadBool();
                        changedList?.Add("Real");
                        break;
                    case "Ip4Connectivity":
                        reader.ReadSignature("u"u8);
                        props.Ip4Connectivity = reader.ReadUInt32();
                        changedList?.Add("Ip4Connectivity");
                        break;
                    case "Ip6Connectivity":
                        reader.ReadSignature("u"u8);
                        props.Ip6Connectivity = reader.ReadUInt32();
                        changedList?.Add("Ip6Connectivity");
                        break;
                    case "InterfaceFlags":
                        reader.ReadSignature("u"u8);
                        props.InterfaceFlags = reader.ReadUInt32();
                        changedList?.Add("InterfaceFlags");
                        break;
                    case "HwAddress":
                        reader.ReadSignature("s"u8);
                        props.HwAddress = reader.ReadString();
                        changedList?.Add("HwAddress");
                        break;
                    case "Ports":
                        reader.ReadSignature("ao"u8);
                        props.Ports = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Ports");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record WiredProperties
    {
        public string HwAddress { get; set; } = default!;
        public string PermHwAddress { get; set; } = default!;
        public uint Speed { get; set; } = default!;
        public string[] S390Subchannels { get; set; } = default!;
        public bool Carrier { get; set; } = default!;
    }
    partial class Wired : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Device.Wired";
        public Wired(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<string> GetHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetPermHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PermHwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetSpeedAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Speed"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetS390SubchannelsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "S390Subchannels"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetCarrierAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Carrier"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<WiredProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static WiredProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<WiredProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<WiredProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<WiredProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "HwAddress": invalidated.Add("HwAddress"); break;
                        case "PermHwAddress": invalidated.Add("PermHwAddress"); break;
                        case "Speed": invalidated.Add("Speed"); break;
                        case "S390Subchannels": invalidated.Add("S390Subchannels"); break;
                        case "Carrier": invalidated.Add("Carrier"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static WiredProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new WiredProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "HwAddress":
                        reader.ReadSignature("s"u8);
                        props.HwAddress = reader.ReadString();
                        changedList?.Add("HwAddress");
                        break;
                    case "PermHwAddress":
                        reader.ReadSignature("s"u8);
                        props.PermHwAddress = reader.ReadString();
                        changedList?.Add("PermHwAddress");
                        break;
                    case "Speed":
                        reader.ReadSignature("u"u8);
                        props.Speed = reader.ReadUInt32();
                        changedList?.Add("Speed");
                        break;
                    case "S390Subchannels":
                        reader.ReadSignature("as"u8);
                        props.S390Subchannels = reader.ReadArrayOfString();
                        changedList?.Add("S390Subchannels");
                        break;
                    case "Carrier":
                        reader.ReadSignature("b"u8);
                        props.Carrier = reader.ReadBool();
                        changedList?.Add("Carrier");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record WirelessProperties
    {
        public string HwAddress { get; set; } = default!;
        public string PermHwAddress { get; set; } = default!;
        public uint Mode { get; set; } = default!;
        public uint Bitrate { get; set; } = default!;
        public ObjectPath[] AccessPoints { get; set; } = default!;
        public ObjectPath ActiveAccessPoint { get; set; } = default!;
        public uint WirelessCapabilities { get; set; } = default!;
        public long LastScan { get; set; } = default!;
    }
    partial class Wireless : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Device.Wireless";
        public Wireless(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<ObjectPath[]> GetAccessPointsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ao(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetAccessPoints");
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath[]> GetAllAccessPointsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ao(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetAllAccessPoints");
                return writer.CreateMessage();
            }
        }
        public Task RequestScanAsync(Dictionary<string, VariantValue> options)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sv}",
                    member: "RequestScan");
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchAccessPointAddedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "AccessPointAdded", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchAccessPointRemovedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "AccessPointRemoved", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public Task<string> GetHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetPermHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "PermHwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetModeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Mode"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetBitrateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Bitrate"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        //public Task<ObjectPath[]> GetAccessPointsAsync()
        //    => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AccessPoints"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath> GetActiveAccessPointAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "ActiveAccessPoint"), (Message m, object? s) => ReadMessage_v_o(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetWirelessCapabilitiesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WirelessCapabilities"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<long> GetLastScanAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "LastScan"), (Message m, object? s) => ReadMessage_v_x(m, (NetworkManagerObject)s!), this);
        public Task<WirelessProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static WirelessProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<WirelessProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<WirelessProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<WirelessProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "HwAddress": invalidated.Add("HwAddress"); break;
                        case "PermHwAddress": invalidated.Add("PermHwAddress"); break;
                        case "Mode": invalidated.Add("Mode"); break;
                        case "Bitrate": invalidated.Add("Bitrate"); break;
                        case "AccessPoints": invalidated.Add("AccessPoints"); break;
                        case "ActiveAccessPoint": invalidated.Add("ActiveAccessPoint"); break;
                        case "WirelessCapabilities": invalidated.Add("WirelessCapabilities"); break;
                        case "LastScan": invalidated.Add("LastScan"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static WirelessProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new WirelessProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "HwAddress":
                        reader.ReadSignature("s"u8);
                        props.HwAddress = reader.ReadString();
                        changedList?.Add("HwAddress");
                        break;
                    case "PermHwAddress":
                        reader.ReadSignature("s"u8);
                        props.PermHwAddress = reader.ReadString();
                        changedList?.Add("PermHwAddress");
                        break;
                    case "Mode":
                        reader.ReadSignature("u"u8);
                        props.Mode = reader.ReadUInt32();
                        changedList?.Add("Mode");
                        break;
                    case "Bitrate":
                        reader.ReadSignature("u"u8);
                        props.Bitrate = reader.ReadUInt32();
                        changedList?.Add("Bitrate");
                        break;
                    case "AccessPoints":
                        reader.ReadSignature("ao"u8);
                        props.AccessPoints = reader.ReadArrayOfObjectPath();
                        changedList?.Add("AccessPoints");
                        break;
                    case "ActiveAccessPoint":
                        reader.ReadSignature("o"u8);
                        props.ActiveAccessPoint = reader.ReadObjectPath();
                        changedList?.Add("ActiveAccessPoint");
                        break;
                    case "WirelessCapabilities":
                        reader.ReadSignature("u"u8);
                        props.WirelessCapabilities = reader.ReadUInt32();
                        changedList?.Add("WirelessCapabilities");
                        break;
                    case "LastScan":
                        reader.ReadSignature("x"u8);
                        props.LastScan = reader.ReadInt64();
                        changedList?.Add("LastScan");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    partial class Loopback : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Device.Loopback";
        public Loopback(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
    }
    record WifiP2PProperties
    {
        public string HwAddress { get; set; } = default!;
        public ObjectPath[] Peers { get; set; } = default!;
    }
    partial class WifiP2P : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Device.WifiP2P";
        public WifiP2P(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task StartFindAsync(Dictionary<string, VariantValue> options)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sv}",
                    member: "StartFind");
                writer.WriteDictionary(options);
                return writer.CreateMessage();
            }
        }
        public Task StopFindAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "StopFind");
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchPeerAddedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "PeerAdded", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchPeerRemovedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "PeerRemoved", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public Task<string> GetHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<ObjectPath[]> GetPeersAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Peers"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<WifiP2PProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static WifiP2PProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<WifiP2PProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<WifiP2PProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<WifiP2PProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "HwAddress": invalidated.Add("HwAddress"); break;
                        case "Peers": invalidated.Add("Peers"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static WifiP2PProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new WifiP2PProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "HwAddress":
                        reader.ReadSignature("s"u8);
                        props.HwAddress = reader.ReadString();
                        changedList?.Add("HwAddress");
                        break;
                    case "Peers":
                        reader.ReadSignature("ao"u8);
                        props.Peers = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Peers");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    partial class AgentManager : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.AgentManager";
        public AgentManager(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task RegisterAsync(string identifier)
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
                    member: "Register");
                writer.WriteString(identifier);
                return writer.CreateMessage();
            }
        }
        public Task RegisterWithCapabilitiesAsync(string identifier, uint capabilities)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "su",
                    member: "RegisterWithCapabilities");
                writer.WriteString(identifier);
                writer.WriteUInt32(capabilities);
                return writer.CreateMessage();
            }
        }
        public Task UnregisterAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Unregister");
                return writer.CreateMessage();
            }
        }
    }
    record DHCP4ConfigProperties
    {
        public Dictionary<string, VariantValue> Options { get; set; } = default!;
    }
    partial class DHCP4Config : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.DHCP4Config";
        public DHCP4Config(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<Dictionary<string, VariantValue>> GetOptionsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Options"), (Message m, object? s) => ReadMessage_v_aesv(m, (NetworkManagerObject)s!), this);
        public Task<DHCP4ConfigProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static DHCP4ConfigProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DHCP4ConfigProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<DHCP4ConfigProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<DHCP4ConfigProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Options": invalidated.Add("Options"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static DHCP4ConfigProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new DHCP4ConfigProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Options":
                        reader.ReadSignature("a{sv}"u8);
                        props.Options = reader.ReadDictionaryOfStringToVariantValue();
                        changedList?.Add("Options");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record DnsManagerProperties
    {
        public string Mode { get; set; } = default!;
        public string RcManager { get; set; } = default!;
        public Dictionary<string, VariantValue>[] Configuration { get; set; } = default!;
    }
    partial class DnsManager : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.DnsManager";
        public DnsManager(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<string> GetModeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Mode"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<string> GetRcManagerAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RcManager"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetConfigurationAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Configuration"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<DnsManagerProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static DnsManagerProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DnsManagerProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<DnsManagerProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<DnsManagerProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Mode": invalidated.Add("Mode"); break;
                        case "RcManager": invalidated.Add("RcManager"); break;
                        case "Configuration": invalidated.Add("Configuration"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static DnsManagerProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new DnsManagerProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Mode":
                        reader.ReadSignature("s"u8);
                        props.Mode = reader.ReadString();
                        changedList?.Add("Mode");
                        break;
                    case "RcManager":
                        reader.ReadSignature("s"u8);
                        props.RcManager = reader.ReadString();
                        changedList?.Add("RcManager");
                        break;
                    case "Configuration":
                        reader.ReadSignature("aa{sv}"u8);
                        props.Configuration = ReadType_aaesv(ref reader);
                        changedList?.Add("Configuration");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record AccessPointProperties
    {
        public uint Flags { get; set; } = default!;
        public uint WpaFlags { get; set; } = default!;
        public uint RsnFlags { get; set; } = default!;
        public byte[] Ssid { get; set; } = default!;
        public uint Frequency { get; set; } = default!;
        public string HwAddress { get; set; } = default!;
        public uint Mode { get; set; } = default!;
        public uint MaxBitrate { get; set; } = default!;
        public byte Strength { get; set; } = default!;
        public int LastSeen { get; set; } = default!;
    }
    partial class AccessPoint : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.AccessPoint";
        public AccessPoint(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<uint> GetFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Flags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetWpaFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "WpaFlags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetRsnFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RsnFlags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<byte[]> GetSsidAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Ssid"), (Message m, object? s) => ReadMessage_v_ay(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetFrequencyAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Frequency"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<string> GetHwAddressAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "HwAddress"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetModeAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Mode"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetMaxBitrateAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MaxBitrate"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<byte> GetStrengthAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Strength"), (Message m, object? s) => ReadMessage_v_y(m, (NetworkManagerObject)s!), this);
        public Task<int> GetLastSeenAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "LastSeen"), (Message m, object? s) => ReadMessage_v_i(m, (NetworkManagerObject)s!), this);
        public Task<AccessPointProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static AccessPointProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<AccessPointProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<AccessPointProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<AccessPointProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Flags": invalidated.Add("Flags"); break;
                        case "WpaFlags": invalidated.Add("WpaFlags"); break;
                        case "RsnFlags": invalidated.Add("RsnFlags"); break;
                        case "Ssid": invalidated.Add("Ssid"); break;
                        case "Frequency": invalidated.Add("Frequency"); break;
                        case "HwAddress": invalidated.Add("HwAddress"); break;
                        case "Mode": invalidated.Add("Mode"); break;
                        case "MaxBitrate": invalidated.Add("MaxBitrate"); break;
                        case "Strength": invalidated.Add("Strength"); break;
                        case "LastSeen": invalidated.Add("LastSeen"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static AccessPointProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new AccessPointProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Flags":
                        reader.ReadSignature("u"u8);
                        props.Flags = reader.ReadUInt32();
                        changedList?.Add("Flags");
                        break;
                    case "WpaFlags":
                        reader.ReadSignature("u"u8);
                        props.WpaFlags = reader.ReadUInt32();
                        changedList?.Add("WpaFlags");
                        break;
                    case "RsnFlags":
                        reader.ReadSignature("u"u8);
                        props.RsnFlags = reader.ReadUInt32();
                        changedList?.Add("RsnFlags");
                        break;
                    case "Ssid":
                        reader.ReadSignature("ay"u8);
                        props.Ssid = reader.ReadArrayOfByte();
                        changedList?.Add("Ssid");
                        break;
                    case "Frequency":
                        reader.ReadSignature("u"u8);
                        props.Frequency = reader.ReadUInt32();
                        changedList?.Add("Frequency");
                        break;
                    case "HwAddress":
                        reader.ReadSignature("s"u8);
                        props.HwAddress = reader.ReadString();
                        changedList?.Add("HwAddress");
                        break;
                    case "Mode":
                        reader.ReadSignature("u"u8);
                        props.Mode = reader.ReadUInt32();
                        changedList?.Add("Mode");
                        break;
                    case "MaxBitrate":
                        reader.ReadSignature("u"u8);
                        props.MaxBitrate = reader.ReadUInt32();
                        changedList?.Add("MaxBitrate");
                        break;
                    case "Strength":
                        reader.ReadSignature("y"u8);
                        props.Strength = reader.ReadByte();
                        changedList?.Add("Strength");
                        break;
                    case "LastSeen":
                        reader.ReadSignature("i"u8);
                        props.LastSeen = reader.ReadInt32();
                        changedList?.Add("LastSeen");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record IP6ConfigProperties
    {
        public (byte[], uint, byte[])[] Addresses { get; set; } = default!;
        public Dictionary<string, VariantValue>[] AddressData { get; set; } = default!;
        public string Gateway { get; set; } = default!;
        public (byte[], uint, byte[], uint)[] Routes { get; set; } = default!;
        public Dictionary<string, VariantValue>[] RouteData { get; set; } = default!;
        public byte[][] Nameservers { get; set; } = default!;
        public string[] Domains { get; set; } = default!;
        public string[] Searches { get; set; } = default!;
        public string[] DnsOptions { get; set; } = default!;
        public int DnsPriority { get; set; } = default!;
    }
    partial class IP6Config : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.IP6Config";
        public IP6Config(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<(byte[], uint, byte[])[]> GetAddressesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Addresses"), (Message m, object? s) => ReadMessage_v_arayuayz(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetAddressDataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AddressData"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<string> GetGatewayAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Gateway"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<(byte[], uint, byte[], uint)[]> GetRoutesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Routes"), (Message m, object? s) => ReadMessage_v_arayuayuz(m, (NetworkManagerObject)s!), this);
        public Task<Dictionary<string, VariantValue>[]> GetRouteDataAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RouteData"), (Message m, object? s) => ReadMessage_v_aaesv(m, (NetworkManagerObject)s!), this);
        public Task<byte[][]> GetNameserversAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Nameservers"), (Message m, object? s) => ReadMessage_v_aay(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetDomainsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Domains"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetSearchesAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Searches"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<string[]> GetDnsOptionsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DnsOptions"), (Message m, object? s) => ReadMessage_v_as(m, (NetworkManagerObject)s!), this);
        public Task<int> GetDnsPriorityAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "DnsPriority"), (Message m, object? s) => ReadMessage_v_i(m, (NetworkManagerObject)s!), this);
        public Task<IP6ConfigProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static IP6ConfigProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<IP6ConfigProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<IP6ConfigProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<IP6ConfigProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Addresses": invalidated.Add("Addresses"); break;
                        case "AddressData": invalidated.Add("AddressData"); break;
                        case "Gateway": invalidated.Add("Gateway"); break;
                        case "Routes": invalidated.Add("Routes"); break;
                        case "RouteData": invalidated.Add("RouteData"); break;
                        case "Nameservers": invalidated.Add("Nameservers"); break;
                        case "Domains": invalidated.Add("Domains"); break;
                        case "Searches": invalidated.Add("Searches"); break;
                        case "DnsOptions": invalidated.Add("DnsOptions"); break;
                        case "DnsPriority": invalidated.Add("DnsPriority"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static IP6ConfigProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new IP6ConfigProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Addresses":
                        reader.ReadSignature("a(ayuay)"u8);
                        props.Addresses = ReadType_arayuayz(ref reader);
                        changedList?.Add("Addresses");
                        break;
                    case "AddressData":
                        reader.ReadSignature("aa{sv}"u8);
                        props.AddressData = ReadType_aaesv(ref reader);
                        changedList?.Add("AddressData");
                        break;
                    case "Gateway":
                        reader.ReadSignature("s"u8);
                        props.Gateway = reader.ReadString();
                        changedList?.Add("Gateway");
                        break;
                    case "Routes":
                        reader.ReadSignature("a(ayuayu)"u8);
                        props.Routes = ReadType_arayuayuz(ref reader);
                        changedList?.Add("Routes");
                        break;
                    case "RouteData":
                        reader.ReadSignature("aa{sv}"u8);
                        props.RouteData = ReadType_aaesv(ref reader);
                        changedList?.Add("RouteData");
                        break;
                    case "Nameservers":
                        reader.ReadSignature("aay"u8);
                        props.Nameservers = ReadType_aay(ref reader);
                        changedList?.Add("Nameservers");
                        break;
                    case "Domains":
                        reader.ReadSignature("as"u8);
                        props.Domains = reader.ReadArrayOfString();
                        changedList?.Add("Domains");
                        break;
                    case "Searches":
                        reader.ReadSignature("as"u8);
                        props.Searches = reader.ReadArrayOfString();
                        changedList?.Add("Searches");
                        break;
                    case "DnsOptions":
                        reader.ReadSignature("as"u8);
                        props.DnsOptions = reader.ReadArrayOfString();
                        changedList?.Add("DnsOptions");
                        break;
                    case "DnsPriority":
                        reader.ReadSignature("i"u8);
                        props.DnsPriority = reader.ReadInt32();
                        changedList?.Add("DnsPriority");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record SettingsProperties
    {
        public ObjectPath[] Connections { get; set; } = default!;
        public string Hostname { get; set; } = default!;
        public bool CanModify { get; set; } = default!;
    }
    partial class Settings : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Settings";
        public Settings(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task<ObjectPath[]> ListConnectionsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_ao(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "ListConnections");
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath> GetConnectionByUuidAsync(string uuid)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "s",
                    member: "GetConnectionByUuid");
                writer.WriteString(uuid);
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath> AddConnectionAsync(Dictionary<string, Dictionary<string, VariantValue>> connection)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}",
                    member: "AddConnection");
                WriteType_aesaesv(ref writer, connection);
                return writer.CreateMessage();
            }
        }
        public Task<ObjectPath> AddConnectionUnsavedAsync(Dictionary<string, Dictionary<string, VariantValue>> connection)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}",
                    member: "AddConnectionUnsaved");
                WriteType_aesaesv(ref writer, connection);
                return writer.CreateMessage();
            }
        }
        public Task<(ObjectPath Path, Dictionary<string, VariantValue> Result)> AddConnection2Async(Dictionary<string, Dictionary<string, VariantValue>> settings, uint flags, Dictionary<string, VariantValue> args)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_oaesv(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}ua{sv}",
                    member: "AddConnection2");
                WriteType_aesaesv(ref writer, settings);
                writer.WriteUInt32(flags);
                writer.WriteDictionary(args);
                return writer.CreateMessage();
            }
        }
        public Task<(bool Status, string[] Failures)> LoadConnectionsAsync(string[] filenames)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_bas(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "as",
                    member: "LoadConnections");
                writer.WriteArray(filenames);
                return writer.CreateMessage();
            }
        }
        public Task<bool> ReloadConnectionsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "ReloadConnections");
                return writer.CreateMessage();
            }
        }
        public Task SaveHostnameAsync(string hostname)
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
                    member: "SaveHostname");
                writer.WriteString(hostname);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchNewConnectionAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "NewConnection", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchConnectionRemovedAsync(Action<Exception?, ObjectPath> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "ConnectionRemoved", (Message m, object? s) => ReadMessage_o(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
        public Task<ObjectPath[]> GetConnectionsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Connections"), (Message m, object? s) => ReadMessage_v_ao(m, (NetworkManagerObject)s!), this);
        public Task<string> GetHostnameAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Hostname"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<bool> GetCanModifyAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "CanModify"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<SettingsProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static SettingsProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<SettingsProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<SettingsProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<SettingsProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Connections": invalidated.Add("Connections"); break;
                        case "Hostname": invalidated.Add("Hostname"); break;
                        case "CanModify": invalidated.Add("CanModify"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static SettingsProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new SettingsProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Connections":
                        reader.ReadSignature("ao"u8);
                        props.Connections = reader.ReadArrayOfObjectPath();
                        changedList?.Add("Connections");
                        break;
                    case "Hostname":
                        reader.ReadSignature("s"u8);
                        props.Hostname = reader.ReadString();
                        changedList?.Add("Hostname");
                        break;
                    case "CanModify":
                        reader.ReadSignature("b"u8);
                        props.CanModify = reader.ReadBool();
                        changedList?.Add("CanModify");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    record ConnectionProperties
    {
        public bool Unsaved { get; set; } = default!;
        public uint Flags { get; set; } = default!;
        public string Filename { get; set; } = default!;
    }
    partial class Connection : NetworkManagerObject
    {
        private const string __Interface = "org.freedesktop.NetworkManager.Settings.Connection";
        public Connection(NetworkManagerService service, ObjectPath path) : base(service, path)
        { }
        public Task UpdateAsync(Dictionary<string, Dictionary<string, VariantValue>> properties)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}",
                    member: "Update");
                WriteType_aesaesv(ref writer, properties);
                return writer.CreateMessage();
            }
        }
        public Task UpdateUnsavedAsync(Dictionary<string, Dictionary<string, VariantValue>> properties)
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}",
                    member: "UpdateUnsaved");
                WriteType_aesaesv(ref writer, properties);
                return writer.CreateMessage();
            }
        }
        public Task DeleteAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Delete");
                return writer.CreateMessage();
            }
        }
        public Task<Dictionary<string, Dictionary<string, VariantValue>>> GetSettingsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesaesv(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "GetSettings");
                return writer.CreateMessage();
            }
        }
        public Task<Dictionary<string, Dictionary<string, VariantValue>>> GetSecretsAsync(string settingName)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesaesv(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "s",
                    member: "GetSecrets");
                writer.WriteString(settingName);
                return writer.CreateMessage();
            }
        }
        public Task ClearSecretsAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "ClearSecrets");
                return writer.CreateMessage();
            }
        }
        public Task SaveAsync()
        {
            return this.Connection.CallMethodAsync(CreateMessage());
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    member: "Save");
                return writer.CreateMessage();
            }
        }
        public Task<Dictionary<string, VariantValue>> Update2Async(Dictionary<string, Dictionary<string, VariantValue>> settings, uint flags, Dictionary<string, VariantValue> args)
        {
            return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (NetworkManagerObject)s!), this);
            MessageBuffer CreateMessage()
            {
                var writer = this.Connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: Service.Destination,
                    path: Path,
                    @interface: __Interface,
                    signature: "a{sa{sv}}ua{sv}",
                    member: "Update2");
                WriteType_aesaesv(ref writer, settings);
                writer.WriteUInt32(flags);
                writer.WriteDictionary(args);
                return writer.CreateMessage();
            }
        }
        public ValueTask<IDisposable> WatchUpdatedAsync(Action<Exception?> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Updated", handler, emitOnCapturedContext, flags);
        public ValueTask<IDisposable> WatchRemovedAsync(Action<Exception?> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
            => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Removed", handler, emitOnCapturedContext, flags);
        public Task<bool> GetUnsavedAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Unsaved"), (Message m, object? s) => ReadMessage_v_b(m, (NetworkManagerObject)s!), this);
        public Task<uint> GetFlagsAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Flags"), (Message m, object? s) => ReadMessage_v_u(m, (NetworkManagerObject)s!), this);
        public Task<string> GetFilenameAsync()
            => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Filename"), (Message m, object? s) => ReadMessage_v_s(m, (NetworkManagerObject)s!), this);
        public Task<ConnectionProperties> GetPropertiesAsync()
        {
            return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), this);
            static ConnectionProperties ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                return ReadProperties(ref reader);
            }
        }
        public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ConnectionProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        {
            return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (NetworkManagerObject)s!), handler, emitOnCapturedContext, flags);
            static PropertyChanges<ConnectionProperties> ReadMessage(Message message, NetworkManagerObject _)
            {
                var reader = message.GetBodyReader();
                reader.ReadString(); // interface
                List<string> changed = new();
                return new PropertyChanges<ConnectionProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                        case "Unsaved": invalidated.Add("Unsaved"); break;
                        case "Flags": invalidated.Add("Flags"); break;
                        case "Filename": invalidated.Add("Filename"); break;
                    }
                }
                return invalidated?.ToArray() ?? Array.Empty<string>();
            }
        }
        private static ConnectionProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
        {
            var props = new ConnectionProperties();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                var property = reader.ReadString();
                switch (property)
                {
                    case "Unsaved":
                        reader.ReadSignature("b"u8);
                        props.Unsaved = reader.ReadBool();
                        changedList?.Add("Unsaved");
                        break;
                    case "Flags":
                        reader.ReadSignature("u"u8);
                        props.Flags = reader.ReadUInt32();
                        changedList?.Add("Flags");
                        break;
                    case "Filename":
                        reader.ReadSignature("s"u8);
                        props.Filename = reader.ReadString();
                        changedList?.Add("Filename");
                        break;
                    default:
                        reader.ReadVariantValue();
                        break;
                }
            }
            return props;
        }
    }
    partial class NetworkManagerService
    {
        public Tmds.DBus.Protocol.Connection Connection { get; }
        public string Destination { get; }
        public NetworkManagerService(Tmds.DBus.Protocol.Connection connection, string destination)
            => (Connection, Destination) = (connection, destination);
        public ObjectManager CreateObjectManager(ObjectPath path) => new ObjectManager(this, path);
        public NetworkManager CreateNetworkManager(ObjectPath path) => new NetworkManager(this, path);
        public Active CreateActive(ObjectPath path) => new Active(this, path);
        public IP4Config CreateIP4Config(ObjectPath path) => new IP4Config(this, path);
        public Statistics CreateStatistics(ObjectPath path) => new Statistics(this, path);
        public Device CreateDevice(ObjectPath path) => new Device(this, path);
        public Wired CreateWired(ObjectPath path) => new Wired(this, path);
        public Wireless CreateWireless(ObjectPath path) => new Wireless(this, path);
        public Loopback CreateLoopback(ObjectPath path) => new Loopback(this, path);
        public WifiP2P CreateWifiP2P(ObjectPath path) => new WifiP2P(this, path);
        public AgentManager CreateAgentManager(ObjectPath path) => new AgentManager(this, path);
        public DHCP4Config CreateDHCP4Config(ObjectPath path) => new DHCP4Config(this, path);
        public DnsManager CreateDnsManager(ObjectPath path) => new DnsManager(this, path);
        public AccessPoint CreateAccessPoint(ObjectPath path) => new AccessPoint(this, path);
        public IP6Config CreateIP6Config(ObjectPath path) => new IP6Config(this, path);
        public Settings CreateSettings(ObjectPath path) => new Settings(this, path);
        public Connection CreateConnection(ObjectPath path) => new Connection(this, path);
    }
    class NetworkManagerObject
    {
        public NetworkManagerService Service { get; }
        public ObjectPath Path { get; }
        protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;
        protected NetworkManagerObject(NetworkManagerService service, ObjectPath path)
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
        protected static Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>> ReadMessage_aeoaesaesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_aeoaesaesv(ref reader);
        }
        protected static (ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>) ReadMessage_oaesaesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = ReadType_aesaesv(ref reader);
            return (arg0, arg1);
        }
        protected static (ObjectPath, string[]) ReadMessage_oas(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadArrayOfString();
            return (arg0, arg1);
        }
        protected static ObjectPath[] ReadMessage_ao(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadArrayOfObjectPath();
        }
        protected static ObjectPath ReadMessage_o(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadObjectPath();
        }
        protected static (ObjectPath, ObjectPath) ReadMessage_oo(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadObjectPath();
            return (arg0, arg1);
        }
        protected static (ObjectPath, ObjectPath, Dictionary<string, VariantValue>) ReadMessage_ooaesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadObjectPath();
            var arg2 = reader.ReadDictionaryOfStringToVariantValue();
            return (arg0, arg1, arg2);
        }
        protected static Dictionary<string, string> ReadMessage_aess(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_aess(ref reader);
        }
        protected static (string, string) ReadMessage_ss(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadString();
            var arg1 = reader.ReadString();
            return (arg0, arg1);
        }
        protected static uint ReadMessage_u(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadUInt32();
        }
        protected static Dictionary<string, uint> ReadMessage_aesu(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_aesu(ref reader);
        }
        protected static ObjectPath[] ReadMessage_v_ao(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("ao"u8);
            return reader.ReadArrayOfObjectPath();
        }
        protected static bool ReadMessage_v_b(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("b"u8);
            return reader.ReadBool();
        }
        protected static uint ReadMessage_v_u(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("u"u8);
            return reader.ReadUInt32();
        }
        protected static ObjectPath ReadMessage_v_o(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("o"u8);
            return reader.ReadObjectPath();
        }
        protected static string ReadMessage_v_s(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("s"u8);
            return reader.ReadString();
        }
        protected static uint[] ReadMessage_v_au(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("au"u8);
            return reader.ReadArrayOfUInt32();
        }
        protected static Dictionary<string, VariantValue> ReadMessage_v_aesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("a{sv}"u8);
            return reader.ReadDictionaryOfStringToVariantValue();
        }
        protected static (uint, uint) ReadMessage_uu(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadUInt32();
            return (arg0, arg1);
        }
        protected static uint[][] ReadMessage_v_aau(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("aau"u8);
            return ReadType_aau(ref reader);
        }
        protected static Dictionary<string, VariantValue>[] ReadMessage_v_aaesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("aa{sv}"u8);
            return ReadType_aaesv(ref reader);
        }
        protected static string[] ReadMessage_v_as(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("as"u8);
            return reader.ReadArrayOfString();
        }
        protected static int ReadMessage_v_i(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("i"u8);
            return reader.ReadInt32();
        }
        protected static ulong ReadMessage_v_t(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("t"u8);
            return reader.ReadUInt64();
        }
        protected static (Dictionary<string, Dictionary<string, VariantValue>>, ulong) ReadMessage_aesaesvt(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = ReadType_aesaesv(ref reader);
            var arg1 = reader.ReadUInt64();
            return (arg0, arg1);
        }
        protected static (uint, uint, uint) ReadMessage_uuu(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadUInt32();
            var arg2 = reader.ReadUInt32();
            return (arg0, arg1, arg2);
        }
        protected static (uint, uint) ReadMessage_v_ruuz(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("(uu)"u8);
            return ReadType_ruuz(ref reader);
        }
        protected static long ReadMessage_v_x(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("x"u8);
            return reader.ReadInt64();
        }
        protected static byte[] ReadMessage_v_ay(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("ay"u8);
            return reader.ReadArrayOfByte();
        }
        protected static byte ReadMessage_v_y(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("y"u8);
            return reader.ReadByte();
        }
        protected static (byte[], uint, byte[])[] ReadMessage_v_arayuayz(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("a(ayuay)"u8);
            return ReadType_arayuayz(ref reader);
        }
        protected static (byte[], uint, byte[], uint)[] ReadMessage_v_arayuayuz(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("a(ayuayu)"u8);
            return ReadType_arayuayuz(ref reader);
        }
        protected static byte[][] ReadMessage_v_aay(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadSignature("aay"u8);
            return ReadType_aay(ref reader);
        }
        protected static (ObjectPath, Dictionary<string, VariantValue>) ReadMessage_oaesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadObjectPath();
            var arg1 = reader.ReadDictionaryOfStringToVariantValue();
            return (arg0, arg1);
        }
        protected static (bool, string[]) ReadMessage_bas(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadBool();
            var arg1 = reader.ReadArrayOfString();
            return (arg0, arg1);
        }
        protected static bool ReadMessage_b(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadBool();
        }
        protected static Dictionary<string, Dictionary<string, VariantValue>> ReadMessage_aesaesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return ReadType_aesaesv(ref reader);
        }
        protected static Dictionary<string, VariantValue> ReadMessage_aesv(Message message, NetworkManagerObject _)
        {
            var reader = message.GetBodyReader();
            return reader.ReadDictionaryOfStringToVariantValue();
        }
        protected static uint[][] ReadType_aau(ref Reader reader)
        {
            List<uint[]> list = new();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Array);
            while (reader.HasNext(arrayEnd))
            {
                list.Add(reader.ReadArrayOfUInt32());
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
        protected static (uint, uint) ReadType_ruuz(ref Reader reader)
        {
            return (reader.ReadUInt32(), reader.ReadUInt32());
        }
        protected static (byte[], uint, byte[])[] ReadType_arayuayz(ref Reader reader)
        {
            List<(byte[], uint, byte[])> list = new();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                list.Add(ReadType_rayuayz(ref reader));
            }
            return list.ToArray();
        }
        protected static (byte[], uint, byte[]) ReadType_rayuayz(ref Reader reader)
        {
            return (reader.ReadArrayOfByte(), reader.ReadUInt32(), reader.ReadArrayOfByte());
        }
        protected static (byte[], uint, byte[], uint)[] ReadType_arayuayuz(ref Reader reader)
        {
            List<(byte[], uint, byte[], uint)> list = new();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
            while (reader.HasNext(arrayEnd))
            {
                list.Add(ReadType_rayuayuz(ref reader));
            }
            return list.ToArray();
        }
        protected static (byte[], uint, byte[], uint) ReadType_rayuayuz(ref Reader reader)
        {
            return (reader.ReadArrayOfByte(), reader.ReadUInt32(), reader.ReadArrayOfByte(), reader.ReadUInt32());
        }
        protected static byte[][] ReadType_aay(ref Reader reader)
        {
            List<byte[]> list = new();
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Array);
            while (reader.HasNext(arrayEnd))
            {
                list.Add(reader.ReadArrayOfByte());
            }
            return list.ToArray();
        }
        protected static Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>> ReadType_aeoaesaesv(ref Reader reader)
        {
            Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>> dictionary = new();
            ArrayEnd dictEnd = reader.ReadDictionaryStart();
            while (reader.HasNext(dictEnd))
            {
                var key = reader.ReadObjectPath();
                var value = ReadType_aesaesv(ref reader);
                dictionary[key] = value;
            }
            return dictionary;
        }
        protected static Dictionary<string, Dictionary<string, VariantValue>> ReadType_aesaesv(ref Reader reader)
        {
            Dictionary<string, Dictionary<string, VariantValue>> dictionary = new();
            ArrayEnd dictEnd = reader.ReadDictionaryStart();
            while (reader.HasNext(dictEnd))
            {
                var key = reader.ReadString();
                var value = reader.ReadDictionaryOfStringToVariantValue();
                dictionary[key] = value;
            }
            return dictionary;
        }
        protected static Dictionary<string, string> ReadType_aess(ref Reader reader)
        {
            Dictionary<string, string> dictionary = new();
            ArrayEnd dictEnd = reader.ReadDictionaryStart();
            while (reader.HasNext(dictEnd))
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                dictionary[key] = value;
            }
            return dictionary;
        }
        protected static Dictionary<string, uint> ReadType_aesu(ref Reader reader)
        {
            Dictionary<string, uint> dictionary = new();
            ArrayEnd dictEnd = reader.ReadDictionaryStart();
            while (reader.HasNext(dictEnd))
            {
                var key = reader.ReadString();
                var value = reader.ReadUInt32();
                dictionary[key] = value;
            }
            return dictionary;
        }
        protected static void WriteType_aesaesv(ref MessageWriter writer, Dictionary<string, Dictionary<string, VariantValue>> value)
        {
            ArrayStart arrayStart = writer.WriteDictionaryStart();
            foreach (var item in value)
            {
                writer.WriteDictionaryEntryStart();
                writer.WriteString(item.Key);
                writer.WriteDictionary(item.Value);
            }
            writer.WriteDictionaryEnd(arrayStart);
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

    enum NetworkManagerState : uint
    {
        Unknown = 0,
        ASleep = 10,
        Disconnected = 20,
        Disconnecting = 30,
        Connecting = 40,
        ConnectedLocal = 50,
        ConnectedSite = 60,
        ConnectedGlobal = 70
    }
    enum NetworkManagerConnectivity : uint
    {
        Unknown = 0,
        None = 1,
        Portal = 2,
        Limited = 3,
        Full = 4
    }
    enum DeviceState : uint
    {
        Unknown = 0,
        Unmanaged = 10,
        Unavailable = 20,
        Disconnected = 30,
        Prepare = 40,
        Config = 50,
        NeedAuth = 60,
        IpConfig = 70,
        IpCheck = 80,
        Secondaries = 90,
        Activated = 100,
        Deactivating = 110,
        Failed = 120
    }
}

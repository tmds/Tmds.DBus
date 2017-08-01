// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    public class Connection : IConnection
    {
        public const string DynamicAssemblyName = "Tmds.DBus.Emit";

        private class ProxyFactory : IProxyFactory
        {
            public Connection Connection { get; }
            public ProxyFactory(Connection connection)
            {
                Connection = connection;
            }
            public T CreateProxy<T>(string serviceName, ObjectPath path)
            {
                return Connection.CreateProxy<T>(serviceName, path);
            }
        }

        private readonly object _gate = new object();
        private readonly Dictionary<ObjectPath, DBusAdapter> _registeredObjects = new Dictionary<ObjectPath, DBusAdapter>();
        private readonly string _address;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly bool _autoConnect;

        private ConnectionState _state = ConnectionState.Created;
        private bool _disposed = false;
        private IProxyFactory _factory;
        private IDBusConnection _dbusConnection;
        private Task<IDBusConnection> _dbusConnectionTask;
        private TaskCompletionSource<IDBusConnection> _dbusConnectionTcs;
        private CancellationTokenSource _connectCts;
        private Exception _disconnectReason;
        private IDBus _bus;
        private bool? _remoteIsBus;
        private string _localName;

        private IDBus DBus
        {
            get
            {
                if (_bus != null)
                {
                    return _bus;
                }
                lock (_gate)
                {
                    _bus = _bus ?? CreateProxy<IDBus>(DBusConnection.DBusServiceName, DBusConnection.DBusObjectPath);
                    return _bus;
                }
            }
        }

        public string LocalName => _localName;
        public bool? RemoteIsBus => _remoteIsBus;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        public Connection(string address) :
            this(address, new ConnectionOptions())
        { }

        public Connection(string address, ConnectionOptions connectionOptions)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (connectionOptions == null)
                throw new ArgumentNullException(nameof(connectionOptions));

            _address = address;
            _factory = new ProxyFactory(this);
            _synchronizationContext = connectionOptions.SynchronizationContext;
            _autoConnect = connectionOptions.AutoConnect;
        }

        public Task ConnectAsync()
            => DoConnectAsync();

        private async Task<IDBusConnection> DoConnectAsync()
        {
            Task<IDBusConnection> connectionTask = null;
            bool alreadyConnecting = false;
            lock (_gate)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(typeof(Connection).FullName); 
                }

                if (!_autoConnect)
                {
                    if (_state != ConnectionState.Created)
                    {
                        throw new InvalidOperationException("Can only connect once");
                    }
                }
                else
                {
                    if (_state == ConnectionState.Connecting || _state == ConnectionState.Connected)
                    {
                        connectionTask = _dbusConnectionTask;
                        alreadyConnecting = true;
                    }
                }
                if (!alreadyConnecting)
                {
                    var previousState = _state;
                    _localName = null;
                    _remoteIsBus = null;
                    _connectCts = new CancellationTokenSource();
                    _dbusConnectionTcs = new TaskCompletionSource<IDBusConnection>();
                    _dbusConnectionTask = _dbusConnectionTcs.Task;
                    connectionTask = _dbusConnectionTask;
                    _state = ConnectionState.Connecting;

                    var connectingEvent = CreateConnectionStateChangedEvent(previousState);
                    _disconnectReason = null;
                    EmitConnectionStateChanged(connectingEvent);
                }
            }

            if (alreadyConnecting)
            {
                return await connectionTask;
            }

            IDBusConnection connection;
            try
            {
                connection = await DBusConnection.OpenAsync(_address, OnDisconnect, _connectCts.Token);
            }
            catch (ConnectException ce)
            {
                Disconnect(dispose: false, exception: ce);
                throw;
            }
            catch (Exception e)
            {
                var ce = new ConnectException(e.Message, e);
                Disconnect(dispose: false, exception: ce);
                throw ce;
            }
            lock (_gate)
            {
                if (_state == ConnectionState.Connecting)
                {
                    var previousState = _state;
                    _localName = connection.LocalName;
                    _remoteIsBus = connection.RemoteIsBus;
                    _dbusConnection = connection;
                    _connectCts.Dispose();
                    _connectCts = null;
                    _state = ConnectionState.Connected;
                    _dbusConnectionTcs.SetResult(connection);
                    _dbusConnectionTcs = null;

                    var connectedEvent = CreateConnectionStateChangedEvent(previousState);
                    connectedEvent.RemoteIsBus = _dbusConnection.RemoteIsBus == true;
                    connectedEvent.LocalName = _dbusConnection.LocalName;
                    EmitConnectionStateChanged(connectedEvent);
                }
                else
                {
                    connection.Dispose();
                }
                ThrowIfNotConnected();
            }
            return connection;
        }

        public void Dispose()
        {
            Disconnect(dispose: true, exception: new ObjectDisposedException(typeof(Connection).FullName));
        }

        public T CreateProxy<T>(string busName, ObjectPath path)
        {
            return (T)CreateProxy(typeof(T), busName, path);
        }

        public async Task<bool> UnregisterServiceAsync(string serviceName)
        {
            ThrowIfAutoConnect();
            var connection = GetConnectedConnection();
            var reply = await connection.ReleaseNameAsync(serviceName);
            return reply == ReleaseNameReply.ReplyReleased;
        }

        public async Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default)
        {
            ThrowIfAutoConnect();
            var connection = GetConnectedConnection();
            if (!options.HasFlag(ServiceRegistrationOptions.AllowReplacement) && (onLost != null))
            {
                throw new ArgumentException($"{nameof(onLost)} can only be set when {nameof(ServiceRegistrationOptions.AllowReplacement)} is also set", nameof(onLost));
            }

            RequestNameOptions requestOptions = RequestNameOptions.None;
            if (options.HasFlag(ServiceRegistrationOptions.ReplaceExisting))
            {
                requestOptions |= RequestNameOptions.ReplaceExisting;
            }
            if (options.HasFlag(ServiceRegistrationOptions.AllowReplacement))
            {
                requestOptions |= RequestNameOptions.AllowReplacement;
            }
            var reply = await connection.RequestNameAsync(serviceName, requestOptions, onAquired, onLost, CaptureSynchronizationContext());
            switch (reply)
            {
                case RequestNameReply.PrimaryOwner:
                case RequestNameReply.InQueue:
                    return;
                case RequestNameReply.Exists:
                case RequestNameReply.AlreadyOwner:
                default:
                    throw new ProtocolException("Unexpected reply");
            }
        }

        public async Task RegisterServiceAsync(string name, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default)
        {
            ThrowIfAutoConnect();
            var connection = GetConnectedConnection();
            if (!options.HasFlag(ServiceRegistrationOptions.AllowReplacement) && (onLost != null))
            {
                throw new ArgumentException($"{nameof(onLost)} can only be set when {nameof(ServiceRegistrationOptions.AllowReplacement)} is also set", nameof(onLost));
            }

            RequestNameOptions requestOptions = RequestNameOptions.DoNotQueue;
            if (options.HasFlag(ServiceRegistrationOptions.ReplaceExisting))
            {
                requestOptions |= RequestNameOptions.ReplaceExisting;
            }
            if (options.HasFlag(ServiceRegistrationOptions.AllowReplacement))
            {
                requestOptions |= RequestNameOptions.AllowReplacement;
            }
            var reply = await connection.RequestNameAsync(name, requestOptions, null, onLost, CaptureSynchronizationContext());
            switch (reply)
            {
                case RequestNameReply.PrimaryOwner:
                    return;
                case RequestNameReply.Exists:
                    throw new InvalidOperationException("Service is registered by another connection");
                case RequestNameReply.AlreadyOwner:
                    throw new InvalidOperationException("Service is already registered by this connection");
                case RequestNameReply.InQueue:
                default:
                    throw new ProtocolException("Unexpected reply");
            }
        }

        public Task RegisterObjectAsync(IDBusObject o)
        {
            return RegisterObjectsAsync(new[] { o });
        }

        public async Task RegisterObjectsAsync(IEnumerable<IDBusObject> objects)
        {
            ThrowIfAutoConnect();
            var connection = GetConnectedConnection();
            var assembly = DynamicAssembly.Instance;
            var registrations = new List<DBusAdapter>();
            foreach (var o in objects)
            {
                var implementationType = assembly.GetExportTypeInfo(o.GetType());
                var objectPath = o.ObjectPath;
                var registration = (DBusAdapter)Activator.CreateInstance(implementationType.AsType(), _dbusConnection, objectPath, o, _factory, CaptureSynchronizationContext());
                registrations.Add(registration);
            }

            lock (_gate)
            {
                connection.AddMethodHandlers(registrations.Select(r => new KeyValuePair<ObjectPath, MethodHandler>(r.Path, r.HandleMethodCall)));

                foreach (var registration in registrations)
                {
                    _registeredObjects.Add(registration.Path, registration);
                }
            }
            try
            {
                foreach (var registration in registrations)
                {
                    await registration.WatchSignalsAsync();
                }
                lock (_gate)
                {
                    foreach (var registration in registrations)
                    {
                        registration.CompleteRegistration();
                    }
                }
            }
            catch
            {
                lock (_gate)
                {
                    foreach (var registration in registrations)
                    {
                        registration.Unregister();
                        _registeredObjects.Remove(registration.Path);
                    }
                    connection.RemoveMethodHandlers(registrations.Select(r => r.Path));
                }
                throw;
            }
        }

        public void UnregisterObject(ObjectPath objectPath)
        {
            UnregisterObjects(new[] { objectPath });
        }

        public void UnregisterObjects(IEnumerable<ObjectPath> paths)
        {
            ThrowIfAutoConnect();
            lock (_gate)
            {
                var connection = GetConnectedConnection();

                foreach(var objectPath in paths)
                {
                    DBusAdapter registration;
                    if (_registeredObjects.TryGetValue(objectPath, out registration))
                    {
                        registration.Unregister();
                        _registeredObjects.Remove(objectPath);
                    }
                }
                
                connection.RemoveMethodHandlers(paths);
            }
        }

        public Task<string[]> ListActivatableServicesAsync()
            => DBus.ListActivatableNamesAsync();

        public async Task<string> ResolveServiceOwnerAsync(string serviceName)
        {
            try
            {
                return await DBus.GetNameOwnerAsync(serviceName);
            }
            catch (DBusException e) when (e.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner")
            {
                return null;
            }
            catch
            {
                throw;
            }
        }

        public Task<ServiceStartResult> ActivateServiceAsync(string serviceName)
            => DBus.StartServiceByNameAsync(serviceName, 0);

        public Task<bool> IsServiceActiveAsync(string serviceName)
            => DBus.NameHasOwnerAsync(serviceName);

        public async Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, Action<Exception> onError = null)
        {
            if (serviceName == "*")
            {
                serviceName = ".*";
            }

            var synchronizationContext = CaptureSynchronizationContext();
            var wrappedDisposable = new WrappedDisposable(synchronizationContext);
            bool namespaceLookup = serviceName.EndsWith(".*");
            bool _eventEmitted = false;
            var _gate = new object();
            var _emittedServices = namespaceLookup ? new List<string>() : null;

            Action<ServiceOwnerChangedEventArgs, Exception> handleEvent = (ownerChange, ex) => {
                if (ex != null)
                {
                    if (onError == null)
                    {
                        return;
                    }
                    wrappedDisposable.Call(onError, ex, disposes: true);
                    return;
                }
                bool first = false;
                lock (_gate)
                {
                    if (namespaceLookup)
                    {
                        first = _emittedServices?.Contains(ownerChange.ServiceName) == false;
                        _emittedServices?.Add(ownerChange.ServiceName);
                    }
                    else
                    {
                        first = _eventEmitted == false;
                        _eventEmitted = true;
                    }
                }
                if (first)
                {
                    if (ownerChange.NewOwner == null)
                    {
                        return;
                    }
                    ownerChange.OldOwner = null;
                }
                wrappedDisposable.Call(handler, ownerChange);
            };

            var connection = await GetConnectionTask();
            wrappedDisposable.Disposable = await connection.WatchNameOwnerChangedAsync(serviceName, handleEvent).ConfigureAwait(false);
            if (namespaceLookup)
            {
                serviceName = serviceName.Substring(0, serviceName.Length - 2);
            }
            try
            {
                if (namespaceLookup)
                {
                    var services = await ListServicesAsync();
                    foreach (var service in services)
                    {
                        if (service.StartsWith(serviceName)
                         && (   (service.Length == serviceName.Length)
                             || (service[serviceName.Length] == '.')
                             || (serviceName.Length == 0 && service[0] != ':')))
                        {
                            var currentName = await ResolveServiceOwnerAsync(service);
                            lock (_gate)
                            {
                                if (currentName != null && !_emittedServices.Contains(serviceName))
                                {
                                    var e = new ServiceOwnerChangedEventArgs(service, null, currentName);
                                    handleEvent(e, null);
                                }
                            }
                        }
                    }
                    lock (_gate)
                    {
                        _emittedServices = null;
                    }
                }
                else
                {
                    var currentName = await ResolveServiceOwnerAsync(serviceName);
                    lock (_gate)
                    {
                        if (currentName != null && !_eventEmitted)
                        {
                            var e = new ServiceOwnerChangedEventArgs(serviceName, null, currentName);
                            handleEvent(e, null);
                        }
                    }
                }
                return wrappedDisposable;
            }
            catch (Exception ex)
            {
                handleEvent(default(ServiceOwnerChangedEventArgs), ex);
            }

            return wrappedDisposable;
        }

        public Task<string[]> ListServicesAsync()
            => DBus.ListNamesAsync();

        // Used by tests
        internal void Connect(IDBusConnection dbusConnection)
        {
            lock (_gate)
            {
                if (_state != ConnectionState.Created)
                {
                    throw new InvalidOperationException("Can only connect once");
                }
                _dbusConnection = dbusConnection;
                _dbusConnectionTask = Task.FromResult(_dbusConnection);
                _state = ConnectionState.Connected;
            }
        }

        private object CreateProxy(Type interfaceType, string busName, ObjectPath path)
        {
            var assembly = DynamicAssembly.Instance;
            var implementationType = assembly.GetProxyTypeInfo(interfaceType);

            DBusObjectProxy instance = (DBusObjectProxy)Activator.CreateInstance(implementationType.AsType(),
                new object[] { this, _factory, busName, path });

            return instance;
        }

        private void OnDisconnect(Exception e)
        {
            Disconnect(dispose: false, exception: e);
        }

        private void ThrowIfNotConnected()
            => ThrowIfNotConnected(_disposed, _state, _disconnectReason);

        internal static void ThrowIfNotConnected(bool disposed, ConnectionState state, Exception disconnectReason)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(Connection).FullName);
            }
            if (state == ConnectionState.Disconnected)
            {
                throw new DisconnectedException(disconnectReason);
            }
            else if (state == ConnectionState.Created)
            {
                throw new InvalidOperationException("Not Connected");
            }
            else if (state == ConnectionState.Connecting)
            {
                throw new InvalidOperationException("Connecting");
            }
        }

        internal static void ThrowIfNotConnecting(bool disposed, ConnectionState state, Exception disconnectReason)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(typeof(Connection).FullName);
            }
            if (state == ConnectionState.Disconnected)
            {
                throw new DisconnectedException(disconnectReason);
            }
            else if (state == ConnectionState.Created)
            {
                throw new InvalidOperationException("Not Connected");
            }
            else if (state == ConnectionState.Connected)
            {
                throw new InvalidOperationException("Already Connected");
            }
        }

        private Task<IDBusConnection> GetConnectionTask()
        {
            var connectionTask = Volatile.Read(ref _dbusConnectionTask);
            if (connectionTask != null)
            {
                return connectionTask;
            }
            if (!_autoConnect)
            {
                return Task.FromResult(GetConnectedConnection());
            }
            else
            {
                return DoConnectAsync();
            }
        }

        private IDBusConnection GetConnectedConnection()
        {
            var connection = Volatile.Read(ref _dbusConnection);
            if (connection != null)
            {
                return connection;
            }
            lock (_gate)
            {
                ThrowIfNotConnected();
                return _dbusConnection;
            }
        }

        private void ThrowIfAutoConnect()
        {
            if (_autoConnect == true)
            {
                throw new InvalidOperationException($"Operation not supported for {nameof(ConnectionOptions.AutoConnect)} Connection.");
            }
        }

        private void Disconnect(bool dispose, Exception exception)
        {
            lock (_gate)
            {
                if (dispose)
                {
                    _disposed = true;
                }
                var previousState = _state;
                if (previousState == ConnectionState.Disconnecting || previousState == ConnectionState.Disconnected || previousState == ConnectionState.Created)
                {
                    return;
                }

                _disconnectReason = exception;

                var connection = _dbusConnection;
                var connectionCts = _connectCts;;
                var dbusConnectionTask = _dbusConnectionTask;
                var dbusConnectionTcs = _dbusConnectionTcs;
                _dbusConnection = null;
                _connectCts = null;
                _dbusConnectionTask = null;
                _dbusConnectionTcs = null;

                foreach (var registeredObject in _registeredObjects)
                {
                    registeredObject.Value.Unregister();
                }
                _registeredObjects.Clear();

                _state = ConnectionState.Disconnecting;
                var disconnectingEvent = CreateConnectionStateChangedEvent(previousState);
                EmitConnectionStateChanged(disconnectingEvent);

                connectionCts?.Cancel();
                connectionCts?.Dispose();
                dbusConnectionTcs?.SetException(
                    dispose ? (Exception)new ObjectDisposedException(typeof(Connection).FullName) : 
                    exception.GetType() == typeof(ConnectException) ? exception :
                    new DisconnectedException(exception));
                connection?.Disconnect(dispose, exception);

                if (_state == ConnectionState.Disconnecting)
                {
                    previousState = _state;
                    _state = ConnectionState.Disconnected;                    
                    var disconnectEvent = CreateConnectionStateChangedEvent(previousState);
                    EmitConnectionStateChanged(disconnectEvent);
                }
            }
        }

        private void EmitConnectionStateChanged(ConnectionStateChangedEventArgs stateChangeEvent)
        {
            if (_synchronizationContext != null && SynchronizationContext.Current != _synchronizationContext)
            {
                if (StateChanged != null)
                {
                    _synchronizationContext.Post(_ => StateChanged?.Invoke(this, stateChangeEvent), null);
                }
            }
            else
            {
                StateChanged?.Invoke(this, stateChangeEvent);
            }
        }

        internal async Task<Message> CallMethodAsync(Message message)
        {
            var connection = await GetConnectionTask();
            try
            {
                return await connection.CallMethodAsync(message);
            }
            catch (DisconnectedException) when (_autoConnect)
            {
                connection = await GetConnectionTask();
                return await connection.CallMethodAsync(message);
            }
        }

        internal async Task<IDisposable> WatchSignalAsync(ObjectPath path, string @interface, string signalName, SignalHandler handler)
        {
            var connection = await GetConnectionTask();
            try
            {
                return await connection.WatchSignalAsync(path, @interface, signalName, handler);
            }
            catch (DisconnectedException) when (_autoConnect)
            {
                connection = await GetConnectionTask();
                return await connection.WatchSignalAsync(path, @interface, signalName, handler);
            }
        }

        private ConnectionStateChangedEventArgs CreateConnectionStateChangedEvent(ConnectionState previousState)
        {
            var disconnectReason = _disconnectReason;
            if (disconnectReason != null
             && disconnectReason.GetType() != typeof(ConnectException)
             && disconnectReason.GetType() != typeof(ObjectDisposedException)
             && disconnectReason.GetType() != typeof(DisconnectedException))
            {
                disconnectReason = new DisconnectedException(disconnectReason?.Message, disconnectReason);
            }
            return new ConnectionStateChangedEventArgs(previousState, _state, disconnectReason);
        }

        internal SynchronizationContext CaptureSynchronizationContext() => _synchronizationContext;
    }
}

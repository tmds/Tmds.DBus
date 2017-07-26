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

        private enum State
        {
            Created,
            Connecting,
            Connected,
            Disconnected,
            Disposed
        }

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

        private State _state = State.Created;
        private IProxyFactory _factory;
        private IDBusConnection _dbusConnection;
        private Action<Exception> _onDisconnect;
        private SynchronizationContext _onDisconnectSynchronizationContext;
        private Exception _disconnectReason;
        private IDBus _bus;

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

        public string LocalName => _dbusConnection?.LocalName;
        public bool? RemoteIsBus => _dbusConnection?.RemoteIsBus;

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
        }

        public async Task ConnectAsync(Action<Exception> onDisconnect = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_gate)
            {
                if (_state != State.Created)
                {
                    throw new InvalidOperationException("Can only connect once");
                }
                _state = State.Connecting;
            }
            try
            {
                if (onDisconnect != null)
                {
                    _onDisconnectSynchronizationContext = CaptureSynchronizationContext();
                }

                _dbusConnection = await DBusConnection.OpenAsync(_address, OnDisconnect, cancellationToken);

                lock (_gate)
                {
                    if (_state == State.Connecting)
                    {
                        _state = State.Connected;
                    }
                    ThrowIfNotConnected();

                    _onDisconnect = onDisconnect;
                }
            }
            catch (Exception e)
            {
                DoDisconnect(State.Disconnected, e);
                throw;
            }
        }

        public void Dispose()
        {
            DoDisconnect(State.Disposed, null);
        }

        public T CreateProxy<T>(string busName, ObjectPath path)
        {
            return (T)CreateProxy(typeof(T), busName, path);
        }

        public async Task<bool> UnregisterServiceAsync(string serviceName)
        {
            ThrowIfNotConnected();
            var reply = await _dbusConnection.ReleaseNameAsync(serviceName);
            return reply == ReleaseNameReply.ReplyReleased;
        }

        public async Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default)
        {
            ThrowIfNotConnected();
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
            var reply = await _dbusConnection.RequestNameAsync(serviceName, requestOptions, onAquired, onLost, CaptureSynchronizationContext());
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
            ThrowIfNotConnected();
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
            var reply = await _dbusConnection.RequestNameAsync(name, requestOptions, null, onLost, CaptureSynchronizationContext());
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
            ThrowIfNotConnected();
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
                ThrowIfNotConnected();

                _dbusConnection.AddMethodHandlers(registrations.Select(r => new KeyValuePair<ObjectPath, MethodHandler>(r.Path, r.HandleMethodCall)));

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
                    ThrowIfNotConnected();

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
                    _dbusConnection.RemoveMethodHandlers(registrations.Select(r => r.Path));
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
            lock (_gate)
            {
                ThrowIfNotConnected();

                foreach(var objectPath in paths)
                {
                    DBusAdapter registration;
                    if (_registeredObjects.TryGetValue(objectPath, out registration))
                    {
                        registration.Unregister();
                        _registeredObjects.Remove(objectPath);
                    }
                }
                
                _dbusConnection.RemoveMethodHandlers(paths);
            }
        }

        public Task<string[]> ListActivatableServicesAsync()
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            return DBus.ListActivatableNamesAsync();
        }

        public async Task<string> ResolveServiceOwnerAsync(string serviceName)
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
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
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            return DBus.StartServiceByNameAsync(serviceName, 0);
        }

        public Task<bool> IsServiceActiveAsync(string serviceName)
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            return DBus.NameHasOwnerAsync(serviceName);
        }

        public async Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, Action<Exception> onError = null)
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
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

            wrappedDisposable.Disposable = await _dbusConnection.WatchNameOwnerChangedAsync(serviceName, handleEvent).ConfigureAwait(false);
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
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();

            return DBus.ListNamesAsync();
        }

        // Used by tests
        internal void Connect(IDBusConnection dbusConnection)
        {
            lock (_gate)
            {
                if (_state != State.Created)
                {
                    throw new InvalidOperationException("Can only connect once");
                }
                _dbusConnection = dbusConnection;
                _state = State.Connected;
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
            if (e != null)
            {
                DoDisconnect(State.Disconnected, e);
            }
            if (_onDisconnect != null)
            {
                if ((_onDisconnectSynchronizationContext != null)
                    && (SynchronizationContext.Current != _onDisconnectSynchronizationContext))
                {
                    _onDisconnectSynchronizationContext.Post(_ => _onDisconnect(_disconnectReason), null);
                }
                else
                {
                    _onDisconnect(_disconnectReason);
                }
            }
        }

        private void ThrowIfNotConnected()
        {
            if (_state == State.Disconnected)
            {
                throw new DisconnectedException(_disconnectReason);
            }
            else if (_state == State.Created)
            {
                throw new InvalidOperationException("Not Connected");
            }
            else if (_state == State.Connecting)
            {
                throw new InvalidOperationException("Connecting");
            }
            else if (_state == State.Disposed)
            {
                throw new ObjectDisposedException(typeof(Connection).FullName);
            }
        }

        private IDBusConnection GetConnectedConnection()
        {
            var connection = Volatile.Read(ref _dbusConnection);
            if (connection == null)
            {
                throw new InvalidOperationException("Not Connected");
            }
            return connection;
        }

        private void ThrowIfRemoteIsNotBus()
        {
            if (_dbusConnection.RemoteIsBus != true)
            {
                throw new InvalidOperationException("The remote peer is not a bus");
            }
        }

        private void DoDisconnect(State nextState, Exception e)
        {
            lock (_gate)
            {
                if ((_state == State.Disconnected) || (_state == State.Disposed))
                {
                    if (nextState == State.Disposed)
                    {
                        _state = nextState;
                    }
                    return;
                }

                _disconnectReason = e;

                _state = nextState;

                foreach (var registeredObject in _registeredObjects)
                {
                    registeredObject.Value.Unregister();
                }
                _registeredObjects.Clear();

                _dbusConnection?.Dispose();
            }
        }

        internal Task<Message> CallMethodAsync(Message message)
        {
            return GetConnectedConnection().CallMethodAsync(message);
        }

        internal Task<IDisposable> WatchSignalAsync(ObjectPath path, string @interface, string signalName, SignalHandler handler)
        {
            return GetConnectedConnection().WatchSignalAsync(path, @interface, signalName, handler);
        }

        internal SynchronizationContext CaptureSynchronizationContext() => _synchronizationContext;
    }
}

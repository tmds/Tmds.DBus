// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    /// <summary>
    /// Connection with a D-Bus peer.
    /// </summary>
    public class Connection : IConnection
    {
        /// <summary>
        /// Assembly name where the dynamically generated code resides.
        /// </summary>
        public const string DynamicAssemblyName = "Tmds.DBus.Emit";

        private static Connection s_systemConnection;
        private static Connection s_sessionConnection;
        private static readonly object NoDispose = new object();

        /// <summary>
        /// An AutoConnect Connection to the system bus.
        /// </summary>
        public static Connection System => s_systemConnection ?? CreateSystemConnection();

        /// <summary>
        /// An AutoConnect Connection to the session bus.
        /// </summary>
        public static Connection Session => s_sessionConnection ?? CreateSessionConnection();

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
        private readonly Func<Task<ClientSetupResult>> _connectFunction;
        private readonly Action<object> _disposeAction;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly bool _autoConnect;

        private ConnectionState _state = ConnectionState.Created;
        private bool _disposed = false;
        private IProxyFactory _factory;
        private DBusConnection _dbusConnection;
        private Task<DBusConnection> _dbusConnectionTask;
        private TaskCompletionSource<DBusConnection> _dbusConnectionTcs;
        private CancellationTokenSource _connectCts;
        private Exception _disconnectReason;
        private IDBus _bus;
        private EventHandler<ConnectionStateChangedEventArgs> _stateChangedEvent;
        private object _disposeUserToken = NoDispose;

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

        /// <summary>
        /// Occurs when the state changes.
        /// </summary>
        /// <remarks>
        /// The event handler will be called when it is added to the event.
        /// The event handler is invoked on the ConnectionOptions.SynchronizationContext.
        /// </remarks>
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged
        {
            add  
            {
                lock (_gate)
                {
                    _stateChangedEvent += value;
                    if (_state != ConnectionState.Created)
                    {
                        EmitConnectionStateChanged(value);
                    }
                }
            }
            remove
            {
                lock (_gate)
                {
                    _stateChangedEvent -= value;
                }
            }  
        }

        /// <summary>
        /// Creates a new Connection with a specific address.
        /// </summary>
        /// <param name="address">Address of the D-Bus peer.</param>
        public Connection(string address) :
            this(new DefaultConnectionOptions(address))
        { }

        /// <summary>
        /// Creates a new Connection with specific ConnectionOptions.
        /// </summary>
        /// <param name="connectionOptions"></param>
        public Connection(ConnectionOptions connectionOptions)
        {
            if (connectionOptions == null)
                throw new ArgumentNullException(nameof(connectionOptions));

            _factory = new ProxyFactory(this);
            _synchronizationContext = connectionOptions.SynchronizationContext;
            if (connectionOptions is ClientConnectionOptions clientConnectionOptions)
            {
                _autoConnect = clientConnectionOptions.AutoConnect;
                _connectFunction = clientConnectionOptions.SetupAsync;
                _disposeAction = clientConnectionOptions.Teardown;
            }
            else if (connectionOptions is ServerConnectionOptions serverConnectionOptions)
            {
                _autoConnect = false;
                _state = ConnectionState.Connected;
                _dbusConnection = DBusConnection.CreateForServer(); // TODO
                _dbusConnectionTask = Task.FromResult(_dbusConnection);
                serverConnectionOptions.Connection = this;
            }
            else
            {
                throw new NotSupportedException($"Unknown ConnectionOptions type: '{typeof(ConnectionOptions).FullName}'");
            }
        }

        /// <summary>
        /// Connect with the remote peer.
        /// </summary>
        /// <returns>
        /// Information about the established connection.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        public async Task<ConnectionInfo> ConnectAsync()
            => (await DoConnectAsync()).ConnectionInfo;

        private async Task<DBusConnection> DoConnectAsync()
        {
            Task<DBusConnection> connectionTask = null;
            bool alreadyConnecting = false;
            lock (_gate)
            {
                if (_disposed)
                {
                    ThrowDisposed();
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
                    _connectCts = new CancellationTokenSource();
                    _dbusConnectionTcs = new TaskCompletionSource<DBusConnection>();
                    _dbusConnectionTask = _dbusConnectionTcs.Task;
                    connectionTask = _dbusConnectionTask;
                    _state = ConnectionState.Connecting;

                    EmitConnectionStateChanged();
                }
            }

            if (alreadyConnecting)
            {
                return await connectionTask;
            }

            DBusConnection connection;
            object disposeUserToken = NoDispose;
            try
            {
                ClientSetupResult connectionContext = await _connectFunction();
                disposeUserToken = connectionContext.TeardownToken;
                connection = await DBusConnection.ConnectAsync(connectionContext, OnDisconnect, _connectCts.Token);
            }
            catch (ConnectException ce)
            {
                if (disposeUserToken != NoDispose)
                {
                    _disposeAction?.Invoke(disposeUserToken);
                }
                Disconnect(dispose: false, exception: ce);
                throw;
            }
            catch (Exception e)
            {
                if (disposeUserToken != NoDispose)
                {
                    _disposeAction?.Invoke(disposeUserToken);
                }
                var ce = new ConnectException(e.Message, e);
                Disconnect(dispose: false, exception: ce);
                throw ce;
            }
            lock (_gate)
            {
                if (_state == ConnectionState.Connecting)
                {
                    _disposeUserToken = disposeUserToken;
                    _dbusConnection = connection;
                    _connectCts.Dispose();
                    _connectCts = null;
                    _state = ConnectionState.Connected;
                    _dbusConnectionTcs.SetResult(connection);
                    _dbusConnectionTcs = null;

                    EmitConnectionStateChanged();
                }
                else
                {
                    connection.Dispose();
                    if (disposeUserToken != NoDispose)
                    {
                        _disposeAction?.Invoke(disposeUserToken);
                    }
                }
                ThrowIfNotConnected();
            }
            return connection;
        }

        /// <summary>
        /// Disposes the connection.
        /// </summary>
        public void Dispose()
        {
            Disconnect(dispose: true, exception: CreateDisposedException());
        }

        /// <summary>
        /// Creates a proxy object that represents a remote D-Bus object.
        /// </summary>
        /// <typeparam name="T">Interface of the D-Bus object.</typeparam>
        /// <param name="serviceName">Name of the service that exposes the object.</param>
        /// <param name="path">Object path of the object.</param>
        /// <returns>
        /// Proxy object.
        /// </returns>
        public T CreateProxy<T>(string serviceName, ObjectPath path)
        {
            return (T)CreateProxy(typeof(T), serviceName, path);
        }

        /// <summary>
        /// Releases a service name assigned to the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// <c>true</c> when the name was assigned to this connection; <c>false</c> when the name was not assigned to this connection.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public async Task<bool> UnregisterServiceAsync(string serviceName)
        {
            ThrowIfAutoConnect();
            var connection = GetConnectedConnection();
            var reply = await connection.ReleaseNameAsync(serviceName);
            return reply == ReleaseNameReply.ReplyReleased;
        }

        /// <summary>
        /// Queues a service name registration for the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="onAquired">Action invoked when the service name is assigned to the connection.</param>
        /// <param name="onLost">Action invoked when the service name is no longer assigned to the connection.</param>
        /// <param name="options">Options for the registration.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <exception cref="ProtocolException">Unexpected reply.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
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

        /// <summary>
        /// Queues a service name registration for the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="options">Options for the registration.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <exception cref="ProtocolException">Unexpected reply.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public Task QueueServiceRegistrationAsync(string serviceName, ServiceRegistrationOptions options)
            => QueueServiceRegistrationAsync(serviceName, null, null, options);

        /// <summary>
        /// Requests a service name to be assigned to the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="onLost">Action invoked when the service name is no longer assigned to the connection.</param>
        /// <param name="options"></param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public async Task RegisterServiceAsync(string serviceName, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default)
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
            var reply = await connection.RequestNameAsync(serviceName, requestOptions, null, onLost, CaptureSynchronizationContext());
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

        /// <summary>
        /// Requests a service name to be assigned to the connection.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="options"></param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed after it was established.</exception>
        /// <exception cref="DBusException">Error returned by remote peer.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public Task RegisterServiceAsync(string serviceName, ServiceRegistrationOptions options)
            => RegisterServiceAsync(serviceName, null, options);

        /// <summary>
        /// Publishes an object.
        /// </summary>
        /// <param name="o">Object to publish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed.</exception>
        public Task RegisterObjectAsync(IDBusObject o)
        {
            return RegisterObjectsAsync(new[] { o });
        }

        /// <summary>
        /// Publishes objects.
        /// </summary>
        /// <param name="objects">Objects to publish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection was closed.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
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

        /// <summary>
        /// Unpublishes an object.
        /// </summary>
        /// <param name="path">Path of object to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public void UnregisterObject(ObjectPath path)
            => UnregisterObjects(new[] { path });

        /// <summary>
        /// Unpublishes an object.
        /// </summary>
        /// <param name="o">object to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public void UnregisterObject(IDBusObject o)
            => UnregisterObject(o.ObjectPath);

        /// <summary>
        /// Unpublishes objects.
        /// </summary>
        /// <param name="paths">Paths of objects to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
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

        /// <summary>
        /// Unpublishes objects.
        /// </summary>
        /// <param name="objects">Objects to unpublish.</param>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        /// <remarks>
        /// This operation is not supported for AutoConnection connections.
        /// </remarks>
        public void UnregisterObjects(IEnumerable<IDBusObject> objects)
            => UnregisterObjects(objects.Select(o => o.ObjectPath));

        /// <summary>
        /// List services that can be activated.
        /// </summary>
        /// <returns>
        /// List of activatable services.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<string[]> ListActivatableServicesAsync()
            => DBus.ListActivatableNamesAsync();

        /// <summary>
        /// Resolves the local address for a service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// Local address of service. <c>null</c> is returned when the service name is not available.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
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

        /// <summary>
        /// Activates a service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// The result of the activation.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<ServiceStartResult> ActivateServiceAsync(string serviceName)
            => DBus.StartServiceByNameAsync(serviceName, 0);

        /// <summary>
        /// Checks if a service is available.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>
        /// <c>true</c> when the service is available, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<bool> IsServiceActiveAsync(string serviceName)
            => DBus.NameHasOwnerAsync(serviceName);

        /// <summary>
        /// Resolves the local address for a service.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="handler">Action invoked when the local name of the service changes.</param>
        /// <param name="onError">Action invoked when the connection closes.</param>
        /// <returns>
        /// Disposable that allows to stop receiving notifications.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        /// <remarks>
        /// The event handler will be called when the service name is already registered.
        /// </remarks>
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

        /// <summary>
        /// List services that are available.
        /// </summary>
        /// <returns>
        /// List of available services.
        /// </returns>
        /// <exception cref="ConnectException">There was an error establishing the connection.</exception>
        /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid in the current state.</exception>
        /// <exception cref="DisconnectedException">The connection is closed.</exception>
        public Task<string[]> ListServicesAsync()
            => DBus.ListNamesAsync();

        internal Task<string> StartServerAsync(string address)
        {
            // TODO: handle state
            lock (_gate)
            {
                ThrowIfNotConnected();
                return _dbusConnection.StartServerAsync(address);
            }
        }

        // Used by tests
        internal void Connect(DBusConnection dbusConnection)
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
                ThrowDisposed();
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

        internal static Exception CreateDisposedException()
            => new ObjectDisposedException(typeof(Connection).FullName);

        private static void ThrowDisposed()
        {
            throw CreateDisposedException();
        }

        internal static void ThrowIfNotConnecting(bool disposed, ConnectionState state, Exception disconnectReason)
        {
            if (disposed)
            {
                ThrowDisposed();
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

        private Task<DBusConnection> GetConnectionTask()
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

        private DBusConnection GetConnectedConnection()
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
                throw new InvalidOperationException($"Operation not supported for {nameof(ClientConnectionOptions.AutoConnect)} Connection.");
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
                var disposeUserToken = _disposeUserToken;
                _dbusConnection = null;
                _connectCts = null;
                _dbusConnectionTask = null;
                _dbusConnectionTcs = null;
                _disposeUserToken = NoDispose;

                foreach (var registeredObject in _registeredObjects)
                {
                    registeredObject.Value.Unregister();
                }
                _registeredObjects.Clear();

                _state = ConnectionState.Disconnecting;
                EmitConnectionStateChanged();

                connectionCts?.Cancel();
                connectionCts?.Dispose();
                dbusConnectionTcs?.SetException(
                    dispose ? CreateDisposedException() : 
                    exception.GetType() == typeof(ConnectException) ? exception :
                    new DisconnectedException(exception));
                connection?.Disconnect(dispose, exception);
                if (disposeUserToken != NoDispose)
                {
                    _disposeAction?.Invoke(disposeUserToken);
                }

                if (_state == ConnectionState.Disconnecting)
                {
                    _state = ConnectionState.Disconnected;
                    EmitConnectionStateChanged();
                }
            }
        }

        private void EmitConnectionStateChanged(EventHandler<ConnectionStateChangedEventArgs> handler = null)
        {
            var disconnectReason = _disconnectReason;
            if (_state == ConnectionState.Connecting)
            {
                _disconnectReason = null;
            }

            if (handler == null)
            {
                handler = _stateChangedEvent;
            }

            if (handler == null)
            {
                return;
            }

            if (disconnectReason != null
             && disconnectReason.GetType() != typeof(ConnectException)
             && disconnectReason.GetType() != typeof(ObjectDisposedException)
             && disconnectReason.GetType() != typeof(DisconnectedException))
            {
                disconnectReason = new DisconnectedException(disconnectReason);
            }
            var connectionInfo = _state == ConnectionState.Connected ? _dbusConnection.ConnectionInfo : null;
            var stateChangeEvent = new ConnectionStateChangedEventArgs(_state, disconnectReason, connectionInfo);


            if (_synchronizationContext != null && SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(_ => handler(this, stateChangeEvent), null);
            }
            else
            {
                handler(this, stateChangeEvent);
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

        internal SynchronizationContext CaptureSynchronizationContext() => _synchronizationContext;

        private static Connection CreateSessionConnection() => CreateConnection(Address.Session, ref s_sessionConnection);

        private static Connection CreateSystemConnection() => CreateConnection(Address.System, ref s_systemConnection);

        private static Connection CreateConnection(string address, ref Connection connection)
        {
            address = address ?? "unix:";
            if (Volatile.Read(ref connection) != null)
            {
                return connection;
            }
            var newConnection = new Connection(new DefaultConnectionOptions(address) { AutoConnect = true, SynchronizationContext = null });
            Interlocked.CompareExchange(ref connection, newConnection, null);
            return connection;
        }
    }
}

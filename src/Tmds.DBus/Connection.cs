// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
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

        public Connection(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            _address = address;
            _factory = new ProxyFactory(this);
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
                    _onDisconnectSynchronizationContext = SynchronizationContext.Current;
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

        public async Task<bool> UnregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var reply = await _dbusConnection.ReleaseNameAsync(serviceName, cancellationToken);
            return reply == ReleaseNameReply.ReplyReleased;
        }

        public async Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default, CancellationToken cancellationToken = default(CancellationToken))
        {
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
            var reply = await _dbusConnection.RequestNameAsync(serviceName, requestOptions, onAquired, onLost, SynchronizationContext.Current, cancellationToken);
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

        public async Task RegisterServiceAsync(string name, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default, CancellationToken cancellationToken = default(CancellationToken))
        {
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
            var reply = await _dbusConnection.RequestNameAsync(name, requestOptions, null, onLost, SynchronizationContext.Current, cancellationToken);
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

        public async Task RegisterObjectAsync(IDBusObject o, CancellationToken cancellationToken = default(CancellationToken))
        {
            var assembly = DynamicAssembly.Instance;
            var implementationType = assembly.GetExportTypeInfo(o.GetType());
            var objectPath = o.ObjectPath;

            var registration = (DBusAdapter)Activator.CreateInstance(implementationType.AsType(), _dbusConnection, objectPath, o, _factory, SynchronizationContext.Current);
            lock (_gate)
            {
                ThrowIfNotConnected();

                _dbusConnection.AddMethodHandler(objectPath, registration.HandleMethodCall);
                _registeredObjects.Add(objectPath, registration);
            }
            try
            {
                await registration.WatchSignalsAsync(cancellationToken);
                lock (_gate)
                {
                    try
                    {
                        ThrowIfNotConnected();

                        registration.CompleteRegistration();
                    }
                    catch
                    {
                        registration.Unregister();
                        throw;
                    }
                }
            }
            catch
            {
                lock (_gate)
                {
                    _dbusConnection.RemoveMethodHandler(objectPath);
                    _registeredObjects.Remove(objectPath);
                }
                throw;
            }
        }

        public void UnregisterObject(ObjectPath objectPath)
        {
            lock (_gate)
            {
                ThrowIfNotConnected();

                DBusAdapter registration;
                if (_registeredObjects.TryGetValue(objectPath, out registration))
                {
                    registration.Unregister();
                    _registeredObjects.Remove(objectPath);
                    _dbusConnection.RemoveMethodHandler(objectPath);
                }
            }
        }

        public Task<string[]> ListActivatableServicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            return DBus.ListActivatableNamesAsync(cancellationToken);
        }

        public async Task<string> ResolveServiceOwnerAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            try
            {
                return await DBus.GetNameOwnerAsync(serviceName, cancellationToken);
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

        public Task<ServiceStartResult> ActivateServiceAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            return DBus.StartServiceByNameAsync(serviceName, 0, cancellationToken);
        }

        public Task<bool> IsServiceAvailableAsync(string serviceName, CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();
            return DBus.NameHasOwnerAsync(serviceName, cancellationToken);
        }

        public async Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();

            var synchronizationContext = SynchronizationContext.Current;
            bool eventEmitted = false;
            var wrappedDisposable = new WrappedDisposable();

            wrappedDisposable.Disposable = await _dbusConnection.WatchNameOwnerChangedAsync(serviceName,
                e => {
                    eventEmitted = true;
                    if (synchronizationContext != null)
                    {
                        synchronizationContext.Post(o =>
                        {
                            if (!wrappedDisposable.IsDisposed)
                            {
                                handler(e);
                            }
                        }, null);
                    }
                    else
                    {
                        if (!wrappedDisposable.IsDisposed)
                        {
                            handler(e);
                        }
                    }
                }, cancellationToken);

            try
            {
                var currentName = await ResolveServiceOwnerAsync(serviceName, cancellationToken);
                if (currentName != null && !eventEmitted)
                {
                    var e = new ServiceOwnerChangedEventArgs(serviceName, null, currentName);
                    handler(e);
                }
                return wrappedDisposable;
            }
            catch
            {
                wrappedDisposable.Dispose();
                throw;
            }
        }

        public Task<string[]> ListServicesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfNotConnected();
            ThrowIfRemoteIsNotBus();

            return DBus.ListNamesAsync(cancellationToken);
        }

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
                new object[] { _dbusConnection, _factory, busName, path });

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
    }
}

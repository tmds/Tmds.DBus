// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.Transports;
using System.Threading.Tasks.Sources;
using System.Diagnostics;

namespace Tmds.DBus
{
    class DBusConnection
    {
        class MyValueTaskSource<T> : IValueTaskSource<T>
        {
            private ManualResetValueTaskSourceCore<T> _core;
            private volatile bool _continuationSet;

            public MyValueTaskSource(bool runContinuationsAsynchronously)
            {
                RunContinuationsAsynchronously = runContinuationsAsynchronously;
            }

            public bool RunContinuationsAsynchronously
            {
                get => _core.RunContinuationsAsynchronously;
                set => _core.RunContinuationsAsynchronously = value;
            }

            public void SetResult(T result)
            {
                // Ensure we complete the Task from the read loop.
                if (!RunContinuationsAsynchronously)
                {
                    SpinWait wait = default;
                    while (!_continuationSet)
                    {
                        wait.SpinOnce();
                    }
                }
                _core.SetResult(result);
            }

            public void SetException(Exception exception) => _core.SetException(exception);

            public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

            public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                _core.OnCompleted(continuation, state, token, flags);
                _continuationSet = true;
            }

            T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);
        }

        private class SignalHandlerRegistration : IDisposable
        {
            public SignalHandlerRegistration(DBusConnection dbusConnection, SignalMatchRule rule, SignalHandler handler)
            {
                _connection = dbusConnection;
                _rule = rule;
                _handler = handler;
            }

            public void Dispose()
            {
                _connection.RemoveSignalHandler(_rule, _handler);
            }

            private DBusConnection _connection;
            private SignalMatchRule _rule;
            private SignalHandler _handler;
        }

        private class NameOwnerWatcherRegistration : IDisposable
        {
            public NameOwnerWatcherRegistration(DBusConnection dbusConnection, string key, OwnerChangedMatchRule rule, Action<ServiceOwnerChangedEventArgs, Exception> handler)
            {
                _connection = dbusConnection;
                _rule = rule;
                _handler = handler;
                _key = key;
            }

            public void Dispose()
            {
                _connection.RemoveNameOwnerWatcher(_key, _rule, _handler);
            }

            private DBusConnection _connection;
            private OwnerChangedMatchRule _rule;
            private Action<ServiceOwnerChangedEventArgs, Exception> _handler;
            private string _key;
        }

        private class ServiceNameRegistration
        {
            public Action OnAquire;
            public Action OnLost;
            public SynchronizationContext SynchronizationContext;
        }

        private sealed class NameWatcher
        {
            public Task<IDisposable> NameOwnerHandlerTask;
            public Task WatchNameOwnerTask;
            public Task<string> GetNameOwnerTask;
            public int RefCount;
            public string CurrentOwner;
        }

        public static readonly ObjectPath DBusObjectPath = new ObjectPath("/org/freedesktop/DBus");
        public const string DBusServiceName = "org.freedesktop.DBus";
        public const string DBusInterface = "org.freedesktop.DBus";
        private static readonly char[] s_dot = new[] { '.' };

        public static async Task<DBusConnection> ConnectAsync(ClientSetupResult connectionContext, bool runContinuationsAsynchronously, Action<Exception> onDisconnect, CancellationToken cancellationToken)
        {
            var _entries = AddressEntry.ParseEntries(connectionContext.ConnectionAddress);
            if (_entries.Length == 0)
            {
                throw new ArgumentException("No addresses were found", nameof(connectionContext.ConnectionAddress));
            }

            IMessageStream stream = null;
            var index = 0;
            while (index < _entries.Length)
            {
                AddressEntry entry = _entries[index++];

                try
                {
                    stream = await Transport.ConnectAsync(entry, connectionContext, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    if (index < _entries.Length)
                        continue;
                    throw;
                }

                break;
            }

            return await DBusConnection.CreateAndConnectAsync(stream, runContinuationsAsynchronously, onDisconnect).ConfigureAwait(false);
        }

        private readonly IMessageStream _stream;
        private readonly object _gate = new object();
        private Dictionary<SignalMatchRule, SignalHandler> _signalHandlers = new Dictionary<SignalMatchRule, SignalHandler>();
        private Dictionary<string, Action<ServiceOwnerChangedEventArgs, Exception>> _nameOwnerHandlers = new Dictionary<string, Action<ServiceOwnerChangedEventArgs, Exception>>();
        private readonly Dictionary<string, NameWatcher> _nameWatchers = new Dictionary<string, NameWatcher>();
        private readonly Dictionary<string, List<string>> _uniqueIdToNames = new Dictionary<string, List<string>>();
        private Dictionary<uint, TaskCompletionSource<Message>> _pendingMethods = new Dictionary<uint, TaskCompletionSource<Message>>();
        private readonly Dictionary<ObjectPath, MethodHandler> _methodHandlers = new Dictionary<ObjectPath, MethodHandler>();
        private readonly Dictionary<ObjectPath, string[]> _childNames = new Dictionary<ObjectPath, string[]>();
        private readonly Dictionary<string, ServiceNameRegistration> _serviceNameRegistrations = new Dictionary<string, ServiceNameRegistration>();
        private readonly bool _runContinuationsAsynchronously;

        private ConnectionState _state = ConnectionState.Created;
        private bool _disposed = false;
        private Action<Exception> _onDisconnect;
        private Exception _disconnectReason;
        private int _methodSerial;

        public ConnectionInfo ConnectionInfo { get; private set; }

        // For testing
        internal static async Task<DBusConnection> CreateAndConnectAsync(IMessageStream stream, bool runContinuationsAsynchronously = false, Action<Exception> onDisconnect = null)
        {
            var connection = new DBusConnection(stream, runContinuationsAsynchronously);
            await connection.ConnectAsync(onDisconnect).ConfigureAwait(false);
            return connection;
        }

        private DBusConnection(IMessageStream stream, bool runContinuationsAsynchronously)
        {
            _stream = stream;
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
        }

        public DBusConnection(bool localServer, bool runContinuationsAsynchronously)
        {
            if (localServer != true)
            {
                throw new ArgumentException("Constructor for LocalServer.", nameof(localServer));
            }
            _stream = new LocalServer(this);
            _runContinuationsAsynchronously = runContinuationsAsynchronously;
            ConnectionInfo = new ConnectionInfo(string.Empty);
        }

        private async Task ConnectAsync(Action<Exception> onDisconnect)
        {
            lock (_gate)
            {
                if (_state != ConnectionState.Created)
                {
                    throw new InvalidOperationException("Unable to connect");
                }
                _state = ConnectionState.Connecting;
            }

            if (SynchronizationContext.Current != null)
            {
                SynchronizationContext.SetSynchronizationContext(null);
                await Task.Yield();
            }

            _onDisconnect = OnDisconnect;

            ReceiveMessages(_stream, EmitDisconnected);

            string localName = await CallHelloAsync().ConfigureAwait(false);
            ConnectionInfo = new ConnectionInfo(localName);

            lock (_gate)
            {
                if (_state == ConnectionState.Connecting)
                {
                    _state = ConnectionState.Connected;
                }
                ThrowIfNotConnected();

                _onDisconnect = onDisconnect;
            }
        }

        private void OnDisconnect(Exception e)
        {
            Disconnect(dispose: false, exception: e);
        }

        public Task<Message> CallMethodAsync(Message msg)
        {
            return CallMethodAsync(msg, checkConnected: true, checkReplyType: true);
        }

        public void EmitSignal(Message message)
        {
            message.Header.Serial = GenerateSerial();
            _stream.TrySendMessage(message);
        }

        public void AddMethodHandlers(IEnumerable<KeyValuePair<ObjectPath, MethodHandler>> handlers)
        {
            lock (_gate)
            {
                foreach (var handler in handlers)
                {
                    _methodHandlers.Add(handler.Key, handler.Value);

                    AddChildName(handler.Key, checkChildNames: false);
                }
            }
        }

        private void AddChildName(ObjectPath path, bool checkChildNames)
        {
            if (path == ObjectPath.Root)
            {
                return;
            }
            var parent = path.Parent;
            var decomposed = path.Decomposed;
            var name = decomposed[decomposed.Length - 1];
            string[] childNames = null;
            if (_childNames.TryGetValue(parent, out childNames) && checkChildNames)
            {
                for (var i = 0; i < childNames.Length; i++)
                {
                    if (childNames[i] == name)
                    {
                        return;
                    }
                }
            }
            var newChildNames = new string[(childNames?.Length ?? 0) + 1];
            if (childNames != null)
            {
                for (var i = 0; i < childNames.Length; i++)
                {
                    newChildNames[i] = childNames[i];
                }
            }
            newChildNames[newChildNames.Length - 1] = name;
            _childNames[parent] = newChildNames;

            AddChildName(parent, checkChildNames: true);
        }

        public void RemoveMethodHandlers(IEnumerable<ObjectPath> paths)
        {
            lock (_gate)
            {
                foreach (var path in paths)
                {
                    var removed = _methodHandlers.Remove(path);
                    var hasChildren = _childNames.ContainsKey(path);
                    if (removed && !hasChildren)
                    {
                        RemoveChildName(path);
                    }
                }
            }
        }

        private void RemoveChildName(ObjectPath path)
        {
            if (path == ObjectPath.Root)
            {
                return;
            }
            var parent = path.Parent;
            var decomposed = path.Decomposed;
            var name = decomposed[decomposed.Length - 1];
            string[] childNames = _childNames[parent];
            if (childNames.Length == 1)
            {
                _childNames.Remove(parent);
                if (!_methodHandlers.ContainsKey(parent))
                {
                    RemoveChildName(parent);
                }
            }
            else
            {
                int writeAt = 0;
                bool found = false;
                var newChildNames = new string[childNames.Length - 1];
                for (int i = 0; i < childNames.Length; i++)
                {
                    if (!found && childNames[i] == name)
                    {
                        found = true;
                    }
                    else
                    {
                        newChildNames[writeAt++] = childNames[i];
                    }
                }
                _childNames[parent] = newChildNames;
            }
        }

        public async Task<IDisposable> WatchSignalAsync(ObjectPath path, string @interface, string signalName, string sender, SignalHandler handler)
        {
            if (!ConnectionInfo.RemoteIsBus)
            {
                sender = null;
            }

            SignalMatchRule rule = new SignalMatchRule()
            {
                Interface = @interface,
                Member = signalName,
                Path = path,
                Sender = sender
            };

            Task addMatch = Task.CompletedTask;
            Task watchNameOwner = Task.CompletedTask;
            lock (_gate)
            {
                ThrowIfNotConnected();

                if (ShouldWatchNameOwner(sender))
                {
                    watchNameOwner = WatchNameOwnerAsync(sender);
                }

                if (_signalHandlers.ContainsKey(rule))
                {
                    _signalHandlers[rule] = (SignalHandler)Delegate.Combine(_signalHandlers[rule], handler);
                }
                else
                {
                    _signalHandlers[rule] = handler;
                    if (ConnectionInfo.RemoteIsBus)
                    {
                        addMatch = CallAddMatchRuleAsync(rule.ToString());
                    }
                }
            }
            SignalHandlerRegistration registration = new SignalHandlerRegistration(this, rule, handler);
            try
            {
                await watchNameOwner.ConfigureAwait(false);
                await addMatch.ConfigureAwait(false);
            }
            catch
            {
                EnsureExceptionObserved(addMatch);
                registration.Dispose();
                throw;
            }
            return registration;
        }

        private bool ShouldWatchNameOwner(string sender)
        {
            return ConnectionInfo.RemoteIsBus && sender != null && sender.Length > 0 && sender[0] != ':' && sender != DBusServiceName;
        }

        public async Task<RequestNameReply> RequestNameAsync(string name, RequestNameOptions options, Action onAquired, Action onLost, SynchronizationContext synchronzationContext)
        {
            lock (_gate)
            {
                ThrowIfNotConnected();
                ThrowIfRemoteIsNotBus();

                if (_serviceNameRegistrations.ContainsKey(name))
                {
                    throw new InvalidOperationException("The name is already requested");
                }
                _serviceNameRegistrations[name] = new ServiceNameRegistration
                {
                    OnAquire = onAquired,
                    OnLost = onLost,
                    SynchronizationContext = synchronzationContext
                };
            }
            try
            {
                var reply = await CallRequestNameAsync(name, options).ConfigureAwait(false);
                return reply;
            }
            catch
            {
                lock (_gate)
                {
                    _serviceNameRegistrations.Remove(name);
                }
                throw;
            }
        }

        public Task<ReleaseNameReply> ReleaseNameAsync(string name)
        {
            lock (_gate)
            {
                ThrowIfRemoteIsNotBus();

                if (!_serviceNameRegistrations.ContainsKey(name))
                {
                    return Task.FromResult(ReleaseNameReply.NotOwner);
                }
                _serviceNameRegistrations.Remove(name);

                ThrowIfNotConnected();
            }
            return CallReleaseNameAsync(name);
        }

        public async Task<IDisposable> WatchNameOwnerChangedAsync(string serviceName, Action<ServiceOwnerChangedEventArgs, Exception> handler)
        {
            var rule = new OwnerChangedMatchRule(serviceName);
            string key = serviceName;

            Task task = null;
            lock (_gate)
            {
                ThrowIfNotConnected();
                ThrowIfRemoteIsNotBus();

                if (_nameOwnerHandlers.ContainsKey(key))
                {
                    _nameOwnerHandlers[key] = (Action<ServiceOwnerChangedEventArgs, Exception>)Delegate.Combine(_nameOwnerHandlers[key], handler);
                    task = Task.CompletedTask;
                }
                else
                {
                    _nameOwnerHandlers[key] = handler;
                    task = CallAddMatchRuleAsync(rule.ToString());
                }
            }
            NameOwnerWatcherRegistration registration = new NameOwnerWatcherRegistration(this, key, rule, handler);
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                registration.Dispose();
                throw;
            }
            return registration;
        }

        private void ThrowIfRemoteIsNotBus()
        {
            if (ConnectionInfo.RemoteIsBus != true)
            {
                throw new InvalidOperationException("The remote peer is not a bus");
            }
        }

        internal async void ReceiveMessages(IMessageStream peer, Action<IMessageStream, Exception> disconnectAction)
        {
            try
            {
                while (true)
                {
                    Message msg = await peer.ReceiveMessageAsync().ConfigureAwait(false);
                    if (msg == null)
                    {
                        throw new IOException("Connection closed by peer");
                    }
                    HandleMessage(msg, peer);
                }
            }
            catch (Exception e)
            {
                disconnectAction?.Invoke(peer, e);
            }
        }

        private void EmitDisconnected(IMessageStream peer, Exception e)
        {
            lock (_gate)
            {
                _onDisconnect?.Invoke(e);
                _onDisconnect = null;
            }
        }

        private void HandleMessage(Message msg, IMessageStream peer)
        {
            uint? serial = msg.Header.ReplySerial;
            if (serial != null)
            {
                uint serialValue = (uint)serial;
                TaskCompletionSource<Message> pending = null;
                lock (_gate)
                {
                    if (_pendingMethods?.TryGetValue(serialValue, out pending) == true)
                    {
                        _pendingMethods.Remove(serialValue);
                    }
                }
                if (pending != null)
                {
                    pending.SetResult(msg);
                }
                else
                {
                    throw new ProtocolException("Unexpected reply message received: MessageType = '" + msg.Header.MessageType + "', ReplySerial = " + serialValue);
                }
                return;
            }

            switch (msg.Header.MessageType)
            {
                case MessageType.MethodCall:
                    HandleMethodCall(msg, peer);
                    break;
                case MessageType.Signal:
                    HandleSignal(msg);
                    break;
                case MessageType.Error:
                    string errMsg = String.Empty;
                    if (msg.Header.Signature.Value.Value.StartsWith("s", StringComparison.Ordinal))
                    {
                        MessageReader reader = new MessageReader(msg, null);
                        errMsg = reader.ReadString();
                    }
                    throw new DBusException(msg.Header.ErrorName, errMsg);
                case MessageType.Invalid:
                default:
                    throw new ProtocolException("Invalid message received: MessageType='" + msg.Header.MessageType + "'");
            }
        }

        private void HandleSignal(Message msg)
        {
            switch (msg.Header.Interface)
            {
                case "org.freedesktop.DBus":
                    if (msg.Header.Sender != DBusServiceName)
                    {
                        break;
                    }
                    switch (msg.Header.Member)
                    {
                        case "NameAcquired":
                        case "NameLost":
                            {
                                MessageReader reader = new MessageReader(msg, null);
                                var name = reader.ReadString();
                                bool aquiredNotLost = msg.Header.Member == "NameAcquired";
                                OnNameAcquiredOrLost(name, aquiredNotLost);
                                return;
                            }
                        case "NameOwnerChanged":
                            {
                                MessageReader reader = new MessageReader(msg, null);
                                var serviceName = reader.ReadString();
                                if (serviceName[0] == ':')
                                {
                                    return;
                                }
                                var oldOwner = reader.ReadString();
                                oldOwner = string.IsNullOrEmpty(oldOwner) ? null : oldOwner;
                                var newOwner = reader.ReadString();
                                newOwner = string.IsNullOrEmpty(newOwner) ? null : newOwner;
                                Action<ServiceOwnerChangedEventArgs, Exception> watchers = null;
                                var splitName = serviceName.Split(s_dot);
                                var keys = new string[splitName.Length + 2];
                                keys[0] = ".*";
                                var sb = new StringBuilder();
                                for (int i = 0; i < splitName.Length; i++)
                                {
                                    sb.Append(splitName[i]);
                                    sb.Append(".*");
                                    keys[i + 1] = sb.ToString();
                                    sb.Remove(sb.Length - 1, 1);
                                }
                                keys[keys.Length - 1] = serviceName;
                                lock (_gate)
                                {
                                    UpdateOwnership(serviceName, oldOwner, newOwner);

                                    foreach (var key in keys)
                                    {
                                        Action<ServiceOwnerChangedEventArgs, Exception> keyWatchers = null;
                                        if (_nameOwnerHandlers?.TryGetValue(key, out keyWatchers) == true)
                                        {
                                            watchers += keyWatchers;
                                        }
                                    }
                                }
                                watchers?.Invoke(new ServiceOwnerChangedEventArgs(serviceName, oldOwner, newOwner, this), null);
                                return;
                            }
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            string sender = msg.Header.Sender;
            SignalMatchRule rule = new SignalMatchRule()
            {
                Interface = msg.Header.Interface,
                Member = msg.Header.Member,
                Path = msg.Header.Path.Value,
                Sender = ConnectionInfo.RemoteIsBus ? sender : null
            };

            SignalHandler signalHandler = null;
            lock (_gate)
            {
                _signalHandlers?.TryGetValue(rule, out signalHandler);

                if (ConnectionInfo.RemoteIsBus && _uniqueIdToNames.TryGetValue(sender, out List<string> ownedNames))
                {
                    foreach (var name in ownedNames)
                    {
                        rule.Sender = name;
                        if (_signalHandlers?.TryGetValue(rule, out SignalHandler handler) == true)
                        {
                            if (signalHandler == null)
                            {
                                signalHandler = handler;
                            }
                            else
                            {
                                signalHandler = (SignalHandler)Delegate.Combine(signalHandler, handler);
                            }
                        }
                    }
                }
            }

            if (signalHandler != null)
            {
                try
                {
                    signalHandler(this, msg, null);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Signal handler for " + msg.Header.Interface + "." + msg.Header.Member + " threw an exception", e);
                }
            }
        }

        private void UpdateOwnership(string serviceName, string oldOwner, string newOwner)
        {
            Debug.Assert(Monitor.IsEntered(_gate));
            if (!_nameWatchers.TryGetValue(serviceName, out NameWatcher watcher))
            {
                return;
            }

            if (oldOwner != null && _uniqueIdToNames.TryGetValue(oldOwner, out List<string> oldOwnedNames) == true)
            {
                oldOwnedNames.Remove(serviceName);
                if (oldOwnedNames.Count == 0)
                {
                    _uniqueIdToNames.Remove(oldOwner);
                }
            }

            if (newOwner != null)
            {
                if (!_uniqueIdToNames.TryGetValue(newOwner, out List<string> newOwnedNames))
                {
                    newOwnedNames = new List<string>();
                    _uniqueIdToNames[newOwner] = newOwnedNames;
                }
                if (!newOwnedNames.Contains(serviceName))
                {
                    newOwnedNames.Add(serviceName);
                }
            }
            watcher.CurrentOwner = newOwner;
        }

        private void OnNameAcquiredOrLost(string name, bool aquiredNotLost)
        {
            Action action = null;
            SynchronizationContext synchronizationContext = null;
            lock (_gate)
            {
                ServiceNameRegistration registration;
                if (_serviceNameRegistrations.TryGetValue(name, out registration))
                {
                    action = aquiredNotLost ? registration.OnAquire : registration.OnLost;
                    synchronizationContext = registration.SynchronizationContext;
                }
            }
            if (action != null)
            {
                if (synchronizationContext != null)
                {
                    synchronizationContext.Post(_ => action(), null);
                }
                else
                {
                    action();
                }
            }
        }

        public void Dispose()
        {
            Disconnect(dispose: true, exception: null);
        }

        public void Disconnect(bool dispose, Exception exception)
        {
            Dictionary<uint, TaskCompletionSource<Message>> pendingMethods = null;
            Dictionary<SignalMatchRule, SignalHandler> signalHandlers = null;
            Dictionary<string, Action<ServiceOwnerChangedEventArgs, Exception>> nameOwnerWatchers = null;
            lock (_gate)
            {
                if (_state == ConnectionState.Disconnected || _state == ConnectionState.Created)
                {
                    return;
                }

                _state = ConnectionState.Disconnected;
                _disposed = dispose;
                _stream.Dispose();
                _disconnectReason = exception;
                pendingMethods = _pendingMethods;
                _pendingMethods = null;
                signalHandlers = _signalHandlers;
                _signalHandlers = null;
                nameOwnerWatchers = _nameOwnerHandlers;
                _nameOwnerHandlers = null;
                _serviceNameRegistrations.Clear();

                _nameWatchers.Clear();
                _uniqueIdToNames.Clear();

                _onDisconnect = null;

                Func<Exception> createException = () =>
                    dispose ? Connection.CreateDisposedException() : new DisconnectedException(exception);

                foreach (var watcher in nameOwnerWatchers)
                {
                    watcher.Value(default(ServiceOwnerChangedEventArgs), createException());
                }

                foreach (var handler in signalHandlers)
                {
                    handler.Value(this, null, createException());
                }

                foreach (var tcs in pendingMethods.Values)
                {
                    tcs.SetException(createException());
                }
            }

            if (_onDisconnect != null)
            {
                _onDisconnect(dispose ? null : exception);
            }
        }

        private void SendMessage(Message message, IMessageStream peer)
        {
            if (message.Header.Serial == 0)
            {
                message.Header.Serial = GenerateSerial();
            }
            peer.TrySendMessage(message);
        }

        private async void HandleMethodCall(Message methodCall, IMessageStream peer)
        {
            switch (methodCall.Header.Interface)
            {
                case "org.freedesktop.DBus.Peer":
                    switch (methodCall.Header.Member)
                    {
                        case "Ping":
                            {
                                SendMessage(MessageHelper.ConstructReply(methodCall), peer);
                                return;
                            }
                        case "GetMachineId":
                            {
                                SendMessage(MessageHelper.ConstructReply(methodCall, Environment.MachineId), peer);
                                return;
                            }
                    }
                    break;
            }

            MethodHandler methodHandler;
            if (_methodHandlers.TryGetValue(methodCall.Header.Path.Value, out methodHandler))
            {
                var reply = await methodHandler(methodCall).ConfigureAwait(false);
                if (methodCall.Header.ReplyExpected)
                {
                    reply.Header.ReplySerial = methodCall.Header.Serial;
                    reply.Header.Destination = methodCall.Header.Sender;
                    SendMessage(reply, peer);
                }
            }
            else
            {
                if (methodCall.Header.Interface == "org.freedesktop.DBus.Introspectable"
                    && methodCall.Header.Member == "Introspect"
                    && methodCall.Header.Path.HasValue)
                {
                    var path = methodCall.Header.Path.Value;
                    var childNames = GetChildNames(path);
                    if (childNames.Length > 0)
                    {
                        var writer = new IntrospectionWriter();

                        writer.WriteDocType();
                        writer.WriteNodeStart(path.Value);
                        writer.WriteIntrospectableInterface();
                        writer.WritePeerInterface();
                        foreach (var child in childNames)
                        {
                            writer.WriteChildNode(child);
                        }
                        writer.WriteNodeEnd();

                        var xml = writer.ToString();
                        SendMessage(MessageHelper.ConstructReply(methodCall, xml), peer);
                        return;
                    }
                }
                SendUnknownMethodError(methodCall, peer);
            }
        }

        private async Task<string> CallHelloAsync()
        {
            Message callMsg = new Message(
                new Header(MessageType.MethodCall)
                {
                    Path = DBusObjectPath,
                    Interface = DBusInterface,
                    Member = "Hello",
                    Destination = DBusServiceName
                },
                body: null,
                unixFds: null
            );

            Message reply = await CallMethodAsync(callMsg, checkConnected: false, checkReplyType: false).ConfigureAwait(false);

            if (reply.Header.MessageType == MessageType.Error)
            {
                return string.Empty;
            }
            else if (reply.Header.MessageType == MessageType.MethodReturn)
            {
                var reader = new MessageReader(reply, null);
                return reader.ReadString();
            }
            else
            {
                throw new ProtocolException("Got unexpected message of type " + reply.Header.MessageType + " while waiting for a MethodReturn or Error");
            }
        }

        private async Task<RequestNameReply> CallRequestNameAsync(string name, RequestNameOptions options)
        {
            var writer = new MessageWriter();
            writer.WriteString(name);
            writer.WriteUInt32((uint)options);

            Message callMsg = new Message(
                new Header(MessageType.MethodCall)
                {
                    Path = DBusObjectPath,
                    Interface = DBusInterface,
                    Member = "RequestName",
                    Destination = DBusServiceName,
                    Signature = "su"
                },
                writer.ToArray(),
                writer.UnixFds
            );

            Message reply = await CallMethodAsync(callMsg, checkConnected: true, checkReplyType: true).ConfigureAwait(false);

            var reader = new MessageReader(reply, null);
            var rv = reader.ReadUInt32();
            return (RequestNameReply)rv;
        }

        private async Task<ReleaseNameReply> CallReleaseNameAsync(string name)
        {
            var writer = new MessageWriter();
            writer.WriteString(name);

            Message callMsg = new Message(
                new Header(MessageType.MethodCall)
                {
                    Path = DBusObjectPath,
                    Interface = DBusInterface,
                    Member = "ReleaseName",
                    Destination = DBusServiceName,
                    Signature = Signature.StringSig
                },
                writer.ToArray(),
                writer.UnixFds
            );

            Message reply = await CallMethodAsync(callMsg, checkConnected: true, checkReplyType: true).ConfigureAwait(false);

            var reader = new MessageReader(reply, null);
            var rv = reader.ReadUInt32();
            return (ReleaseNameReply)rv;
        }

        private void SendUnknownMethodError(Message callMessage, IMessageStream peer)
        {
            if (!callMessage.Header.ReplyExpected)
            {
                return;
            }

            string errMsg = String.Format("Method \"{0}\" with signature \"{1}\" on interface \"{2}\" doesn't exist",
                                           callMessage.Header.Member,
                                           callMessage.Header.Signature?.Value,
                                           callMessage.Header.Interface);

            SendErrorReply(callMessage, "org.freedesktop.DBus.Error.UnknownMethod", errMsg, peer);
        }

        private void SendErrorReply(Message incoming, string errorName, string errorMessage, IMessageStream peer)
        {
            SendMessage(MessageHelper.ConstructErrorReply(incoming, errorName, errorMessage), peer);
        }

        private uint GenerateSerial()
        {
            return (uint)Interlocked.Increment(ref _methodSerial);
        }

        private async Task<Message> CallMethodAsync(Message msg, bool checkConnected, bool checkReplyType)
        {
            msg.Header.ReplyExpected = true;
            var serial = GenerateSerial();
            msg.Header.Serial = serial;

            TaskCompletionSource<Message> pending = new TaskCompletionSource<Message>(_runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : default);
            lock (_gate)
            {
                if (checkConnected)
                {
                    ThrowIfNotConnected();
                }
                else
                {
                    ThrowIfNotConnecting();
                }
                _pendingMethods[msg.Header.Serial] = pending;
            }

            try
            {
                await _stream.SendMessageAsync(msg).ConfigureAwait(false);
            }
            catch
            {
                lock (_gate)
                {
                    _pendingMethods?.Remove(serial);
                }
                throw;
            }

            var reply = await pending.Task.ConfigureAwait(false);

            if (checkReplyType)
            {
                switch (reply.Header.MessageType)
                {
                    case MessageType.MethodReturn:
                        return reply;
                    case MessageType.Error:
                        string errorMessage = String.Empty;
                        if (reply.Header.Signature?.Value?.StartsWith("s", StringComparison.Ordinal) == true)
                        {
                            MessageReader reader = new MessageReader(reply, null);
                            errorMessage = reader.ReadString();
                        }
                        throw new DBusException(reply.Header.ErrorName, errorMessage);
                    default:
                        throw new ProtocolException("Got unexpected message of type " + reply.Header.MessageType + " while waiting for a MethodReturn or Error");
                }
            }

            return reply;
        }

        private void RemoveSignalHandler(SignalMatchRule rule, SignalHandler dlg)
        {
            lock (_gate)
            {
                if (_signalHandlers?.ContainsKey(rule) == true)
                {
                    var previous = _signalHandlers[rule];
                    _signalHandlers[rule] = (SignalHandler)Delegate.Remove(previous, dlg);
                    if (_signalHandlers[rule] == null)
                    {
                        _signalHandlers.Remove(rule);
                        if (ConnectionInfo.RemoteIsBus)
                        {
                            CallRemoveMatchRule(rule.ToString());
                        }
                        if (ShouldWatchNameOwner(rule.Sender))
                        {
                            ReleaseSenderTracking(rule.Sender);
                        }
                    }
                }
            }
        }

        private void RemoveNameOwnerWatcher(string key, OwnerChangedMatchRule rule, Action<ServiceOwnerChangedEventArgs, Exception> dlg)
        {
            lock (_gate)
            {
                if (_nameOwnerHandlers?.ContainsKey(key) == true)
                {
                    _nameOwnerHandlers[key] = (Action<ServiceOwnerChangedEventArgs, Exception>)Delegate.Remove(_nameOwnerHandlers[key], dlg);
                    if (_nameOwnerHandlers[key] == null)
                    {
                        _nameOwnerHandlers.Remove(key);
                        CallRemoveMatchRule(rule.ToString());
                    }
                }
            }
        }

        private void CallRemoveMatchRule(string rule)
        {
            EnsureExceptionObserved(CallMethodAsync(DBusServiceName, DBusObjectPath, DBusInterface, "RemoveMatch", rule));
        }

        private Task CallAddMatchRuleAsync(string rule)
        {
            return CallMethodAsync(DBusServiceName, DBusObjectPath, DBusInterface, "AddMatch", rule);
        }

        private Task CallMethodAsync(string destination, ObjectPath objectPath, string @interface, string method, string arg)
        {
            var header = new Header(MessageType.MethodCall)
            {
                Path = objectPath,
                Interface = @interface,
                Member = @method,
                Signature = Signature.StringSig,
                Destination = destination
            };
            var writer = new MessageWriter();
            writer.WriteString(arg);
            var message = new Message(
                header,
                writer.ToArray(),
                writer.UnixFds
            );
            return CallMethodAsync(message);
        }

        private Task WatchNameOwnerAsync(string name)
        {
            Debug.Assert(Monitor.IsEntered(_gate));
            Debug.Assert(name != DBusServiceName);
            // Watch for NameOwnerChanged to track when ownership changes
            // Set this up BEFORE getting the current owner to avoid race conditions
            if (_nameWatchers.TryGetValue(name, out NameWatcher existing))
            {
                existing.RefCount++;
                return existing.WatchNameOwnerTask;
            }

            var watcher = new NameWatcher
            {
                RefCount = 1,
                CurrentOwner = name,
                NameOwnerHandlerTask = WatchNameOwnerChangedAsync(name, (args, ex) => { }),
                // Get the current owner AFTER starting to watch for changes.
                GetNameOwnerTask = GetNameOwnerAsync(name)
            };
            _nameWatchers[name] = watcher;

            return watcher.WatchNameOwnerTask = AwaitWatchNameOwnerAsync(watcher);

            async Task AwaitWatchNameOwnerAsync(NameWatcher watcher)
            {
                try
                {
                    await watcher.NameOwnerHandlerTask.ConfigureAwait(false);
                    string currentOwner = await watcher.GetNameOwnerTask.ConfigureAwait(false);

                    lock (_gate)
                    {
                        // Update _nameToUniqueId mapping only if it's still pointing to itself to avoid overriding a name change.
                        if (watcher.CurrentOwner == name)
                        {
                            watcher.CurrentOwner = currentOwner;

                            if (currentOwner is not null)
                            {
                                if (!_uniqueIdToNames.TryGetValue(currentOwner, out List<string> ownedNames))
                                {
                                    ownedNames = new List<string>();
                                    _uniqueIdToNames[currentOwner] = ownedNames;
                                }
                                if (!ownedNames.Contains(name))
                                {
                                    ownedNames.Add(name);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    EnsureExceptionObserved(watcher.GetNameOwnerTask);
                    ReleaseSenderTracking(name);
                    throw;
                }
            }
        }

        private void ReleaseSenderTracking(string name)
        {
            lock (_gate)
            {
                if (_nameWatchers?.TryGetValue(name, out NameWatcher watcher) == true)
                {
                    if (--watcher.RefCount > 0)
                    {
                        return;
                    }
                    _nameWatchers.Remove(name);
                    string uniqueId = watcher.CurrentOwner;
                    if (uniqueId != null)
                    {
                        if (_uniqueIdToNames.TryGetValue(uniqueId, out List<string> ownedNames))
                        {
                            if (ownedNames.Remove(name) && ownedNames.Count == 0)
                            {
                                _uniqueIdToNames.Remove(uniqueId);
                            }
                        }
                    }
                    watcher.NameOwnerHandlerTask.ContinueWith(static t => t.Result.Dispose(), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }
            }
        }

        private async Task<string> GetNameOwnerAsync(string name)
        {
            var writer = new MessageWriter();
            writer.WriteString(name);

            Message callMsg = new Message(
                new Header(MessageType.MethodCall)
                {
                    Path = DBusObjectPath,
                    Interface = DBusInterface,
                    Member = "GetNameOwner",
                    Destination = DBusServiceName,
                    Signature = Signature.StringSig
                },
                writer.ToArray(),
                writer.UnixFds
            );

            try
            {
                Message reply = await CallMethodAsync(callMsg, checkConnected: true, checkReplyType: true).ConfigureAwait(false);
                var reader = new MessageReader(reply, null);
                return reader.ReadString();
            }
            catch (DBusException ex) when (ex.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner")
            {
                // Name has no current owner
                return null;
            }
        }

        private static void EnsureExceptionObserved(Task task)
        {
            task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        private void ThrowIfNotConnected()
            => Connection.ThrowIfNotConnected(_disposed, _state, _disconnectReason);

        private void ThrowIfNotConnecting()
            => Connection.ThrowIfNotConnecting(_disposed, _state, _disconnectReason);

        public string[] GetChildNames(ObjectPath path)
        {
            lock (_gate)
            {
                string[] childNames = null;
                if (_childNames.TryGetValue(path, out childNames))
                {
                    return childNames;
                }
                else
                {
                    return Array.Empty<string>();
                }
            }
        }

        public Task<string> StartServerAsync(string address)
        {
            var localServer = _stream as LocalServer;
            if (localServer == null)
            {
                throw new InvalidOperationException("Not a server connection.");
            }
            return localServer.StartAsync(address);
        }
    }
}

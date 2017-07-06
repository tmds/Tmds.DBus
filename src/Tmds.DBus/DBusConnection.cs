// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.Transports;

namespace Tmds.DBus
{
    class DBusConnection : IDBusConnection
    {
        private struct PendingSend
        {
            public Message Message;
            public TaskCompletionSource<bool> CompletionSource;
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
            public NameOwnerWatcherRegistration(DBusConnection dbusConnection, string key, OwnerChangedMatchRule rule, Action<ServiceOwnerChangedEventArgs> handler)
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
            private Action<ServiceOwnerChangedEventArgs> _handler;
            private string _key;
        }

        private class ServiceNameRegistration
        {
            public Action OnAquire;
            public Action OnLost;
            public SynchronizationContext SynchronizationContext;
        }

        private enum State
        {
            Created,
            Connecting,
            Connected,
            Disconnected,
            Disposed
        }

        public static readonly ObjectPath DBusObjectPath = new ObjectPath("/org/freedesktop/DBus");
        public const string DBusServiceName = "org.freedesktop.DBus";
        public const string DBusInterface = "org.freedesktop.DBus";
        private static readonly char[] s_dot = new[] { '.' };

        public static async Task<DBusConnection> OpenAsync(string address, Action<Exception> onDisconnect, CancellationToken cancellationToken)
        {
            var _entries = AddressEntry.ParseEntries(address);
            if (_entries.Length == 0)
            {
                throw new ArgumentException("No addresses were found", nameof(address));
            }

            Guid _serverId = Guid.Empty;
            IMessageStream stream = null;
            var index = 0;
            while (index < _entries.Length)
            {
                AddressEntry entry = _entries[index++];

                _serverId = entry.Guid;
                try
                {
                    stream = await Transport.OpenAsync(entry, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    if (index < _entries.Length)
                        continue;
                    throw;
                }

                break;
            }

            var dbusConnection = new DBusConnection(stream);
            await dbusConnection.ConnectAsync(onDisconnect, cancellationToken);
            return dbusConnection;
        }

        private readonly IMessageStream _stream;
        private readonly object _gate = new object();
        private readonly Dictionary<SignalMatchRule, SignalHandler> _signalHandlers = new Dictionary<SignalMatchRule, SignalHandler>();
        private readonly Dictionary<string, Action<ServiceOwnerChangedEventArgs>> _nameOwnerWatchers = new Dictionary<string, Action<ServiceOwnerChangedEventArgs>>();
        private Dictionary<uint, TaskCompletionSource<Message>> _pendingMethods = new Dictionary<uint, TaskCompletionSource<Message>>();
        private readonly Dictionary<ObjectPath, MethodHandler> _methodHandlers = new Dictionary<ObjectPath, MethodHandler>();
        private readonly Dictionary<ObjectPath, string[]> _childNames = new Dictionary<ObjectPath, string[]>();
        private readonly Dictionary<string, ServiceNameRegistration> _serviceNameRegistrations = new Dictionary<string, ServiceNameRegistration>();

        private State _state = State.Created;
        private string _localName;
        private bool? _remoteIsBus;
        private Action<Exception> _onDisconnect;
        private Exception _disconnectReason;
        private int _methodSerial;
        private ConcurrentQueue<PendingSend> _sendQueue;
        private SemaphoreSlim _sendSemaphore;

        public string LocalName => _localName;
        public bool? RemoteIsBus => _remoteIsBus;

        internal DBusConnection(IMessageStream stream)
        {
            _stream = stream;
            _sendQueue = new ConcurrentQueue<PendingSend>();
            _sendSemaphore = new SemaphoreSlim(1);
        }

        public async Task ConnectAsync(Action<Exception> onDisconnect = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_gate)
            {
                if (_state != State.Created)
                {
                    throw new InvalidOperationException("Unable to connect");
                }
                _state = State.Connecting;
            }

            ReceiveMessages();

            _localName = await CallHelloAsync();
            _remoteIsBus = !string.IsNullOrEmpty(_localName);

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

        public void Dispose()
        {
            DoDisconnect(State.Disposed, null);
        }

        public Task<Message> CallMethodAsync(Message msg)
        {
            return CallMethodAsync(msg, checkConnected: true, checkReplyType: true);
        }

        public void EmitSignal(Message message)
        {
            message.Header.Serial = GenerateSerial();
            SendMessageAsync(message);
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

        public async Task<IDisposable> WatchSignalAsync(ObjectPath path, string @interface, string signalName, SignalHandler handler)
        {
            SignalMatchRule rule = new SignalMatchRule()
            {
                Interface = @interface,
                Member = signalName,
                Path = path
            };

            Task task = null;
            lock (_gate)
            {
                ThrowIfNotConnected();
                if (_signalHandlers.ContainsKey(rule))
                {
                    _signalHandlers[rule] = (SignalHandler)Delegate.Combine(_signalHandlers[rule], handler);
                    task = Task.CompletedTask;
                }
                else
                {
                    _signalHandlers[rule] = handler;
                    if (_remoteIsBus == true)
                    {
                        task = CallAddMatchRuleAsync(rule.ToString());
                    }
                }
            }
            SignalHandlerRegistration registration = new SignalHandlerRegistration(this, rule, handler);
            try
            {
                if (task != null)
                {
                    await task;
                }
            }
            catch
            {
                registration.Dispose();
                throw;
            }
            return registration;
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
                var reply = await CallRequestNameAsync(name, options);
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

        public async Task<IDisposable> WatchNameOwnerChangedAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler)
        {
            var rule = new OwnerChangedMatchRule(serviceName);
            string key = serviceName;

            Task task = null;
            lock (_gate)
            {
                ThrowIfNotConnected();
                ThrowIfRemoteIsNotBus();

                if (_nameOwnerWatchers.ContainsKey(key))
                {
                    _nameOwnerWatchers[key] = (Action<ServiceOwnerChangedEventArgs>)Delegate.Combine(_nameOwnerWatchers[key], handler);
                    task = Task.CompletedTask;
                }
                else
                {
                    _nameOwnerWatchers[key] = handler;
                    task = CallAddMatchRuleAsync(rule.ToString());
                }
            }
            NameOwnerWatcherRegistration registration = new NameOwnerWatcherRegistration(this, key, rule, handler);
            try
            {
                await task;
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
            if (RemoteIsBus != true)
            {
                throw new InvalidOperationException("The remote peer is not a bus");
            }
        }

        private async void SendPendingMessages()
        {
            try
            {
                await _sendSemaphore.WaitAsync();
                PendingSend pendingSend;
                while (_sendQueue.TryDequeue(out pendingSend))
                {
                    try
                    {
                        await _stream.SendMessageAsync(pendingSend.Message);
                        pendingSend.CompletionSource.SetResult(true);
                    }
                    catch (System.Exception e)
                    {
                        pendingSend.CompletionSource.SetException(e);
                    }
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private Task SendMessageAsync(Message message)
        {
            var tcs = new TaskCompletionSource<bool>();
            var pendingSend = new PendingSend()
            {
                Message = message,
                CompletionSource = tcs
            };
            _sendQueue.Enqueue(pendingSend);
            SendPendingMessages();
            return tcs.Task;
        }

        private async void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    Message msg = await _stream.ReceiveMessageAsync();
                    if (msg == null)
                    {
                        throw new IOException("Connection closed by peer");
                    }
                    HandleMessage(msg);
                }
            }
            catch (Exception e)
            {
                DoDisconnect(State.Disposed, e);
            }
        }

        private void HandleMessage(Message msg)
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
                    HandleMethodCall(msg);
                    break;
                case MessageType.Signal:
                    HandleSignal(msg);
                    break;
                case MessageType.Error:
                    string errMsg = String.Empty;
                    if (msg.Header.Signature.Value.Value.StartsWith("s"))
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
                            Action<ServiceOwnerChangedEventArgs> watchers = null;
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
                                foreach (var key in keys)
                                {
                                    Action<ServiceOwnerChangedEventArgs> keyWatchers;
                                    _nameOwnerWatchers.TryGetValue(key, out keyWatchers);
                                    if (keyWatchers != null)
                                    {
                                        watchers += keyWatchers;
                                    }
                                }
                            }
                            watchers?.Invoke(new ServiceOwnerChangedEventArgs(serviceName, oldOwner, newOwner));
                            return;
                        }
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            SignalMatchRule rule = new SignalMatchRule()
            {
                Interface = msg.Header.Interface,
                Member = msg.Header.Member,
                Path = msg.Header.Path.Value
            };

            SignalHandler signalHandler;
            lock (_gate)
            {
                if (_signalHandlers.TryGetValue(rule, out signalHandler) && signalHandler != null)
                {
                    try
                    {
                        signalHandler(msg);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Signal handler for " + msg.Header.Interface + "." + msg.Header.Member + " threw an exception", e);
                    }
                }
            }
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

        private void DoDisconnect(State nextState, Exception disconnectReason)
        {
            Dictionary<uint, TaskCompletionSource<Message>> pendingMethods = null;
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

                _state = nextState;
                _stream.Dispose();
                _disconnectReason = disconnectReason;
                pendingMethods = _pendingMethods;
                _pendingMethods = null;
                _signalHandlers.Clear();
                _nameOwnerWatchers.Clear();
                _serviceNameRegistrations.Clear();
            }

            foreach (var tcs in pendingMethods.Values)
            {
                if (disconnectReason != null)
                {
                    tcs.SetException(new DisconnectedException(disconnectReason));
                }
                else
                {
                    tcs.SetException(new ObjectDisposedException(typeof(Connection).FullName));
                }
            }
            if (_onDisconnect != null)
            {
                _onDisconnect(disconnectReason);
            }
        }

        private void SendMessage(Message message)
        {
            if (message.Header.Serial == 0)
            {
                message.Header.Serial = GenerateSerial();
            }
            SendMessageAsync(message);
        }

        private async void HandleMethodCall(Message methodCall)
        {
            switch (methodCall.Header.Interface)
            {
                case "org.freedesktop.DBus.Peer":
                    switch (methodCall.Header.Member)
                    {
                        case "Ping":
                        {
                            SendMessage(MessageHelper.ConstructReply(methodCall));
                            return;
                        }
                        case "GetMachineId":
                        {
                            SendMessage(MessageHelper.ConstructReply(methodCall, Environment.MachineId));
                            return;
                        }
                    }
                    break;
            }

            MethodHandler methodHandler;
            if (_methodHandlers.TryGetValue(methodCall.Header.Path.Value, out methodHandler))
            {
                var reply = await methodHandler(methodCall);
                reply.Header.ReplySerial = methodCall.Header.Serial;
                reply.Header.Destination = methodCall.Header.Sender;
                SendMessage(reply);
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
                        foreach (var child in childNames)
                        {
                            writer.WriteChildNode(child);
                        }
                        writer.WriteNodeEnd();
                        
                        var xml = writer.ToString();
                        SendMessage(MessageHelper.ConstructReply(methodCall, xml));
                    }
                }
                SendUnknownMethodError(methodCall);
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

            Message reply = await CallMethodAsync(callMsg, checkConnected: false, checkReplyType: false);

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

            Message reply = await CallMethodAsync(callMsg, checkConnected: true, checkReplyType: true);

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

            Message reply = await CallMethodAsync(callMsg, checkConnected: true, checkReplyType: true);

            var reader = new MessageReader(reply, null);
            var rv = reader.ReadUInt32();
            return (ReleaseNameReply)rv;
        }

        private void SendUnknownMethodError(Message callMessage)
        {
            if (!callMessage.Header.ReplyExpected)
            {
                return;
            }

            string errMsg = String.Format("Method \"{0}\" with signature \"{1}\" on interface \"{2}\" doesn't exist",
                                           callMessage.Header.Member,
                                           callMessage.Header.Signature?.Value,
                                           callMessage.Header.Interface);

            SendErrorReply(callMessage, "org.freedesktop.DBus.Error.UnknownMethod", errMsg);
        }

        private void SendErrorReply(Message incoming, string errorName, string errorMessage)
        {
            SendMessage(MessageHelper.ConstructErrorReply(incoming, errorName, errorMessage));
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

            TaskCompletionSource<Message> pending = new TaskCompletionSource<Message>();
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
                await SendMessageAsync(msg);
            }
            catch
            {
                lock (_gate)
                {
                    _pendingMethods?.Remove(serial);
                }
                throw;
            }

            var reply = await pending.Task;

            if (checkReplyType)
            {
                switch (reply.Header.MessageType)
                {
                    case MessageType.MethodReturn:
                        return reply;
                    case MessageType.Error:
                        string errorMessage = String.Empty;
                        if (reply.Header.Signature?.Value?.StartsWith("s") == true)
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
                if (_signalHandlers.ContainsKey(rule))
                {
                    _signalHandlers[rule] = (SignalHandler)Delegate.Remove(_signalHandlers[rule], dlg);
                    if (_signalHandlers[rule] == null)
                    {
                        _signalHandlers.Remove(rule);
                        if (_remoteIsBus == true)
                        {
                            CallRemoveMatchRule(rule.ToString());
                        }
                    }
                }
            }
        }

        private void RemoveNameOwnerWatcher(string key, OwnerChangedMatchRule rule, Action<ServiceOwnerChangedEventArgs> dlg)
        {
            lock (_gate)
            {
                if (_nameOwnerWatchers.ContainsKey(key))
                {
                    _nameOwnerWatchers[key] = (Action<ServiceOwnerChangedEventArgs>)Delegate.Remove(_nameOwnerWatchers[key], dlg);
                    if (_nameOwnerWatchers[key] == null)
                    {
                        _nameOwnerWatchers.Remove(key);
                        CallRemoveMatchRule(rule.ToString());
                    }
                }
            }
        }

        private void CallRemoveMatchRule(string rule)
        {
            var reply = CallMethodAsync(DBusServiceName, DBusObjectPath, DBusInterface, "RemoveMatch", rule);
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

        private void ThrowIfNotConnecting()
        {
            if (_state == State.Disconnected)
            {
                throw new DisconnectedException(_disconnectReason);
            }
            else if (_state == State.Created)
            {
                throw new InvalidOperationException("Not Connected");
            }
            else if (_state == State.Connected)
            {
                throw new InvalidOperationException("Already Connected");
            }
            else if (_state == State.Disposed)
            {
                throw new ObjectDisposedException(typeof(Connection).FullName);
            }
        }

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
    }
}
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace Tmds.DBus.Protocol;

class InnerConnection : IDisposable
{
    private delegate void MessageReceivedHandler(Exception? exception, Message? message, object? state);

    sealed class MyValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core;
        private volatile bool _continuationSet;

        public void SetResult(T result)
        {
            // Ensure we complete the Task from the read loop.
            SpinWait wait = new();
            while (!_continuationSet)
            {
                wait.SpinOnce();
            }
            _core.SetResult(result);
        }

        public void SetException(Exception exception) => _core.SetException(exception);

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _core.OnCompleted(continuation, state, token, flags);
            _continuationSet = true;
        }

        T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);

        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
    }

    enum DBusConnectionState
    {
        Created,
        Connecting,
        Connected,
        Disconnected
    }

    delegate void MessageHandlerDelegate(Exception? exception, Message? message, object? state1, object? state2, object? state3);

    readonly struct MessageHandler
    {
        public MessageHandler(MessageHandlerDelegate handler, object? state1 = null, object? state2 = null, object? state3 = null)
        {
            _delegate = handler;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
        }

        public void Invoke(Exception? exception, Message? message)
        {
            _delegate(exception, message, _state1, _state2, _state3);
        }

        public bool HasValue => _delegate is not null;

        private readonly MessageHandlerDelegate _delegate;
        private readonly object? _state1;
        private readonly object? _state2;
        private readonly object? _state3;
    }

    sealed class SenderName
    {
        private object? _object;

        public ReadOnlySpan<byte> Name => _object as byte[];

        public Task ResolveUniqueName => _object as Task ?? Task.CompletedTask;

        public SenderName(byte[] name)
        {
            _object = name;
        }

        public SenderName()
        { }

        public void SetName(byte[] name)
        {
            _object = name;
        }

        internal void SetTask(Task task)
        {
            _object = task;
        }
    }

    private readonly Lock _gate = new();
    private readonly DBusConnection _parentDBusConnection;
    private readonly Dictionary<uint, MessageHandler> _pendingCalls;
    private readonly CancellationTokenSource _connectCts;
    private readonly Dictionary<string, Watcher> _watchers;
    private readonly Dictionary<byte[], SenderName> _nameOwners; // maps a (well-known) name to a unique name (that owns the well-known name).
    private readonly List<Observer> _matchedObservers;
    private readonly PathNodeDictionary _pathNodes;
    private readonly string _machineId;
    private Dictionary<string, ServiceNameRegistration?>? _serviceNameRegistrations;

    private IMessageStream? _messageStream;
    private DBusConnectionState _state;
    private Exception? _disconnectReason;
    private string? _localName;
    private TaskCompletionSource<Exception?>? _disconnectedTcs;
    private CancellationTokenSource _abortedCts;
    private bool _isMonitor;
    private Action<Exception?, DisposableMessage>? _monitorHandler;

    public string? UniqueName => _localName;

    public Exception DisconnectReason
    {
        get => _disconnectReason ?? new ObjectDisposedException(GetType().FullName);
        set => Interlocked.CompareExchange(ref _disconnectReason, value, null);
    }

    public bool RemoteIsBus => _localName is not null;

    public InnerConnection(DBusConnection parent, string machineId)
    {
        _parentDBusConnection = parent;
        _connectCts = new();
        _pendingCalls = new();
        _watchers = new();
        _matchedObservers = new();
        _pathNodes = new();
        _machineId = machineId;
        _abortedCts = new();
        _nameOwners = new(new ByteArrayComparer());
    }

    // For tests.
    internal void Connect(IMessageStream stream)
    {
        _messageStream = stream;

        stream.ReceiveMessages(
                    static (Exception? exception, Message? message, InnerConnection connection) =>
                        connection.HandleMessages(exception, message), this);

        _state = DBusConnectionState.Connected;
    }

    public async ValueTask ConnectAsync(string address, string? userId, bool supportsFdPassing, CancellationToken cancellationToken)
    {
        _state = DBusConnectionState.Connecting;
        Exception? firstException = null;

        AddressParser.AddressEntry addr = default;
        while (AddressParser.TryGetNextEntry(address, ref addr))
        {
            Socket? socket = null;
            EndPoint? endpoint = null;
            Guid guid = default;

            if (AddressParser.IsType(addr, "unix"))
            {
                AddressParser.ParseUnixProperties(addr, out string path, out guid);
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                endpoint = new UnixDomainSocketEndPoint(path);
            }
            else if (AddressParser.IsType(addr, "tcp"))
            {
                AddressParser.ParseTcpProperties(addr, out string host, out int? port, out guid);
                if (!port.HasValue)
                {
                    throw new ArgumentException("port");
                }
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                endpoint = new DnsEndPoint(host, port.Value);
            }

            if (socket is null)
            {
                continue;
            }

            try
            {
                await socket.ConnectAsync(endpoint!, cancellationToken).ConfigureAwait(false);

                MessageStream stream;
                lock (_gate)
                {
                    if (_state != DBusConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _messageStream = stream = new MessageStream(socket);
                }

                await stream.DoClientAuthAsync(guid, userId, supportsFdPassing).ConfigureAwait(false);

                stream.ReceiveMessages(
                    static (Exception? exception, Message? message, InnerConnection connection) =>
                        connection.HandleMessages(exception, message), this);

                lock (_gate)
                {
                    if (_state != DBusConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _state = DBusConnectionState.Connected;
                }

                _localName = await GetLocalNameAsync().ConfigureAwait(false);

                return;
            }
            catch (Exception exception)
            {
                socket.Dispose();
                firstException ??= exception;
            }
        }

        if (firstException is not null)
        {
            throw firstException;
        }

        throw new ArgumentException("No addresses were found", nameof(address));
    }

    public async ValueTask ConnectAsync(Stream stream, string? userId, CancellationToken cancellationToken)
    {
        _state = DBusConnectionState.Connecting;

        try
        {
            MessageStream messageStream = new MessageStream(stream);
            _messageStream = messageStream;

            await messageStream.DoClientAuthAsync(default(Guid), userId, supportsFdPassing: false).ConfigureAwait(false);

            messageStream.ReceiveMessages(
                static (Exception? exception, Message? message, InnerConnection connection) =>
                    connection.HandleMessages(exception, message), this);

            lock (_gate)
            {
                if (_state != DBusConnectionState.Connecting)
                {
                    throw new DisconnectedException(DisconnectReason);
                }
                _state = DBusConnectionState.Connected;
            }

            _localName = await GetLocalNameAsync().ConfigureAwait(false);
        }
        catch
        {
            stream.Dispose();

            throw;
        }
    }

    private async Task<string?> GetLocalNameAsync()
    {
        MyValueTaskSource<string?> vts = new();

        CallMethod(
            message: CreateHelloMessage(),
            static (Exception? exception, Message? message, object? state) =>
            {
                var vtsState = (MyValueTaskSource<string?>)state!;

                if (exception is not null)
                {
                    vtsState.SetException(exception);
                    return;
                }
                Debug.Assert(message is not null);
                if (message.MessageType == MessageType.MethodReturn)
                {
                    vtsState.SetResult(message.GetBodyReader().ReadString());
                }
                else
                {
                    vtsState.SetResult(null);
                }
            }, vts);

        return await new ValueTask<string?>(vts, token: 0).ConfigureAwait(false);

        MessageBuffer CreateHelloMessage()
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "Hello");

            return writer.CreateMessage();
        }
    }

    private async void HandleMessages(Exception? exception, Message? message)
    {
        if (exception is not null)
        {
            Disconnect(exception);
        }
        else
        {
            Debug.Assert(message is not null);
            try
            {
                MessageHandler pendingCall = default;
                IPathMethodHandler? methodHandler = null;
                Action<Exception?, DisposableMessage>? monitor = null;
                MethodContext? methodContext = null;

                lock (_gate)
                {
                    if (_state == DBusConnectionState.Disconnected)
                    {
                        return;
                    }

                    monitor = _monitorHandler;

                    if (monitor is null)
                    {
                        if (message.MessageType == MessageType.Signal &&
                            message.Interface.SequenceEqual("org.freedesktop.DBus"u8) &&
                            message.Sender.SequenceEqual("org.freedesktop.DBus"u8))
                        {
                            HandleDBusInterfaceSignal(message);
                        }

                        if (message.ReplySerial.HasValue)
                        {
                            _pendingCalls.Remove(message.ReplySerial.Value, out pendingCall);
                        }

                        foreach (var watcher in _watchers.Values)
                        {
                            if (watcher.Observes(message))
                            {
                                _matchedObservers.AddRange(watcher.Observers);
                            }
                        }

                        if (message.MessageType == MessageType.MethodCall)
                        {
                            // This is a small object. We don't pool it to avoid re-use issues.
                            methodContext = new MethodContext(_parentDBusConnection, message, _abortedCts.Token);

                            if (message.PathIsSet)
                            {
                                _pathNodes.TryGetValue(message.PathAsString, out methodHandler, out PathNode? node);
                                // Track the child name list for nodes that don't have handlers to reply below
                                // or for nodes that don't handle child paths to include them when they call ReplyIntrospectXml.
                                // Handlers that handle child paths are expected to provide the child names when calling ReplyIntrospectXml.
                                if (node is not null && methodHandler?.HandlesChildPaths != true && methodContext.IsDBusIntrospectRequest)
                                {
                                    node.SetIntrospectChildNames(methodContext);
                                }
                            }
                        }
                    }
                }

                if (monitor is not null)
                {
                    lock (monitor)
                    {
                        if (_monitorHandler is not null)
                        {
                            monitor(null, new DisposableMessage(message));
                        }
                    }
                }
                else
                {
                    if (_matchedObservers.Count != 0)
                    {
                        foreach (var observer in _matchedObservers)
                        {
                            observer.Emit(message);
                        }
                        _matchedObservers.Clear();
                    }

                    if (pendingCall.HasValue)
                    {
                        pendingCall.Invoke(null, message);
                    }

                    if (methodContext is not null)
                    {
                        // Suppress methodContext nullability warnings.
                        try
                        {
                            if (methodContext.IsPeerInterface)
                            {
                                HandlePeerInterface(methodContext);
                            }
                            else if (methodHandler is not null)
                            {
                                await methodHandler.HandleMethodAsync(methodContext).ConfigureAwait(false);
                            }
                            else if (methodContext.IntrospectChildNames is not null)
                            {
                                methodContext.ReplyIntrospectXml(interfaceXmls: []);
                            }
                        }
                        finally
                        {
                            if (!methodContext.DisposesAsynchronously)
                            {
                                methodContext.CanDispose = false; // Ensure the context is no longer disposable by the user.
                                methodContext.Dispose(force: true);
                            }
                        }
                    }

                    message.DecrementRef();
                }
            }
            catch (Exception ex)
            {
                Disconnect(ex);
            }
        }
    }

    private void HandlePeerInterface(MethodContext context)
    {
        var request = context.Request;
        if (request.Member.SequenceEqual("Ping"u8))
        {
            using var writer = context.CreateReplyWriter(null);
            context.Reply(writer.CreateMessage());
        }
        else if (request.Member.SequenceEqual("GetMachineId"u8))
        {
            using var writer = context.CreateReplyWriter("s");
            writer.WriteString(_machineId);
            context.Reply(writer.CreateMessage());
        }
    }

    public void UpdateMethodHandlers<T>(Action<IMethodHandlerDictionary, T> update, T state)
    {
        lock (_gate)
        {
            update(_pathNodes, state);
        }
    }

    public void Dispose()
    {
        Action<Exception?, DisposableMessage>? monitor = null;

        lock (_gate)
        {
            if (_state == DBusConnectionState.Disconnected)
            {
                return;
            }
            _state = DBusConnectionState.Disconnected;
            monitor = _monitorHandler;
            _serviceNameRegistrations = null;
        }

        Exception disconnectReason = DisconnectReason;

        _messageStream?.Close(disconnectReason);

        _abortedCts.Cancel();

        if (_pendingCalls is not null)
        {
            foreach (var pendingCall in _pendingCalls.Values)
            {
                pendingCall.Invoke(new DisconnectedException(disconnectReason), null!);
            }
            _pendingCalls.Clear();
        }

        foreach (var watcher in _watchers.Values)
        {
            foreach (var observer in watcher.Observers)
            {
                bool emitException = !object.ReferenceEquals(disconnectReason, DBusConnection.DisposedException) ||
                                     observer.EmitOnConnectionDispose;
                Exception? exception = emitException ? new DisconnectedException(disconnectReason) : null;
                observer.Dispose(exception, removeObserver: false);
            }
            if (watcher.SubscribeTask is not null)
            {
                EnsureExceptionObserved(watcher.SubscribeTask);
            }
        }
        _watchers.Clear();
        _nameOwners.Clear();

        if (monitor is not null)
        {
            lock (monitor)
            {
                _monitorHandler = null;
                monitor(new DisconnectedException(disconnectReason), new DisposableMessage(null));
            }
        }

        _disconnectedTcs?.SetResult(GetWaitForDisconnectException());
    }

    private void CallMethod(MessageBuffer message, MessageReceivedHandler returnHandler, object? state)
    {
        MessageHandlerDelegate fn = static (Exception? exception, Message? message, object? state1, object? state2, object? state3) =>
        {
            ((MessageReceivedHandler)state1!)(exception, message, state2);
        };
        MessageHandler handler = new(fn, returnHandler, state);

        CallMethod(message, handler);
    }

    private void CallMethod(MessageBuffer message, MessageHandler handler)
    {
        bool messageSent = false;
        try
        {
            lock (_gate)
            {
                if (_state != DBusConnectionState.Connected)
                {
                    throw new DisconnectedException(DisconnectReason!);
                }
                if (_isMonitor)
                {
                    throw new InvalidOperationException("Cannot send messages on monitor connection.");
                }
                if ((message.MessageFlags & MessageFlags.NoReplyExpected) == 0)
                {
                    _pendingCalls.Add(message.Serial, handler);
                }
            }

            messageSent = _messageStream!.TrySendMessage(message);
        }
        finally
        {
            if (!messageSent)
            {
                message.ReturnToPool();
            }
        }
    }

    public async Task<T> CallMethodAsync<T>(MessageBuffer message, MessageValueReader<T> valueReader, object? state = null)
    {
        MessageHandlerDelegate fn = static (Exception? exception, Message? message, object? state1, object? state2, object? state3) =>
        {
            var valueReaderState = (MessageValueReader<T>)state1!;
            var vtsState = (MyValueTaskSource<T>)state2!;

            if (exception is not null)
            {
                vtsState.SetException(exception);
                return;
            }
            Debug.Assert(message is not null);
            if (message.MessageType == MessageType.MethodReturn)
            {
                try
                {
                    vtsState.SetResult(valueReaderState(message, state3));
                }
                catch (Exception ex)
                {
                    vtsState.SetException(ex);
                }
            }
            else if (message.MessageType == MessageType.Error)
            {
                vtsState.SetException(CreateDBusExceptionForErrorMessage(message));
            }
            else
            {
                vtsState.SetException(new ProtocolException($"Unexpected reply type: {message.MessageType}."));
            }
        };

        MyValueTaskSource<T> vts = new();
        MessageHandler handler = new(fn, valueReader, vts, state);

        CallMethod(message, handler);

        return await new ValueTask<T>(vts, 0).ConfigureAwait(false);
    }

    public async Task CallMethodAsync(MessageBuffer message)
    {
        MyValueTaskSource<object?> vts = new();

        CallMethod(message, static (Exception? exception, Message? message, object? state) => CompleteCallValueTaskSource(exception, message, state), vts);

        await new ValueTask(vts, 0).ConfigureAwait(false);
    }

    private static void CompleteCallValueTaskSource(Exception? exception, Message? message, object? vts)
    {
        var vtsState = (MyValueTaskSource<object?>)vts!;

        if (exception is not null)
        {
            vtsState.SetException(exception);
            return;
        }
        Debug.Assert(message is not null);
        if (message.MessageType == MessageType.MethodReturn)
        {
            vtsState.SetResult(null);
        }
        else if (message.MessageType == MessageType.Error)
        {
            vtsState.SetException(CreateDBusExceptionForErrorMessage(message));
        }
        else
        {
            vtsState.SetException(new ProtocolException($"Unexpected reply type: {message.MessageType}."));
        }
    }

    private static DBusException CreateDBusExceptionForErrorMessage(Message message)
    {
        string errorName = message.ErrorNameAsString ?? "<<No ErrorName>>.";
        string errMessage = errorName;
        if (message.SignatureIsSet && message.Signature.Length > 0 && (DBusType)message.Signature[0] == DBusType.String)
        {
            errMessage = message.GetBodyReader().ReadString();
        }
        return new DBusException(errorName, errMessage);
    }

    public async Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules)
    {
        Task reply;

        lock (_gate)
        {
            if (_state != DBusConnectionState.Connected)
            {
                throw new DisconnectedException(DisconnectReason!);
            }
            if (!RemoteIsBus)
            {
                throw new InvalidOperationException("The remote is not a bus.");
            }
            if (_watchers.Count != 0)
            {
                throw new InvalidOperationException("The connection has observers.");
            }
            if (_pendingCalls.Count != 0)
            {
                throw new InvalidOperationException("The connection has pending method calls.");
            }
            if (_serviceNameRegistrations?.Count > 0)
            {
                throw new InvalidOperationException("The connection has service name registrations.");
            }

            HashSet<string>? ruleStrings = null;
            if (rules is not null)
            {
                ruleStrings = new();
                foreach (var rule in rules)
                {
                    ruleStrings.Add(rule.ToString());
                }
            }

            reply = CallMethodAsync(CreateMessage(ruleStrings));
            _isMonitor = true;
        }

        try
        {
            await reply.ConfigureAwait(false);
            lock (_gate)
            {
                _messageStream!.BecomeMonitor();
                _monitorHandler = handler;
            }
        }
        catch
        {
            lock (_gate)
            {
                _isMonitor = false;
            }

            throw;
        }

        MessageBuffer CreateMessage(IEnumerable<string>? rules)
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusConnection.DBusServiceName,
                path: DBusConnection.DBusObjectPath,
                @interface: "org.freedesktop.DBus.Monitoring",
                signature: "asu",
                member: "BecomeMonitor");
            writer.WriteArray(rules ?? Array.Empty<string>());
            writer.WriteUInt32(0);
            return writer.CreateMessage();
        }
    }

    public async ValueTask<IDisposable> AddMatchAsync<T>(SynchronizationContext? synchronizationContext, MatchRule rule, MessageValueReader<T> valueReader, Action<Exception?, T, object?, object?> valueHandler, object? readerState, object? handlerState, ObserverFlags flags)
    {
        if (!RemoteIsBus)
        {
            flags |= ObserverFlags.NoSubscribe;
        }
        Observer observer = Observer.Create(synchronizationContext, valueReader, valueHandler, readerState, handlerState, flags);
        await AddWatcherUserAsync(rule.Data, observer);
        return observer;
    }

    private ValueTask<Watcher> AddWatcherUserAsync(in MatchRuleData data, Observer? observer)
    {
        // When we're making a match for a (service) name we need to match it with the unique name that is currently owning that name.
        // To do that, we need another AddMatch to watch changes for the name.
        // This associated AddMatch is tracked with the WatchNameOwnerTask property. To that Watcher we are a non-observing subscriber.
        // This watches for changes, we also need to get the current name.
        // The SenderName class tracks the resolving of that name or the current value when it is known.
        // It is stored in the _nameOwners dictionary which maps the name to the owner.
        // Items from that dictionary are removed when the associated AddMatch is removed (or when we fail to create one).
        Watcher? watcher;
        string ruleString;
        MessageBuffer? addMatchMessage = null;
        bool subscribe;
        Task resolveUniqueNameTask = Task.CompletedTask;
        lock (_gate)
        {
            if (_state != DBusConnectionState.Connected)
            {
                throw new DisconnectedException(DisconnectReason!);
            }
            if (_isMonitor)
            {
                throw new InvalidOperationException("Cannot add subscriptions on a monitor connection.");
            }

            // The rest of this block doesn't throw.
            // Throwing happens when we await after releasing the lock.

            ruleString = data.GetRuleString();

            SenderName? sender = null;
            if (!_watchers.TryGetValue(ruleString, out watcher))
            {
                Task<Watcher>? watchNameOwnerTask = null;
                if (data.Sender is not null)
                {
                    byte[] senderBytes = Encoding.UTF8.GetBytes(data.Sender);
                    if (!RemoteIsBus)
                    {
                        Debug.Assert(sender == null);
                    }
                    // Is this a bus name we should map to a unique name for filtering.
                    else if (senderBytes.Length > 0 && senderBytes[0] != (byte)':' && data.Sender != DBusConnection.DBusServiceName)
                    {
                        watchNameOwnerTask = WatchNameOwnerAsync(this, data.Sender!);
                        if (!TryGetSenderName(senderBytes, out sender))
                        {
                            sender = new SenderName();
                            resolveUniqueNameTask = ResolveUniqueNameAsync(this, sender, data.Sender);
                            sender.SetTask(resolveUniqueNameTask);
                            _nameOwners[senderBytes] = sender;
                        }
                        else
                        {
                            resolveUniqueNameTask = sender.ResolveUniqueName;
                        }
                    }
                    else
                    {
                        sender = new SenderName(senderBytes);
                    }
                }
                watcher = new Watcher(this, ruleString, data, sender, watchNameOwnerTask);
                _watchers.Add(ruleString, watcher);
            }
            else
            {
                sender = watcher.Sender;
                if (sender is not null)
                {
                    resolveUniqueNameTask = sender.ResolveUniqueName;
                }
            }

            if (observer is not null)
            {
                observer.Watcher = watcher;
                watcher.Observers.Add(observer);
                subscribe = observer.Subscribes;
            }
            else
            {
                Debug.Assert(watcher.IsNameOwnerWatcher);
                watcher.AddNonObserverSubscriber();
                subscribe = true;
            }

            bool sendMessage = subscribe && watcher.SubscribeTask is null;
            if (sendMessage)
            {
                var subscribeTcs = new MyValueTaskSource<object?>();
                watcher.SubscribeTask = new ValueTask<object?>(subscribeTcs, token: 0).AsTask();
                addMatchMessage = CreateAddMatchMessage(watcher.RuleString);
                MessageHandlerDelegate fn = static (Exception? exception, Message? message, object? state1, object? state2, object? state3) =>
                {
                    var mm = (Watcher)state1!;
                    var vtsState = (MyValueTaskSource<object?>)state2!;
                    if (message is not null)
                    {
                        if (message.MessageType == MessageType.MethodReturn)
                        {
                            mm.HasSubscribed = true;
                        }
                    }
                    CompleteCallValueTaskSource(exception, message, vtsState);
                };
                _pendingCalls.Add(addMatchMessage.Serial, new(fn, watcher, subscribeTcs));
                _messageStream!.TrySendMessage(addMatchMessage);
            }
        }

        return AwaitMatchAsync(this, watcher, resolveUniqueNameTask, subscribe, observer);

        static async ValueTask<Watcher> AwaitMatchAsync(InnerConnection connection, Watcher watcher, Task resolveUniqueNameTask, bool subscribe, Observer? observer)
        {
            try
            {
                // We might throw before we've awaited all tasks.
                // For the first one (AddMatchAsyncOwnerTask) this isn't an issue.
                // For resolveUniqueNameTask, and SubscribeTask we call the 'EnsureExceptionObserved' method to avoid unhandled exceptions from these Tasks.
                if (watcher.AddMatchAsyncOwnerTask is not null)
                {
                    Debug.Assert(observer is not null); // the observer is null when we're watching an owner, and owner watchers don't have owners to watch (because they watch the bus itself).
                    await watcher.AddMatchAsyncOwnerTask.ConfigureAwait(false);

                    await resolveUniqueNameTask.ConfigureAwait(false);
                }

                if (subscribe)
                {
                    await watcher.SubscribeTask!.ConfigureAwait(false);
                }
            }
            catch
            {
                EnsureExceptionObserved(resolveUniqueNameTask);

                if (observer is not null)
                {
                    bool disposedObserver = observer.Dispose(exception: null);
                    // If something had already disposed the observer, we won't throw and just return it.
                    // The handler took care of the exception (if ObserverFlags registered for it).
                    if (disposedObserver)
                    {
                        throw;
                    }
                }
                else
                {
                    connection.RemoveWatcherUser(watcher, null);
                    throw;
                }
            }

            return watcher;
        }

        static async Task ResolveUniqueNameAsync(InnerConnection connection, SenderName senderName, string name)
        {
            byte[] uniqueName = await connection.GetNameOwnerAsync(name).ConfigureAwait(false);
            lock (connection._gate)
            {
                senderName.SetName(uniqueName);
            }
        }

        static Task<Watcher> WatchNameOwnerAsync(InnerConnection connection, string name)
        {
            Debug.Assert(connection._gate.IsHeldByCurrentThread);
            Debug.Assert(name != DBusConnection.DBusServiceName);
            MatchRuleData data = new MatchRuleData
            {
                MessageType = MessageType.Signal,
                Interface = "org.freedesktop.DBus",
                Member = "NameOwnerChanged",
                Sender = "org.freedesktop.DBus",
                Arg0 = name
            };
            return connection.AddWatcherUserAsync(data, observer: null).AsTask();
        }

        MessageBuffer CreateAddMatchMessage(string ruleString)
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "AddMatch",
                signature: "s");

            writer.WriteString(ruleString);

            return writer.CreateMessage();
        }
    }

    internal static readonly ObjectDisposedException ObserverDisposedException = new ObjectDisposedException(typeof(Observer).FullName);

    sealed class Observer : IDisposable
    {
        private delegate void MessageHandlerDelegate4(Observer observer, Exception? exception, Message? message, object? state1, object? state2, object? state3, object? state4);

        private readonly struct MessageHandler4
        {
            public MessageHandler4(MessageHandlerDelegate4 handler, object? state1 = null, object? state2 = null, object? state3 = null, object? state4 = null)
            {
                _delegate = handler;
                _state1 = state1;
                _state2 = state2;
                _state3 = state3;
                _state4 = state4;
            }

            public void Invoke(Observer observer, Exception? exception, Message? message)
            {
                _delegate(observer, exception, message, _state1, _state2, _state3, _state4);
            }

            public bool HasValue => _delegate is not null;

            private readonly MessageHandlerDelegate4 _delegate;
            private readonly object? _state1;
            private readonly object? _state2;
            private readonly object? _state3;
            private readonly object? _state4;
        }

        private readonly Lock _gate = new();
        private readonly SynchronizationContext? _synchronizationContext;
        private readonly MessageHandler4 _messageHandler;
        private readonly SendOrPostCallback _scMessageCallback;
        private readonly ObserverFlags _flags;
        private bool _disposed;

        public bool Subscribes => (_flags & ObserverFlags.NoSubscribe) == 0;
        public bool EmitOnConnectionDispose => (_flags & ObserverFlags.EmitOnConnectionDispose) != 0;
        public bool EmitOnObserverDispose => (_flags & ObserverFlags.EmitOnObserverDispose) != 0;
        public InnerConnection Connection => Watcher.DBusConnection;

        internal Watcher Watcher = null!;

        private Observer(SynchronizationContext? synchronizationContext, in MessageHandler4 messageHandler, ObserverFlags flags)
        {
            _synchronizationContext = synchronizationContext;
            _messageHandler = messageHandler;
            _flags = flags;
            _scMessageCallback ??= EmitMessageOnSynchronizationContext;
        }

        public static Observer Create<T>(SynchronizationContext? synchronizationContext, MessageValueReader<T> valueReader, Action<Exception?, T, object?, object?> valueHandler, object? readerState, object? handlerState, ObserverFlags flags)
        {
            MessageHandlerDelegate4 fn = static (Observer observer, Exception? exception, Message? message, object? reader, object? handler, object? rs, object? hs) =>
            {
                try
                {
                    var valueHandlerState = (Action<Exception?, T, object?, object?>)handler!;
                    if (exception is not null)
                    {
                        valueHandlerState(exception, default(T)!, rs, hs);
                        return;
                    }
                    Debug.Assert(message is not null);
                    var valueReaderState = (MessageValueReader<T>)reader!;
                    T value = valueReaderState(message, rs);
                    valueHandlerState(null, value, rs, hs);
                }
                catch (Exception ex)
                {
                    observer.Connection.Disconnect(ex);
                }
            };

            MessageHandler4 handler = new(fn, valueReader, valueHandler, readerState, handlerState);
            return new Observer(synchronizationContext, handler, flags);
        }

        public void Dispose() =>
            Dispose(EmitOnObserverDispose ? ObserverDisposedException : null);

        public bool Dispose(Exception? exception, bool removeObserver = true, bool ignoreSynchronizationContext = false)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return false;
                }
                _disposed = true;
            }

            if (exception is not null)
            {
                Emit(exception, ignoreSynchronizationContext);
            }

            if (removeObserver)
            {
                Connection.RemoveWatcherUser(Watcher, this);
            }

            return true;
        }

        public void Emit(Message message)
        {
            if (Subscribes && !Watcher.HasSubscribed)
            {
                return;
            }

            if (_synchronizationContext is null)
            {
                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _messageHandler.Invoke(this, null, message);
                }
            }
            else
            {
                if (_disposed)
                {
                    return;
                }
                message.IncrementRef();
                _synchronizationContext!.Post(_scMessageCallback, message);
            }
        }

        private void EmitMessageOnSynchronizationContext(object? state)
        {
            var message = (Message)state!;
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _messageHandler.Invoke(this, null, message);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
                message.DecrementRef();
            }
        }

        private void Emit(Exception exception, bool ignoreSynchronizationContext = false)
        {
            if (ignoreSynchronizationContext ||
                _synchronizationContext is null ||
                SynchronizationContext.Current == _synchronizationContext)
            {
                _messageHandler.Invoke(this, exception, null!);
            }
            else
            {
                _synchronizationContext.Post(EmitExceptionOnSynchronizationContext, exception);
            }
        }

        private void EmitExceptionOnSynchronizationContext(object? state)
        {
            SynchronizationContext? previousContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                _messageHandler.Invoke(this, (Exception)state!, null!);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        internal void InvokeHandler(Message message)
        {
            if (Subscribes && !Watcher.HasSubscribed)
            {
                return;
            }

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _messageHandler.Invoke(this, null, message);
            }
        }
    }

    private void Disconnect(Exception ex)
    {
        _parentDBusConnection.Disconnect(ex, this);
    }

    private void RemoveWatcherUser(Watcher watcher, Observer? observer)
    {
        lock (_gate)
        {
            if (_state == DBusConnectionState.Disconnected)
            {
                return;
            }

            Debug.Assert(_watchers.ContainsKey(watcher.RuleString));

            if (observer is not null)
            {
                watcher.Observers.Remove(observer);
            }
            else
            {
                watcher.RemoveNonObservingSubscriber();
            }

            // Also when SubscribeTask failed we make a "RemoveMatch" call.
            // There may not be a rule, but we ignore the reply anyway.
            bool hasNoMoreSubscribers = watcher.SubscribeTask is not null && !watcher.HasSubscribers;
            if (hasNoMoreSubscribers)
            {
                EnsureExceptionObserved(watcher.SubscribeTask!);
                watcher.SubscribeTask = null; // We need to re-subscribe.

                var message = CreateRemoveMatchMessage(watcher.RuleString);
                SendMessage(message);

                if (watcher.IsNameOwnerWatcher)
                {
                    _nameOwners.Remove(watcher.Arg0!, out SenderName? sender);
                }
            }

            if (!watcher.HasUsers)
            {
                _watchers.Remove(watcher.RuleString);
                if (watcher.AddMatchAsyncOwnerTask is not null)
                {
                    Debug.Assert(observer is not null);
                    // When the observer was created it awaited this task, so we don't need to observe its failure.
                    // When it was succesfull, we need to remove ourselves as a user.
                    watcher.AddMatchAsyncOwnerTask.ContinueWith(static (t, o) =>
                    {
                        Watcher nameOwnerWatcher = t.Result;
                        InnerConnection connection = nameOwnerWatcher.DBusConnection;
                        connection.RemoveWatcherUser(nameOwnerWatcher, observer: null /* we are a non-observing user to the owner */);
                    }, null, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }
            }
        }

        MessageBuffer CreateRemoveMatchMessage(string ruleString)
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "RemoveMatch",
                signature: "s",
                flags: MessageFlags.NoReplyExpected);

            writer.WriteString(ruleString);

            return writer.CreateMessage();
        }
    }

    sealed class Watcher
    {
        private readonly MessageType? _type;
        private readonly SenderName? _sender;
        private readonly byte[]? _interface;
        private readonly byte[]? _member;
        private readonly byte[]? _path;
        private readonly byte[]? _pathNamespace;
        private readonly byte[]? _destination;
        private readonly byte[]? _arg0;
        private readonly byte[]? _arg0Path;
        private readonly byte[]? _arg0Namespace;
        private readonly string _rule;

        private int _nonObserverSubscribers;

        public List<Observer> Observers { get; } = new();

        public Task<object?>? SubscribeTask { get; set; } // Task for the "AddMatch" D-Bus call.

        public bool HasSubscribed { get; set; }

        public InnerConnection DBusConnection { get; }

        public byte[]? Arg0 => _arg0;

        public bool IsNameOwnerWatcher
            => _arg0 is not null &&
               _type == MessageType.Signal &&
               _interface is not null && _interface.AsSpan().SequenceEqual("org.freedesktop.DBus"u8) &&
               _member is not null && _member.AsSpan().SequenceEqual("NameOwnerChanged"u8) &&
               _sender is not null && _sender.Name.SequenceEqual("org.freedesktop.DBus"u8) &&
               _path is null &&
               _pathNamespace is null &&
               _destination is null &&
               _arg0Path is null &&
               _arg0Namespace is null;

        public string RuleString => _rule;

        public SenderName? Sender => _sender;

        // This is the AddMatchAsync call to watch for D-Bus name owner changes for the sender of this rule (so we can map it to the unique name for matching).
        // It is not null when this Watcher watches a D-Bus registered bus name
        public Task<Watcher>? AddMatchAsyncOwnerTask { get; private set; } // AddMatchAsync call for watching the owner changes.

        public Watcher(InnerConnection connection, string rule, in MatchRuleData data, SenderName? sender, Task<Watcher>? watchNameOwnerTask)
        {
            DBusConnection = connection;
            _rule = rule;
            _type = data.MessageType;
            _sender = sender;
            if (data.Interface is not null)
            {
                _interface = Encoding.UTF8.GetBytes(data.Interface);
            }
            if (data.Member is not null)
            {
                _member = Encoding.UTF8.GetBytes(data.Member);
            }
            if (data.Path is not null)
            {
                _path = Encoding.UTF8.GetBytes(data.Path);
            }
            if (data.PathNamespace is not null)
            {
                _pathNamespace = Encoding.UTF8.GetBytes(data.PathNamespace);
            }
            if (data.Destination is not null)
            {
                _destination = Encoding.UTF8.GetBytes(data.Destination);
            }
            if (data.Arg0 is not null)
            {
                _arg0 = Encoding.UTF8.GetBytes(data.Arg0);
            }
            if (data.Arg0Path is not null)
            {
                _arg0Path = Encoding.UTF8.GetBytes(data.Arg0Path);
            }
            if (data.Arg0Namespace is not null)
            {
                _arg0Namespace = Encoding.UTF8.GetBytes(data.Arg0Namespace);
            }
            AddMatchAsyncOwnerTask = watchNameOwnerTask;
        }

        public bool HasUsers
        {
            get
            {
                return _nonObserverSubscribers > 0 || Observers.Count > 0;
            }
        }

        public bool HasSubscribers
        {
            get
            {
                if (_nonObserverSubscribers > 0)
                {
                    return true;
                }
                if (Observers.Count == 0)
                {
                    return false;
                }
                foreach (var observer in Observers)
                {
                    if (observer.Subscribes)
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        public override string ToString() => _rule;

        internal bool Observes(Message message)
        {
            if (Observers.Count == 0)
            {
                return false;
            }

            if (_type.HasValue && _type != message.MessageType)
            {
                return false;
            }

            if (_path is not null && !IsEqual(_path, message.Path))
            {
                return false;
            }

            if (_member is not null && !IsEqual(_member, message.Member))
            {
                return false;
            }

            if (_interface is not null && !IsEqual(_interface, message.Interface))
            {
                return false;
            }

            if (_pathNamespace is not null && (!message.PathIsSet || !IsEqualOrChildOfPath(message.Path, _pathNamespace)))
            {
                return false;
            }

            if (_arg0Namespace is not null ||
                _arg0 is not null ||
                _arg0Path is not null)
            {
                if (message.Signature.Length == 0)
                {
                    return false;
                }

                DBusType arg0Type = (DBusType)message.Signature![0];

                if (arg0Type != DBusType.String &&
                    arg0Type != DBusType.ObjectPath)
                {
                    return false;
                }

                ReadOnlySpan<byte> arg0 = message.GetBodyReader().ReadStringAsSpan();

                if (_arg0Path is not null && !IsEqualParentOrChildOfPath(arg0, _arg0Path))
                {
                    return false;
                }

                if (arg0Type != DBusType.String)
                {
                    return false;
                }

                if (_arg0 is not null && !IsEqual(_arg0, arg0))
                {
                    return false;
                }

                if (_arg0Namespace is not null && !IsEqualOrChildOfName(arg0, _arg0Namespace))
                {
                    return false;
                }
            }

            if (_sender is not null && !IsEqual(_sender.Name, message.Sender))
            {
                return false;
            }

            if (_destination is not null && !IsEqual(_destination, message.Destination))
            {
                return false;
            }

            return true;
        }


        private static bool IsEqualOrChildOfName(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.StartsWith(rhs) && (lhs.Length == rhs.Length || lhs[rhs.Length] == '.');
        }

        private static bool IsEqualOrChildOfPath(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.StartsWith(rhs) && (lhs.Length == rhs.Length || lhs[rhs.Length] == '/');
        }

        private static bool IsEqualParentOrChildOfPath(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            if (rhs.Length < lhs.Length)
            {
                return rhs[rhs.Length - 1] == '/' && lhs.StartsWith(rhs);
            }
            else if (lhs.Length < rhs.Length)
            {
                return lhs[lhs.Length - 1] == '/' && rhs.StartsWith(lhs);
            }
            else
            {
                return IsEqual(lhs, rhs);
            }
        }

        private static bool IsEqual(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
        {
            return lhs.SequenceEqual(rhs);
        }

        internal void RemoveNonObservingSubscriber()
        {
            Debug.Assert(_nonObserverSubscribers > 0);
            _nonObserverSubscribers--;
        }

        internal void AddNonObserverSubscriber()
        {
            _nonObserverSubscribers++;
        }
    }

    private bool TryGetSenderName(ReadOnlySpan<byte> name, [NotNullWhen(true)]out SenderName? senderName)
    {
        Debug.Assert(_gate.IsHeldByCurrentThread);
#if NET9_0_OR_GREATER
        var lookup = _nameOwners.GetAlternateLookup<ReadOnlySpan<byte>>();
        if (lookup.TryGetValue(name, out var entry))
        {
            senderName = entry;
            return true;
        }
#else
        byte[] nameKey = name.ToArray();
        if (_nameOwners.TryGetValue(nameKey, out var entry))
        {
            senderName = entry;
            return true;
        }
#endif
        senderName = null;
        return false;
    }

    private ReadOnlySpan<byte> MapNameToCurrentOwner(ReadOnlySpan<byte> name)
    {
        Debug.Assert(_gate.IsHeldByCurrentThread);
#if NET9_0_OR_GREATER
        var lookup = _nameOwners.GetAlternateLookup<ReadOnlySpan<byte>>();
        if (lookup.TryGetValue(name, out var entry))
        {
            return entry.Name;
        }
#else
        byte[] nameKey = name.ToArray();
        if (_nameOwners.TryGetValue(nameKey, out var entry))
        {
            return entry.Name;
        }
#endif
        return name;
    }

    public MessageWriter GetMessageWriter() => _parentDBusConnection.GetMessageWriter();

    public void SendMessage(MessageBuffer message)
    {
        if (!_messageStream!.TrySendMessage(message))
        {
            message.ReturnToPool();
        }
    }

    public Task<Exception?> DisconnectedAsync()
    {
        lock (_gate)
        {
            if (_disconnectedTcs is null)
            {
                if (_state == DBusConnectionState.Disconnected)
                {
                    return Task.FromResult(GetWaitForDisconnectException());
                }
                else
                {
                    _disconnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
            return _disconnectedTcs.Task;
        }
    }

    private Exception? GetWaitForDisconnectException()
        => _disconnectReason is ObjectDisposedException ? null : _disconnectReason;

    private void HandleDBusInterfaceSignal(Message message)
    {
        Debug.Assert(message.MessageType == MessageType.Signal);
        Debug.Assert(message.Interface.SequenceEqual("org.freedesktop.DBus"u8));
        Debug.Assert(message.Sender.SequenceEqual("org.freedesktop.DBus"u8));

        if (message.Member.SequenceEqual("NameOwnerChanged"u8))
        {
            var reader = message.GetBodyReader();
            ReadOnlySpan<byte> name = reader.ReadStringAsSpan();

            if (TryGetSenderName(name, out var senderName))
            {
                _ = reader.ReadStringAsSpan(); // old name
                byte[] newOwner = reader.ReadStringAsSpan().ToArray();
                senderName.SetName(newOwner);
            }
            return;
        }

        bool acquiredNotLost = message.Member.SequenceEqual("NameAcquired"u8);
        string? serviceName = null;

        if (acquiredNotLost || message.Member.SequenceEqual("NameLost"u8))
        {
            var reader = message.GetBodyReader();
            serviceName = reader.ReadString();
            ServiceNameRegistration? registration = null;
            lock (_gate)
            {
                _serviceNameRegistrations?.TryGetValue(serviceName, out registration);
            }
            if (registration is not null)
            {
                Action<string, object?>? action = acquiredNotLost ? registration.OnAcquired : registration.OnLost;
                if (action is not null)
                {
                    if (registration.SynchronizationContext is null)
                    {
                        action(serviceName, registration.ActionState);
                    }
                    else
                    {
                        registration.SynchronizationContext.Post(
                            delegate
                            {
                                SynchronizationContext? previousContext = SynchronizationContext.Current;
                                try
                                {
                                    SynchronizationContext.SetSynchronizationContext(registration.SynchronizationContext);
                                    action(serviceName, registration.ActionState);
                                }
                                finally
                                {
                                    SynchronizationContext.SetSynchronizationContext(previousContext);
                                }
                            }, null);
                    }
                }
            }
        }
    }

    internal async Task<RequestNameReply> RequestNameAsync(string serviceName, RequestNameOptions flags, Action<string, object?>? onAcquired, Action<string, object?>? onLost, object? actionState, bool emitOnCapturedContext)
    {
        Task<uint> reply;
        lock (_gate)
        {
            _serviceNameRegistrations ??= new();

            bool hasAction = onAcquired is not null || onLost is not null;
            ServiceNameRegistration? registration = hasAction
                ? new ServiceNameRegistration
                {
                    OnAcquired = onAcquired,
                    OnLost = onLost,
                    ActionState = actionState,
                    SynchronizationContext = emitOnCapturedContext ? SynchronizationContext.Current : null
                }
                : null;
#if NETSTANDARD2_0
            if (_serviceNameRegistrations.ContainsKey(serviceName))
            {
                throw new InvalidOperationException("The name is already registered");
            }
            _serviceNameRegistrations.Add(serviceName, registration);
#else
            if (!_serviceNameRegistrations.TryAdd(serviceName, registration))
            {
                throw new InvalidOperationException("The name is already registered");
            }
#endif
            reply = CallMethodAsync(CreateRequestNameMessage(serviceName, (uint)flags), (Message m, object? s) => m.GetBodyReader().ReadUInt32());
        }

        try
        {
            return (RequestNameReply)await reply.ConfigureAwait(false);
        }
        catch
        {
            lock (_gate)
            {
                _serviceNameRegistrations.Remove(serviceName);
            }

            throw;
        }

        MessageBuffer CreateRequestNameMessage(string name, uint requestFlags)
        {
            var writer = _parentDBusConnection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                signature: "su",
                member: "RequestName");
            writer.WriteString(name);
            writer.WriteUInt32(requestFlags);
            return writer.CreateMessage();
        }
    }

    internal async Task<ReleaseNameReply> ReleaseNameAsync(string serviceName)
    {
        var reply = (ReleaseNameReply)await CallMethodAsync(CreateReleaseNameMessage(serviceName), (Message m, object? s) => m.GetBodyReader().ReadUInt32()).ConfigureAwait(false);

        if (reply == ReleaseNameReply.ReplyReleased)
        {
            lock (_gate)
            {
                _serviceNameRegistrations?.Remove(serviceName);
            }
        }

        return reply;

        MessageBuffer CreateReleaseNameMessage(string name)
        {
            var writer = _parentDBusConnection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                signature: "s",
                member: "ReleaseName");
            writer.WriteString(name);
            return writer.CreateMessage();
        }
    }

    private static void EnsureExceptionObserved(Task task)
    {
        task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    private async Task<byte[]> GetNameOwnerAsync(string name)
    {
        MyValueTaskSource<byte[]> vts = new();

        CallMethod(
            message: CreateGetNameOwnerMessage(name),
            static (Exception? exception, Message? message, object? state) =>
            {
                var vtsState = (MyValueTaskSource<byte[]>)state!;

                if (exception is not null)
                {
                    vtsState.SetException(exception);
                    return;
                }
                Debug.Assert(message is not null);
                if (message.MessageType == MessageType.MethodReturn)
                {
                    try
                    {
                        vtsState.SetResult(message.GetBodyReader().ReadStringAsSpan().ToArray());
                    }
                    catch (Exception ex)
                    {
                        vtsState.SetException(ex);
                    }
                }
                else if (message.MessageType == MessageType.Error)
                {
                    if (message.ErrorName.SequenceEqual("org.freedesktop.DBus.Error.NameHasNoOwner"u8))
                    {
                        vtsState.SetResult(Array.Empty<byte>());
                    }
                    else
                    {
                        vtsState.SetException(CreateDBusExceptionForErrorMessage(message));
                    }
                }
            }, vts);

        return await new ValueTask<byte[]>(vts, token: 0).ConfigureAwait(false);

        MessageBuffer CreateGetNameOwnerMessage(string serviceName)
        {
            using var writer = GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: "org.freedesktop.DBus",
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "GetNameOwner",
                signature: "s");

            writer.WriteString(serviceName);

            return writer.CreateMessage();
        }
    }

    internal sealed class ServiceNameRegistration
    {
        public Action<string, object?>? OnAcquired;
        public Action<string, object?>? OnLost;
        public object? ActionState;
        public SynchronizationContext? SynchronizationContext;
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
#if NET9_0_OR_GREATER
    , IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
#endif
    {
        public byte[] Create(ReadOnlySpan<byte> alternate) => alternate.ToArray();
        public bool Equals(byte[]? x, byte[]? y) => x.AsSpan().SequenceEqual(y);
        public bool Equals(ReadOnlySpan<byte> alternate, byte[] other) => alternate.SequenceEqual(other);
        public int GetHashCode([DisallowNull] byte[] obj) => GetHashCode(obj.AsSpan());
        public int GetHashCode(ReadOnlySpan<byte> alternate)
        {
#if NETSTANDARD2_0 || NETSTANDARD2_1
            int hash = 17;
            foreach (byte b in alternate)
            {
                hash = hash * 31 + b;
            }
            return hash;
#else
            var hash = new HashCode();
            hash.AddBytes(alternate);
            return hash.ToHashCode();
#endif
        }
    }
}

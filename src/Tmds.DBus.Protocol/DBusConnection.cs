using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks.Sources;

#pragma warning disable VSTHRD100 // Avoid "async void" methods

namespace Tmds.DBus.Protocol;

class DBusConnection : IDisposable
{
    private delegate void MessageReceivedHandler(Exception? exception, Message message, object? state);

    class MyValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
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

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _core.OnCompleted(continuation, state, token, flags);
            _continuationSet = true;
        }

        T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);

        void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
    }

    enum ConnectionState
    {
        Created,
        Connecting,
        Connected,
        Disconnected
    }

    delegate void MessageHandlerDelegate(Exception? exception, Message message, object? state1, object? state2, object? state3);

    readonly struct MessageHandler
    {
        public MessageHandler(MessageHandlerDelegate handler, object? state1 = null, object? state2 = null, object? state3 = null)
        {
            _delegate = handler;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
        }

        public void Invoke(Exception? exception, Message message)
        {
            _delegate(exception, message, _state1, _state2, _state3);
        }

        public bool HasValue => _delegate is not null;

        private readonly MessageHandlerDelegate _delegate;
        private readonly object? _state1;
        private readonly object? _state2;
        private readonly object? _state3;
    }

    delegate void MessageHandlerDelegate4(Exception? exception, Message message, object? state1, object? state2, object? state3, object? state4);

    readonly struct MessageHandler4
    {
        public MessageHandler4(MessageHandlerDelegate4 handler, object? state1 = null, object? state2 = null, object? state3 = null, object? state4 = null)
        {
            _delegate = handler;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
            _state4 = state4;
        }

        public void Invoke(Exception? exception, Message message)
        {
            _delegate(exception, message, _state1, _state2, _state3, _state4);
        }

        public bool HasValue => _delegate is not null;

        private readonly MessageHandlerDelegate4 _delegate;
        private readonly object? _state1;
        private readonly object? _state2;
        private readonly object? _state3;
        private readonly object? _state4;
    }

    private readonly object _gate = new object();
    private readonly Connection _parentConnection;
    private readonly Dictionary<uint, MessageHandler> _pendingCalls;
    private readonly CancellationTokenSource _connectCts;
    private readonly Dictionary<string, MatchMaker> _matchMakers;
    private readonly List<Observer> _matchedObservers;
    private readonly Dictionary<string, IMethodHandler> _pathHandlers;

    private IMessageStream? _messageStream;
    private ConnectionState _state;
    private Exception? _disconnectReason;
    private string? _localName;
    private Message? _currentMessage;
    private Observer? _currentObserver;
    private TaskCompletionSource<Exception?>? _disconnectedTcs;

    public string? UniqueName => _localName;

    public Exception DisconnectReason
    {
        get => _disconnectReason ?? new ObjectDisposedException(GetType().FullName);
        set => Interlocked.CompareExchange(ref _disconnectReason, value, null);
    }

    public bool RemoteIsBus => _localName is not null;

    public DBusConnection(Connection parent)
    {
        _parentConnection = parent;
        _connectCts = new();
        _pendingCalls = new();
        _matchMakers = new();
        _matchedObservers = new();
        _pathHandlers = new();
    }

    public async ValueTask ConnectAsync(string address, string? userId, bool supportsFdPassing, CancellationToken cancellationToken)
    {
        _state = ConnectionState.Connecting;
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
                    if (_state != ConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _messageStream = stream = new MessageStream(socket);
                }

                await stream.DoClientAuthAsync(guid, userId, supportsFdPassing).ConfigureAwait(false);

                stream.ReceiveMessages(
                    static (Exception? exception, Message message, DBusConnection connection) =>
                        connection.HandleMessages(exception, message), this);

                lock (_gate)
                {
                    if (_state != ConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _state = ConnectionState.Connected;
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

    private async Task<string?> GetLocalNameAsync()
    {
        MyValueTaskSource<string?> vts = new();

        await CallMethodAsync(
            message: CreateHelloMessage(),
            static (Exception? exception, Message message, object? state) =>
            {
                var vtsState = (MyValueTaskSource<string?>)state!;

                if (exception is not null)
                {
                    vtsState.SetException(exception);
                }
                else if (message.MessageType == MessageType.MethodReturn)
                {
                    vtsState.SetResult(message.GetBodyReader().ReadString().ToString());
                }
                else
                {
                    vtsState.SetResult(null);
                }
            }, vts).ConfigureAwait(false);

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

    private async void HandleMessages(Exception? exception, Message message)
    {
        if (exception is not null)
        {
            _parentConnection.Disconnect(exception, this);
        }
        else
        {
            try
            {
                bool returnMessageToPool = true;
                MessageHandler pendingCall = default;
                IMethodHandler? methodHandler = null;

                bool isMethodCall = message.MessageType == MessageType.MethodCall;

                lock (_gate)
                {
                    if (_state == ConnectionState.Disconnected)
                    {
                        return;
                    }

                    if (message.ReplySerial.HasValue)
                    {
                        _pendingCalls.Remove(message.ReplySerial.Value, out pendingCall);
                    }

                    foreach (var matchMaker in _matchMakers.Values)
                    {
                        if (matchMaker.Matches(message))
                        {
                            _matchedObservers.AddRange(matchMaker.Observers);
                        }
                    }

                    if (isMethodCall)
                    {
                        if (message.Path is not null)
                        {
                            _pathHandlers.TryGetValue(message.Path, out methodHandler);
                        }
                    }
                }

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

                if (isMethodCall)
                {
                    if (methodHandler is not null)
                    {
                        bool runHandlerSynchronously = methodHandler.RunMethodHandlerSynchronously(message);
                        if (runHandlerSynchronously)
                        {
                            bool handled = await methodHandler.TryHandleMethodAsync(_parentConnection, message);
                            if (!handled)
                            {
                                SendUnknownMethodError(message);
                            }
                        }
                        else
                        {
                            returnMessageToPool = false;
                            RunMethodHandler(methodHandler, message);
                        }
                    }
                    else
                    {
                        SendUnknownMethodError(message);
                    }
                }

                if (returnMessageToPool)
                {
                    message.ReturnToPool();
                }
            }
            catch (Exception ex)
            {
                _parentConnection.Disconnect(ex, this);
            }
        }
    }

    private void SendUnknownMethodError(Message methodCall)
    {
        if ((methodCall.MessageFlags & MessageFlags.NoReplyExpected) != 0)
        {
            return;
        }

        string errMsg = String.Format("Method \"{0}\" with signature \"{1}\" on interface \"{2}\" doesn't exist",
                                        methodCall.Member?.ToString() ?? "",
                                        methodCall.Signature?.ToString() ?? "",
                                        methodCall.Interface?.ToString() ?? "");

        SendErrorReplyMessage(methodCall, "org.freedesktop.DBus.Error.UnknownMethod", errMsg);
    }

    private async void RunMethodHandler(IMethodHandler methodHandler, Message message)
    {
        try
        {
            bool handled = await methodHandler.TryHandleMethodAsync(_parentConnection, message);
            if (!handled)
            {
                SendUnknownMethodError(message);
            }
            message.ReturnToPool();
        }
        catch (Exception ex)
        {
            _parentConnection.Disconnect(ex, this);
        }
    }

    private void EmitOnSynchronizationContextHelper(Observer observer, SynchronizationContext synchronizationContext, Message message)
    {
        _currentMessage = message;
        _currentObserver = observer;

#pragma warning disable VSTHRD001 // Await JoinableTaskFactory.SwitchToMainThreadAsync() to switch to the UI thread instead of APIs that can deadlock or require specifying a priority.
        // note: Send blocks the current thread until the SynchronizationContext ran the delegate.
        synchronizationContext.Send(static o => {
            DBusConnection conn = (DBusConnection)o;
            conn._currentObserver!.Emit(conn._currentMessage!);
        }, this);

        _currentMessage = null;
        _currentObserver = null;
    }

    public void AddMethodHandlers(IList<IMethodHandler> methodHandlers)
    {
        lock (_gate)
        {
            if (_state == ConnectionState.Disconnected)
            {
                return;
            }

            int registeredCount = 0;

            try
            {
                for (int i = 0; i < methodHandlers.Count; i++)
                {
                    IMethodHandler methodHandler = methodHandlers[i];

                    _pathHandlers.Add(methodHandler.Path, methodHandler);

                    registeredCount++;
                }
            }
            catch
            {
                for (int i = 0; i < registeredCount; i++)
                {
                    IMethodHandler methodHandler = methodHandlers[i];

                    _pathHandlers.Remove(methodHandler.Path);
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_state == ConnectionState.Disconnected)
            {
                return;
            }
            _state = ConnectionState.Disconnected;
        }

        Exception disconnectReason = DisconnectReason;

        _messageStream?.Close(disconnectReason);

        if (_pendingCalls is not null)
        {
            foreach (var pendingCall in _pendingCalls.Values)
            {
                pendingCall.Invoke(new DisconnectedException(disconnectReason), null!);
            }
            _pendingCalls.Clear();
        }

        foreach (var matchMaker in _matchMakers.Values)
        {
            foreach (var observer in matchMaker.Observers)
            {
                observer.Disconnect(new DisconnectedException(disconnectReason));
            }
        }
        _matchMakers.Clear();

        _disconnectedTcs?.SetResult(GetWaitForDisconnectException());
    }

    private ValueTask CallMethodAsync(MessageBuffer message, MessageReceivedHandler returnHandler, object? state)
    {
        MessageHandlerDelegate fn = static (Exception? exception, Message message, object? state1, object? state2, object? state3) =>
        {
            ((MessageReceivedHandler)state1!)(exception, message, state2);
        };
        MessageHandler handler = new(fn, returnHandler, state);

        return CallMethodAsync(message, handler);
    }

    private async ValueTask CallMethodAsync(MessageBuffer message, MessageHandler handler)
    {
        bool messageSent = false;
        try
        {
            lock (_gate)
            {
                if (_state != ConnectionState.Connected)
                {
                    throw new DisconnectedException(DisconnectReason!);
                }
                if ((message.MessageFlags & MessageFlags.NoReplyExpected) == 0)
                {
                    _pendingCalls.Add(message.Serial, handler);
                }
            }

            messageSent = await _messageStream!.TrySendMessageAsync(message).ConfigureAwait(false);
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
        MessageHandlerDelegate fn = static (Exception? exception, Message message, object? state1, object? state2, object? state3) =>
        {
            var valueReaderState = (MessageValueReader<T>)state1!;
            var vtsState = (MyValueTaskSource<T>)state2!;

            if (exception is not null)
            {
                vtsState.SetException(exception);
            }
            else if (message.MessageType == MessageType.MethodReturn)
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

        await CallMethodAsync(message, handler).ConfigureAwait(false);

        return await new ValueTask<T>(vts, 0).ConfigureAwait(false);
    }

    public async Task CallMethodAsync(MessageBuffer message)
    {
        MyValueTaskSource<object?> vts = new();

        await CallMethodAsync(message,
            static (Exception? exception, Message message, object? state) => CompleteCallValueTaskSource(exception, message, state), vts).ConfigureAwait(false);

        await new ValueTask(vts, 0).ConfigureAwait(false);
    }

    private static void CompleteCallValueTaskSource(Exception? exception, Message message, object? vts)
    {
        var vtsState = (MyValueTaskSource<object?>)vts!;

        if (exception is not null)
        {
            vtsState.SetException(exception);
        }
        else if (message.MessageType == MessageType.MethodReturn)
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
        string errorName = message.ErrorName ?? "<<No ErrorName>>.";
        string errMessage = errorName;
        if (message.Signature is not null && message.Signature.Length > 0 && (DBusType)message.Signature[0] == DBusType.String)
        {
            errMessage = message.GetBodyReader().ReadString();
        }
        return new DBusException(errorName, errMessage);
    }

    public ValueTask<IDisposable> AddMatchAsync<T>(SynchronizationContext? synchronizationContext, MatchRule rule, MessageValueReader<T> valueReader,Action<Exception?, T, object?, object?> valueHandler, object? readerState, object? handlerState, bool subscribe)
    {
        MessageHandlerDelegate4 fn = static (Exception? exception, Message message, object? reader, object? handler, object? rs, object? hs) =>
        {
            var valueHandlerState = (Action<Exception?, T, object?, object?>)handler!;
            if (exception is not null)
            {
                valueHandlerState(exception, default(T)!, rs, hs);
            }
            else
            {
                var valueReaderState = (MessageValueReader<T>)reader!;
                T value = valueReaderState(message, rs);
                valueHandlerState(null, value, rs, hs);
            }
        };

        return AddMatchAsync(synchronizationContext, rule, new(fn, valueReader, valueHandler, readerState, handlerState), subscribe);
    }

    private async ValueTask<IDisposable> AddMatchAsync(SynchronizationContext? synchronizationContext, MatchRule rule, MessageHandler4 handler, bool subscribe)
    {
        MatchRuleData data = rule.Data;
        MatchMaker? matchMaker;
        string ruleString;
        Observer observer;
        MessageBuffer? addMatchMessage = null;

        lock (_gate)
        {
            if (_state != ConnectionState.Connected)
            {
                throw new DisconnectedException(DisconnectReason!);
            }

            if (!RemoteIsBus)
            {
                subscribe = false;
            }

            ruleString = data.GetRuleString();

            if (!_matchMakers.TryGetValue(ruleString, out matchMaker))
            {
                matchMaker = new MatchMaker(this, ruleString, data);
                _matchMakers.Add(ruleString, matchMaker);
            }

            observer = new Observer(synchronizationContext, matchMaker, handler, subscribe);
            matchMaker.Observers.Add(observer);

            bool sendMessage = subscribe && matchMaker.AddMatchTcs is null;
            if (sendMessage)
            {
                addMatchMessage = CreateAddMatchMessage(matchMaker.RuleString);
                matchMaker.AddMatchTcs = new();

                MessageHandlerDelegate fn = static (Exception? exception, Message message, object? state1, object? state2, object? state3) =>
                {
                    var mm = (MatchMaker)state1!;
                    if (message.MessageType == MessageType.MethodReturn)
                    {
                        mm.HasSubscribed = true;
                    }
                    CompleteCallValueTaskSource(exception, message, mm.AddMatchTcs!);
                };

                _pendingCalls.Add(addMatchMessage.Serial, new(fn, matchMaker));
            }
        }

        if (subscribe)
        {
            if (addMatchMessage is not null)
            {
                if (!await _messageStream!.TrySendMessageAsync(addMatchMessage).ConfigureAwait(false))
                {
                    addMatchMessage.ReturnToPool();
                }
            }

            try
            {
                await matchMaker.AddMatchTask!.ConfigureAwait(false);
            }
            catch
            {
                observer.Dispose(invokeHandler: false);

                throw;
            }
        }

        return observer;

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

    sealed class Observer : IDisposable
    {
        private static readonly ObjectDisposedException s_objectDisposedException = new ObjectDisposedException(typeof(Observer).FullName);
        private readonly object _gate = new object();
        private readonly SynchronizationContext? _synchronizationContext;
        private readonly MatchMaker _matchMaker;
        private readonly MessageHandler4 _messageHandler;
        private bool _disposed;

        public bool Subscribes { get; }

        public Observer(SynchronizationContext? synchronizationContext, MatchMaker matchMaker, in MessageHandler4 messageHandler, bool subscribes)
        {
            _synchronizationContext = synchronizationContext;
            _matchMaker = matchMaker;
            _messageHandler = messageHandler;
            Subscribes = subscribes;
        }

        public void Dispose() => Dispose(invokeHandler: true);

        public void Dispose(bool invokeHandler)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            if (invokeHandler)
            {
                _messageHandler.Invoke(s_objectDisposedException, null!);
            }

            _matchMaker.Connection.RemoveObserver(_matchMaker, this);
        }

        public void EmitOnSynchronizationContext(Message message)
        {
            if (_synchronizationContext is null)
            {
                Emit(message);
            }
            else
            {
                _matchMaker.Connection.EmitOnSynchronizationContextHelper(this, _synchronizationContext, message);
            }
        }

        public void Emit(Message message)
        {
            if (Subscribes && !_matchMaker.HasSubscribed)
            {
                return;
            }

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _messageHandler.Invoke(null, message);
            }
        }

        internal void Disconnect(DisconnectedException disconnectedException)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            if (_synchronizationContext is null)
            {
                InvokeHandler(disconnectedException);
            }
            else
            {
                _synchronizationContext.Send(delegate { InvokeHandler(disconnectedException); }, null);
            }

            void InvokeHandler(DisconnectedException disconnectedException)
            {
                _messageHandler.Invoke(disconnectedException, null!);
            }
        }
    }

    private async void RemoveObserver(MatchMaker matchMaker, Observer observer)
    {
        string ruleString = matchMaker.RuleString;
        bool sendMessage = false;

        lock (_gate)
        {
            if (_state == ConnectionState.Disconnected)
            {
                return;
            }

            if (_matchMakers.TryGetValue(ruleString, out _))
            {
                matchMaker.Observers.Remove(observer);
                sendMessage = matchMaker.AddMatchTcs is not null && matchMaker.HasSubscribers;
                if (sendMessage)
                {
                    _matchMakers.Remove(ruleString);
                }
            }
        }

        if (sendMessage)
        {
            var message = CreateRemoveMatchMessage();
            if (!await _messageStream!.TrySendMessageAsync(message).ConfigureAwait(false))
            {
                message.ReturnToPool();
            }
        }

        MessageBuffer CreateRemoveMatchMessage()
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

    sealed class MatchMaker
    {
        private readonly MessageType? _type;
        private readonly string? _sender;
        private readonly string? _interface;
        private readonly string? _member;
        private readonly string? _path;
        private readonly string? _pathNamespace;
        private readonly string? _destination;
        private readonly string? _arg0;
        private readonly string? _arg0Path;
        private readonly string? _arg0Namespace;
        private readonly string _rule;

        private MyValueTaskSource<object?>? _vts;

        public List<Observer> Observers { get; } = new();

        public MyValueTaskSource<object?>? AddMatchTcs
        {
            get => _vts;
            set
            {
                _vts = value;
                if (value != null)
                {
                    AddMatchTask = new ValueTask<object?>(value, token: 0).AsTask();
                }
            }
        }

        public Task<object?>? AddMatchTask { get; private set; }

        public bool HasSubscribed { get; set; }

        public DBusConnection Connection { get; }

        public MatchMaker(DBusConnection connection, string rule, in MatchRuleData data)
        {
            Connection = connection;
            _rule = rule;

            _type = data.MessageType;

            if (data.Sender is not null && data.Sender.StartsWith(":"))
            {
                _sender = data.Sender;
            }
            _interface = data.Interface;
            _member = data.Member;
            _path = data.Path;
            _pathNamespace = data.PathNamespace;
            _destination = data.Destination;
            _arg0 = data.Arg0;
            _arg0Path = data.Arg0Path;
            _arg0Namespace = data.Arg0Namespace;
        }

        public string RuleString => _rule;

        public bool HasSubscribers
        {
            get
            {
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

        internal bool Matches(Message message)
        {
            if (_type.HasValue && _type != message.MessageType)
            {
                return false;
            }

            if (_sender is not null && !IsEqual(_sender, message.Sender))
            {
                return false;
            }

            if (_interface is not null && !IsEqual(_interface, message.Interface))
            {
                return false;
            }

            if (_member is not null && !IsEqual(_member, message.Member))
            {
                return false;
            }

            if (_path is not null && !IsEqual(_path, message.Path))
            {
                return false;
            }

            if (_destination is not null && !IsEqual(_destination, message.Destination))
            {
                return false;
            }

            if (_pathNamespace is not null && (message.Path is null || !IsEqualOrChildOfPath(message.Path, _pathNamespace)))
            {
                return false;
            }

            if (_arg0Namespace is not null ||
                _arg0 is not null ||
                _arg0Path is not null)
            {
                if (string.IsNullOrEmpty(message.Signature))
                {
                    return false;
                }

                DBusType arg0Type = (DBusType)message.Signature![0];

                if (arg0Type != DBusType.String &&
                    arg0Type != DBusType.ObjectPath)
                {
                    return false;
                }

                string arg0 = message.GetBodyReader().ReadString();

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

            return true;
        }

        private static bool IsEqualOrChildOfName(string lhs, string rhs)
        {
            return lhs.StartsWith(rhs) && (lhs.Length == rhs.Length || lhs[rhs.Length] == '.');
        }

        private static bool IsEqualOrChildOfPath(string lhs, string rhs)
        {
            return lhs.StartsWith(rhs) && (lhs.Length == rhs.Length || lhs[rhs.Length] == '/');
        }

        private static bool IsEqualParentOrChildOfPath(string lhs, string rhs)
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

        private static bool IsEqual(string lhs, string? rhs)
        {
            return string.CompareOrdinal(lhs, rhs) == 0;
        }
    }

    public MessageWriter GetMessageWriter() => _parentConnection.GetMessageWriter();

    public async void SendMessage(MessageBuffer message)
    {
        bool messageSent = await _messageStream!.TrySendMessageAsync(message).ConfigureAwait(false);
        if (!messageSent)
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
                if (_state == ConnectionState.Disconnected)
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

    private void SendErrorReplyMessage(Message methodCall, string errorName, string errorMsg)
    {
        SendMessage(CreateErrorMessage(methodCall, errorName, errorMsg));

        MessageBuffer CreateErrorMessage(Message methodCall, string errorName, string errorMsg)
        {
            using var writer = GetMessageWriter();

            writer.WriteError(
                replySerial: methodCall.Serial,
                destination: methodCall.Sender,
                errorName: errorName,
                errorMsg: errorMsg);

            return writer.CreateMessage();
        }
    }
}
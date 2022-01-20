using System.Net;
using System.Net.Sockets;

#pragma warning disable VSTHRD100 // Avoid "async void" methods

namespace Tmds.DBus.Protocol;

class DBusConnection : IDisposable
{
    private delegate void MessageReceivedHandler(Exception? exception, in Message message, object? state);
    private static readonly Exception s_disposedSentinel = new ObjectDisposedException(typeof(Connection).FullName);

    enum ConnectionState
    {
        Created,
        Connecting,
        Connected,
        Disconnected
    }

    delegate void MessageHandlerDelegate(Exception? exception, in Message message, object? state1, object? state2, object? state3);

    readonly struct MessageHandler
    {
        public MessageHandler(MessageHandlerDelegate handler, object? state1 = null, object? state2 = null, object? state3 = null)
        {
            _delegate = handler;
            _state1 = state1;
            _state2 = state2;
            _state3 = state3;
        }

        public void Invoke(Exception? exception, in Message message)
        {
            _delegate(exception, in message, _state1, _state2, _state3);
        }

        public bool HasValue => _delegate is not null;

        private readonly MessageHandlerDelegate _delegate;
        private readonly object? _state1;
        private readonly object? _state2;
        private readonly object? _state3;
    }

    delegate void MessageHandlerDelegate4(Exception? exception, in Message message, object? state1, object? state2, object? state3, object? state4);

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

        public void Invoke(Exception? exception, in Message message)
        {
            _delegate(exception, in message, _state1, _state2, _state3, _state4);
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
                    static (Exception? exception, in Message message, DBusConnection connection) =>
                        connection.HandleMessages(exception, in message), this);

                lock (_gate)
                {
                    if (_state != ConnectionState.Connecting)
                    {
                        throw new DisconnectedException(DisconnectReason);
                    }
                    _state = ConnectionState.Connected;
                }

                _localName = await GetLocalNameAsync();

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
        TaskCompletionSource<string?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await CallMethodAsync(
            message: CreateHelloMessage(),
            static (Exception? exception, in Message message, object? state) =>
            {
                var tcsState = (TaskCompletionSource<string?>)state!;

                if (exception is not null)
                {
                    tcsState.SetException(exception);
                }
                else if (message.MessageType == MessageType.MethodReturn)
                {
                    tcsState.SetResult(message.GetBodyReader().ReadString().ToString());
                }
                else
                {
                    tcsState.SetResult(null);
                }
            }, tcs);

        return await tcs.Task;

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

    private void HandleMessages(Exception? exception, in Message message)
    {
        if (exception is not null)
        {
            _parentConnection.Disconnect(exception, this);
        }
        else
        {
            MessageHandler pendingCall = default;
            IMethodHandler? messageHandler = null;

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
                    string path = message.Path.ToString();
                    if (_pathHandlers.TryGetValue(path, out messageHandler))
                    {

                    }
                }
            }

            foreach (var observer in _matchedObservers)
            {
                observer.Emit(in message);
            }
            _matchedObservers.Clear();

            if (pendingCall.HasValue)
            {
                pendingCall.Invoke(null, in message);
            }

            if (isMethodCall)
            {
                bool handlingMessage = messageHandler is not null && messageHandler.TryHandleMethod(_parentConnection, message);

                if (!handlingMessage)
                {
                    SendUnknownMethodError(message);
                }
            }
        }

        void SendUnknownMethodError(in Message methodCall)
        {
            if ((methodCall.MessageFlags & MessageFlags.NoReplyExpected) != 0)
            {
                return;
            }

            string errMsg = String.Format("Method \"{0}\" with signature \"{1}\" on interface \"{2}\" doesn't exist",
                                            methodCall.Member.ToString(),
                                            methodCall.Signature.ToString(),
                                            methodCall.Interface.ToString());

            SendErrorReplyMessage(methodCall, "org.freedesktop.DBus.Error.UnknownMethod", errMsg);
        }
    }

    public void AddMethodHandlers(IList<IMethodHandler> methodHandlers)
    {
        lock (_gate)
        {
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
            Message message = default;
            foreach (var pendingCall in _pendingCalls.Values)
            {
                pendingCall.Invoke(new DisconnectedException(disconnectReason), in message);
            }
            _pendingCalls.Clear();
        }

        if (_matchMakers is not null)
        {
            foreach (var matchMaker in _matchMakers.Values)
            {
                foreach (var observer in matchMaker.Observers)
                {
                    observer.Disconnect(new DisconnectedException(disconnectReason));
                }
            }
            _matchMakers.Clear();
        }
    }

    private ValueTask CallMethodAsync(MessageBuffer message, MessageReceivedHandler returnHandler, object? state)
    {
        MessageHandlerDelegate fn = static (Exception? exception, in Message message, object? state1, object? state2, object? state3) =>
        {
            ((MessageReceivedHandler)state1!)(exception, in message, state2);
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
                // TODO: don't add pending call when NoReplyExpected.
                _pendingCalls.Add(message.Serial, handler);
            }

            messageSent = await _messageStream!.TrySendMessageAsync(message);
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
        MessageHandlerDelegate fn = static (Exception? exception, in Message message, object? state1, object? state2, object? state3) =>
        {
            var valueReaderState = (MessageValueReader<T>)state1!;
            var tcsState = (TaskCompletionSource<T>)state2!;

            if (exception is not null)
            {
                tcsState.SetException(exception);
            }
            else if (message.MessageType == MessageType.MethodReturn)
            {
                tcsState.SetResult(valueReaderState(in message, state3));
            }
            else if (message.MessageType == MessageType.Error)
            {
                string errorName = message.ErrorName.ToString();
                string errMessage = errorName;
                if (!message.Signature.IsEmpty && (DBusType)message.Signature.Span[0] == DBusType.String)
                {
                    errMessage = message.GetBodyReader().ReadString().ToString();
                }
                tcsState.SetException(new DBusException(errorName, errMessage));
            }
            else
            {
                tcsState.SetException(new ProtocolException($"Unexpected reply type: {message.MessageType}."));
            }
        };

        TaskCompletionSource<T> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MessageHandler handler = new(fn, valueReader, tcs, state);

        await CallMethodAsync(message, handler);

        return await tcs.Task;
    }

    public async Task CallMethodAsync(MessageBuffer message)
    {
        TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await CallMethodAsync(message,
            static (Exception? exception, in Message message, object? state) => CompleteCallTaskCompletionSource(exception, in message, state), tcs);

        await tcs.Task;
    }

    private static void CompleteCallTaskCompletionSource(Exception? exception, in Message message, object? tcs)
    {
        var tcsState = (TaskCompletionSource<object?>)tcs!;

        if (exception is not null)
        {
            tcsState.SetException(exception);
        }
        else if (message.MessageType == MessageType.MethodReturn)
        {
            tcsState.SetResult(null);
        }
        else if (message.MessageType == MessageType.Error)
        {
            string errorName = message.ErrorName.ToString();
            string errMessage = errorName;
            if (!message.Signature.IsEmpty && (DBusType)message.Signature.Span[0] == DBusType.String)
            {
                errMessage = message.GetBodyReader().ReadString().ToString();
            }
            tcsState.SetException(new DBusException(errorName, errMessage));
        }
        else
        {
            tcsState.SetException(new ProtocolException($"Unexpected reply type: {message.MessageType}."));
        }
    }

    public ValueTask<IDisposable> AddMatchAsync<T>(MatchRule rule, MessageValueReader<T> valueReader, Action<Exception?, T, object?> valueHandler, object? readerState, object? handlerState, bool subscribe)
    {
        MessageHandlerDelegate4 fn = static (Exception? exception, in Message message, object? state1, object? state2, object? state3, object? state4) =>
        {
            var valueHandlerState = (Action<Exception?, T, object?>)state2!;
            if (exception is not null)
            {
                valueHandlerState(exception, default(T)!, state4);
            }
            else
            {
                var valueReaderState = (MessageValueReader<T>)state1!;
                T value = valueReaderState(in message, state3);
                valueHandlerState(null, value, state4);
            }
        };

        return AddMatchAsync(rule, new(fn, valueReader, valueHandler, readerState, handlerState), subscribe);
    }

    private async ValueTask<IDisposable> AddMatchAsync(MatchRule rule, MessageHandler4 handler, bool subscribe)
    {
        MatchRuleData data = rule.Data;
        MatchMaker? matchMaker;
        string ruleString;
        uint nextSerial = 0;
        bool sendMessage;
        Observer observer;

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

            observer = new Observer(matchMaker, handler, subscribe);
            matchMaker.Observers.Add(observer);

            sendMessage = subscribe && matchMaker.AddMatchTcs is null;

            if (sendMessage)
            {
                matchMaker.AddMatchTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                MessageHandlerDelegate fn = static (Exception? exception, in Message message, object? state1, object? state2, object? state3) =>
                {
                    var mm = (MatchMaker)state1!;
                    if (message.MessageType == MessageType.MethodReturn)
                    {
                        mm.HasSubscribed = true;
                    }
                    CompleteCallTaskCompletionSource(exception, in message, mm.AddMatchTcs!);
                };

                _pendingCalls.Add(nextSerial, new(fn, matchMaker));
            }
        }

        if (subscribe)
        {
            if (sendMessage)
            {
                var message = CreateAddMatchMessage(matchMaker.RuleString);
                if (!await _messageStream!.TrySendMessageAsync(message))
                {
                    message.ReturnToPool();
                }
            }

            try
            {
                await matchMaker.AddMatchTcs!.Task;
            }
            catch
            {
                observer.Dispose();

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
        private readonly object _gate = new object();
        private readonly MatchMaker _matchMaker;
        private readonly MessageHandler4 _messageHandler;
        private bool _disposed;

        public bool Subscribes { get; }

        public Observer(MatchMaker matchMaker, in MessageHandler4 messageHandler, bool subscribes)
        {
            _matchMaker = matchMaker;
            _messageHandler = messageHandler;
            Subscribes = subscribes;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                Message message = default;
                // TODO: signal Dispose without allocating an Exception?
                _messageHandler.Invoke(new ObjectDisposedException(GetType().FullName), in message);
            }

            _matchMaker.Connection.RemoveObserver(_matchMaker, this);
        }

        public void Emit(in Message message)
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

                _messageHandler.Invoke(null, in message);
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

                Message message = default;
                _messageHandler.Invoke(disconnectedException, in message);
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
            if (!await _messageStream!.TrySendMessageAsync(message))
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
        private readonly byte[]? _sender;
        private readonly byte[]? _interface;
        private readonly byte[]? _member;
        private readonly byte[]? _path;
        private readonly byte[]? _pathNamespace;
        private readonly byte[]? _destination;
        private readonly byte[]? _arg0;
        private readonly byte[]? _arg0Path;
        private readonly byte[]? _arg0Namespace;
        private readonly string _rule;

        public List<Observer> Observers { get; } = new();

        public TaskCompletionSource<object?>? AddMatchTcs { get; set; }

        public bool HasSubscribed { get; set; }

        public DBusConnection Connection { get; }

        public MatchMaker(DBusConnection connection, string rule, in MatchRuleData data)
        {
            Connection = connection;
            _rule = rule;

            _type = data.MessageType;

            if (data.Sender is not null && data.Sender.StartsWith(":"))
            {
                _sender = Encoding.UTF8.GetBytes(data.Sender);
            }
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

        internal bool Matches(in Message message) // TODO: 'in' arg
        {
            if (_type.HasValue && _type != message.MessageType)
            {
                return false;
            }

            if (_sender is not null && !message.Sender.Span.SequenceEqual(_sender))
            {
                return false;
            }

            if (_interface is not null && !message.Interface.Span.SequenceEqual(_interface))
            {
                return false;
            }

            if (_member is not null && !message.Member.Span.SequenceEqual(_member))
            {
                return false;
            }

            if (_path is not null && !message.Path.Span.SequenceEqual(_path))
            {
                return false;
            }

            if (_destination is not null && !message.Destination.Span.SequenceEqual(_destination))
            {
                return false;
            }

            if (_pathNamespace is not null && !IsEqualOrChildOfPath(message.Path, _pathNamespace))
            {
                return false;
            }

            if (_arg0Namespace is not null ||
                _arg0 is not null ||
                _arg0Path is not null)
            {
                if (message.Signature.IsEmpty)
                {
                    return false;
                }

                DBusType arg0Type = (DBusType)message.Signature.Span[0];

                if (arg0Type != DBusType.String ||
                    arg0Type != DBusType.ObjectPath)
                {
                    return false;
                }

                ReadOnlySpan<byte> arg0 = message.GetBodyReader().ReadString();

                if (_arg0Path is not null && !IsEqualParentOrChildOfPath(arg0, _arg0Path))
                {
                    return false;
                }

                if (arg0Type != DBusType.String)
                {
                    return false;
                }

                if (_arg0 is not null && !arg0.SequenceEqual(_arg0))
                {
                    return false;
                }

                if (_arg0Namespace is not null && !IsEqualOrChildOfName(_arg0, _arg0Namespace))
                {
                    return false;
                }
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
                return lhs.SequenceEqual(rhs);
            }
        }
    }

    public MessageWriter GetMessageWriter() => _parentConnection.GetMessageWriter();

    public async void SendMessage(MessageBuffer message)
    {
        bool messageSent = await _messageStream!.TrySendMessageAsync(message);
        if (!messageSent)
        {
            message.ReturnToPool();
        }
    }

    private void SendErrorReplyMessage(in Message methodCall, string errorName, string errorMsg)
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
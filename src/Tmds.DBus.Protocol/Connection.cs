namespace Tmds.DBus.Protocol;

public delegate T MessageValueReader<T>(in Message message, object? state);

public interface IMethodHandler
{
    bool TryHandleMethod(Connection connection, in Message message);

    string Path { get; }
}

public class Connection : IDisposable
{
    private static readonly Exception s_disposedSentinel = new ObjectDisposedException(typeof(Connection).FullName);
    private static Connection? s_systemConnection;
    private static Connection? s_sessionConnection;

    public static Connection System => s_systemConnection ?? CreateConnection(ref s_systemConnection, Address.System);
    public static Connection Session => s_sessionConnection ?? CreateConnection(ref s_sessionConnection, Address.Session);

    public string? UniqueName => GetConnection()?.UniqueName;

    enum ConnectionState
    {
        Created,
        Connecting,
        Connected,
        Disconnected
    }

    private readonly object _gate = new object();
    private readonly ClientConnectionOptions _connectionOptions;
    private DBusConnection? _connection;
    private CancellationTokenSource? _connectCts;
    private Task<DBusConnection>? _connectingTask;
    private ClientSetupResult? _setupResult;
    private ConnectionState _state;
    private bool _disposed;
    private int _nextSerial;

    public Connection(string address) :
        this(new ClientConnectionOptions(address))
    { }

    public Connection(ConnectionOptions connectionOptions)
    {
        if (connectionOptions == null)
            throw new ArgumentNullException(nameof(connectionOptions));

        _connectionOptions = (ClientConnectionOptions)connectionOptions;
    }

    public async ValueTask ConnectAsync()
    {
        await ConnectCoreAsync(autoConnect: false);
    }

    private ValueTask<DBusConnection> ConnectCoreAsync(bool autoConnect = true)
    {
        lock (_gate)
        {
            ThrowHelper.ThrowIfDisposed(_disposed, this);

            ConnectionState state = _state;

            if (state == ConnectionState.Connected)
            {
                return new ValueTask<DBusConnection>(_connection!);
            }

            if (!_connectionOptions.AutoConnect)
            {
                if (autoConnect || _state != ConnectionState.Created)
                {
                    throw new InvalidOperationException("Can only connect once using an explicit call.");
                }
            }

            if (state == ConnectionState.Connecting)
            {
                return new ValueTask<DBusConnection>(_connectingTask!);
            }

            _state = ConnectionState.Connecting;
            _connectingTask = DoConnectAsync();

            return new ValueTask<DBusConnection>(_connectingTask);
        }
    }

    private async Task<DBusConnection> DoConnectAsync()
    {
        Debug.Assert(Monitor.IsEntered(_gate));

        DBusConnection? connection = null;
        try
        {
            _connectCts = new();
            _setupResult = await _connectionOptions.SetupAsync(_connectCts.Token);
            connection = _connection = new DBusConnection(this);

            await connection.ConnectAsync(_setupResult.ConnectionAddress, _setupResult.UserId, _setupResult.SupportsFdPassing, _connectCts.Token);

            lock (_gate)
            {
                ThrowHelper.ThrowIfDisposed(_disposed, this);

                if (_connection == connection)
                {
                    _connectingTask = null;
                    _connectCts = null;
                    _state = ConnectionState.Connected;
                }
                else
                {
                    throw new DisconnectedException(connection.DisconnectReason);
                }
            }

            return connection;
        }
        catch (Exception exception)
        {
            Disconnect(exception, connection);

            ThrowHelper.ThrowIfDisposed(_disposed, this);

            throw;
        }
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
        }

        Disconnect(s_disposedSentinel);
    }

    internal void Disconnect(Exception disconnectReason, DBusConnection? trigger = null)
    {
        DBusConnection? connection;
        ClientSetupResult? setupResult;
        CancellationTokenSource? connectCts;
        lock (_gate)
        {
            if (trigger is not null && trigger != _connection)
            {
                // Already disconnected from this stream.
                return;
            }

            ConnectionState state = _state;
            if (state == ConnectionState.Disconnected)
            {
                return;
            }

            _state = ConnectionState.Disconnected;

            connection = _connection;
            setupResult = _setupResult;
            connectCts = _connectCts;

            _connection = null;
            _connectingTask = null;
            _setupResult = null;
            _connectCts = null;

            if (connection is not null)
            {
                connection.DisconnectReason = disconnectReason;
            }
        }

        connectCts?.Cancel();
        connection?.Dispose();
        if (setupResult != null)
        {
            _connectionOptions.Teardown(setupResult.TeardownToken);
        }
    }

    public async Task CallMethodAsync(MessageBuffer message)
    {
        DBusConnection connection;
        try
        {
            connection = await ConnectCoreAsync();
        }
        catch
        {
            message.ReturnToPool();
            throw;
        }
        await connection.CallMethodAsync(message);
    }

    public async Task<T> CallMethodAsync<T>(MessageBuffer message, MessageValueReader<T> reader, object? readerState = null)
    {
        DBusConnection connection;
        try
        {
            connection = await ConnectCoreAsync();
        }
        catch
        {
            message.ReturnToPool();
            throw;
        }
        return await connection.CallMethodAsync(message, reader, readerState);
    }

    public async ValueTask<IDisposable> AddMatchAsync<T>(MatchRule rule, MessageValueReader<T> reader, Action<Exception?, T, object?> handler, object? readerState = null, object? handlerState = null, bool subscribe = true)
    {
        DBusConnection connection = await ConnectCoreAsync();
        return await connection.AddMatchAsync(rule, reader, handler, readerState, handlerState, subscribe);
    }

    public void AddMethodHandler(IMethodHandler methodHandler)
        => AddMethodHandlers(new[] { methodHandler });

    public void AddMethodHandlers(IList<IMethodHandler> methodHandlers)
    {
        GetConnection()?.AddMethodHandlers(methodHandlers);
    }

    private static Connection CreateConnection(ref Connection? field, string? address)
    {
        address = address ?? "unix:";
        var connection = Volatile.Read(ref field);
        if (connection is not null)
        {
            return connection;
        }
        var newConnection = new Connection(new ClientConnectionOptions(address) { AutoConnect = true });
        connection = Interlocked.CompareExchange(ref field, newConnection, null);
        if (connection != null)
        {
            newConnection.Dispose();
            return connection;
        }
        return newConnection;
    }

    public MessageWriter GetMessageWriter() => new MessageWriter(MessagePool.Shared.Rent(), GetNextSerial());

    public bool TrySendMessage(MessageBuffer message)
    {
        DBusConnection? connection = GetConnection();
        if (connection is null)
        {
            message.ReturnToPool();
            return false;
        }
        connection.SendMessage(message);
        return true;
    }

    private DBusConnection? GetConnection()
    {
        lock (_gate)
        {
            ThrowHelper.ThrowIfDisposed(_disposed, this);

            if (_connectionOptions.AutoConnect)
            {
                throw new InvalidOperationException("Method cannot be used on autoconnect connections.");
            }

            ConnectionState state = _state;

            if (state == ConnectionState.Created ||
                state == ConnectionState.Connecting)
            {
                throw new InvalidOperationException("Connect before using this method.");
            }

            return _connection;
        }
    }

    internal uint GetNextSerial() => (uint)Interlocked.Increment(ref _nextSerial);
}
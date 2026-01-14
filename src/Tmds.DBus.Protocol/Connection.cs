namespace Tmds.DBus.Protocol;

#pragma warning disable CS0618 // IMethodHandler is obsolete.

/// <summary>
/// Delegate for reading a value from a D-Bus message.
/// </summary>
/// <typeparam name="T">The type of value that is read.</typeparam>
/// <param name="message">The message to read from.</param>
/// <param name="state">Optional state object.</param>
/// <returns>The value read from the message.</returns>
public delegate T MessageValueReader<T>(Message message, object? state);

/// <summary>
/// Represents a client connection to a D-Bus message bus or peer.
/// </summary>
public partial class Connection : IDisposable
{
    internal static readonly Exception DisposedException = new ObjectDisposedException(typeof(Connection).FullName);
    private static Connection? s_systemConnection;
    private static Connection? s_sessionConnection;

    /// <summary>
    /// Gets a shared connection to the system bus.
    /// </summary>
    public static Connection System => s_systemConnection ?? CreateConnection(ref s_systemConnection, DBusAddress.System);

    /// <summary>
    /// Gets a shared connection to the session bus.
    /// </summary>
    public static Connection Session => s_sessionConnection ?? CreateConnection(ref s_sessionConnection, DBusAddress.Session);

    /// <summary>
    /// Gets the unique name assigned to this connection by the bus.
    /// </summary>
    public string? UniqueName => GetConnection().UniqueName;

    enum ConnectionState
    {
        Created,
        Connecting,
        Connected,
        Disconnected
    }

    private readonly Lock _gate = new();
    private readonly ClientConnectionOptions _connectionOptions;
    private DBusConnection? _connection;
    private CancellationTokenSource? _connectCts;
    private Task<DBusConnection>? _connectingTask;
    private ClientSetupResult? _setupResult;
    private ConnectionState _state;
    private bool _disposed;
    private int _nextSerial;

    /// <summary>
    /// Initializes a new instance of the Connection class.
    /// </summary>
    /// <param name="address">The D-Bus address to connect to.</param>
    public Connection(string address) :
        this(new ClientConnectionOptions(address))
    { }

    /// <summary>
    /// Initializes a new instance of the Connection class.
    /// </summary>
    /// <param name="connectionOptions">The connection options.</param>
    public Connection(ConnectionOptions connectionOptions)
    {
        if (connectionOptions == null)
            throw new ArgumentNullException(nameof(connectionOptions));

        _connectionOptions = (ClientConnectionOptions)connectionOptions;
    }

    // For tests.
    internal void Connect(IMessageStream stream)
    {
        _connection = new DBusConnection(this, DBusEnvironment.MachineId);
        _connection.Connect(stream);
        _state = ConnectionState.Connected;
    }

    /// <summary>
    /// Establishes the connection.
    /// </summary>
    public async ValueTask ConnectAsync()
    {
        await ConnectCoreAsync(explicitConnect: true).ConfigureAwait(false);
    }

    private ValueTask<DBusConnection> ConnectCoreAsync(bool explicitConnect = false)
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
                DBusConnection? connection = _connection;
                if (!explicitConnect && _state == ConnectionState.Disconnected && connection is not null)
                {
                    throw new DisconnectedException(connection.DisconnectReason);
                }

                if (!explicitConnect || _state != ConnectionState.Created)
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
        Debug.Assert(_gate.IsHeldByCurrentThread);

        DBusConnection? connection = null;
        try
        {
            _connectCts = new();
            _setupResult = await _connectionOptions.SetupAsync(_connectCts.Token).ConfigureAwait(false);
            connection = _connection = new DBusConnection(this, _setupResult.MachineId ?? DBusEnvironment.MachineId);

            if (_setupResult.ConnectionStream is Stream stream)
            {
                await connection.ConnectAsync(stream, _setupResult.UserId, _connectCts.Token).ConfigureAwait(false);
            }
            else
            {
                await connection.ConnectAsync(_setupResult.ConnectionAddress, _setupResult.UserId, _setupResult.SupportsFdPassing, _connectCts.Token).ConfigureAwait(false);
            }

            lock (_gate)
            {
                ThrowHelper.ThrowIfDisposed(_disposed, this);

                if (_connection == connection && _state == ConnectionState.Connecting)
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

            // Prefer throwing ObjectDisposedException.
            ThrowHelper.ThrowIfDisposed(_disposed, this);

            // Throw DisconnectedException or ConnectException.
            if (exception is DisconnectedException || exception is ConnectException)
            {
                throw;
            }
            else
            {
                throw new ConnectException(exception.Message, exception);
            }
        }
    }

    /// <summary>
    /// Disposes the connection and releases all resources.
    /// </summary>
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

        Disconnect(DisposedException);
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

    /// <summary>
    /// Calls a D-Bus method asynchronously without returning a response value.
    /// </summary>
    /// <param name="message">The method call message.</param>
    public async Task CallMethodAsync(MessageBuffer message)
    {
        DBusConnection connection;
        try
        {
            RefHandles(message);
            connection = await ConnectCoreAsync().ConfigureAwait(false);
        }
        catch
        {
            message.ReturnToPool();
            throw;
        }
        await connection.CallMethodAsync(message).ConfigureAwait(false);
    }

    /// <summary>
    /// Calls a D-Bus method asynchronously and returns a response value.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="message">The method call message.</param>
    /// <param name="reader">The delegate to read the return value from the reply.</param>
    /// <param name="readerState">Optional state passed to the reader delegate.</param>
    /// <returns>Value read from the reply.</returns>
    public async Task<T> CallMethodAsync<T>(MessageBuffer message, MessageValueReader<T> reader, object? readerState = null)
    {
        DBusConnection connection;
        try
        {
            RefHandles(message);
            connection = await ConnectCoreAsync().ConfigureAwait(false);
        }
        catch
        {
            message.ReturnToPool();
            throw;
        }
        return await connection.CallMethodAsync(message, reader, readerState).ConfigureAwait(false);
    }

    private void RefHandles(MessageBuffer message)
    {
        // Take a reference on any handles we might be sending.
        // This ensures the handles are valid or that we throw an exception at this point.
        // It also enables a user to to dispose the handles as soon as Connection method returns
        // (without having to await it).
        message.RefHandles();
    }

    /// <summary>
    /// Adds an observer to receive D-Bus messages matching the specified criteria.
    /// </summary>
    /// <typeparam name="T">The type of value read from matched messages.</typeparam>
    /// <param name="rule">The match rule defining which messages to receive.</param>
    /// <param name="reader">Delegate to read values from matched messages.</param>
    /// <param name="handler">Callback invoked for each matched message.</param>
    /// <param name="flags">Observer behavior flags.</param>
    /// <param name="readerState">Optional state passed to the reader delegate.</param>
    /// <param name="handlerState">Optional state passed to the handler delegate.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke the handler on the captured synchronization context.</param>
    /// <returns><see cref="IDisposable"/> that removes the observer when disposed.</returns>
    public ValueTask<IDisposable> AddMatchAsync<T>(MatchRule rule, MessageValueReader<T> reader, Action<Exception?, T, object?, object?> handler, ObserverFlags flags, object? readerState = null, object? handlerState = null, bool emitOnCapturedContext = true)
        => AddMatchAsync(rule, reader, handler, readerState, handlerState, emitOnCapturedContext, flags);

    /// <summary>
    /// Adds an observer to receive D-Bus messages matching the specified criteria.
    /// </summary>
    /// <typeparam name="T">The type of value read from matched messages.</typeparam>
    /// <param name="rule">The match rule defining which messages to receive.</param>
    /// <param name="reader">Delegate to read values from matched messages.</param>
    /// <param name="handler">Callback invoked for each matched message.</param>
    /// <param name="readerState">Optional state passed to the reader delegate.</param>
    /// <param name="handlerState">Optional state passed to the handler delegate.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke the handler on the captured synchronization context.</param>
    /// <param name="flags">Observer behavior flags.</param>
    /// <returns><see cref="IDisposable"/> that removes the observer when disposed.</returns>
    public ValueTask<IDisposable> AddMatchAsync<T>(MatchRule rule, MessageValueReader<T> reader, Action<Exception?, T, object?, object?> handler, object? readerState, object? handlerState, bool emitOnCapturedContext, ObserverFlags flags)
        => AddMatchAsync(rule, reader, handler, readerState, handlerState, emitOnCapturedContext ? SynchronizationContext.Current : null, flags);

    /// <summary>
    /// Adds an observer to receive D-Bus messages matching the specified criteria.
    /// </summary>
    /// <typeparam name="T">The type of value read from matched messages.</typeparam>
    /// <param name="rule">The match rule defining which messages to receive.</param>
    /// <param name="reader">Delegate to read values from matched messages.</param>
    /// <param name="handler">Callback invoked for each matched message.</param>
    /// <param name="readerState">Optional state passed to the reader delegate.</param>
    /// <param name="handlerState">Optional state passed to the handler delegate.</param>
    /// <param name="synchronizationContext">The synchronization context to invoke the handler on.</param>
    /// <param name="flags">Observer behavior flags.</param>
    /// <returns><see cref="IDisposable"/> that removes the observer when disposed.</returns>
    public async ValueTask<IDisposable> AddMatchAsync<T>(MatchRule rule, MessageValueReader<T> reader, Action<Exception?, T, object?, object?> handler, object? readerState , object? handlerState, SynchronizationContext? synchronizationContext, ObserverFlags flags)
    {
        DBusConnection connection = await ConnectCoreAsync().ConfigureAwait(false);
        return await connection.AddMatchAsync(synchronizationContext, rule, reader, handler, readerState, handlerState, flags).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds an <see cref="IMethodHandler"/> to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandler">The method handler to add.</param>
    public void AddMethodHandler(IMethodHandler methodHandler)
        => UpdateMethodHandlers((dictionary, handler) => dictionary.AddMethodHandler(handler.AsPathMethodHandler()), methodHandler);

    /// <summary>
    /// Adds multiple <see cref="IMethodHandler"/> instances to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandlers">The method handlers to add.</param>
    public void AddMethodHandlers(IReadOnlyList<IMethodHandler> methodHandlers)
        => UpdateMethodHandlers((dictionary, handlers) => dictionary.AddMethodHandlers(handlers.Select(h => h.AsPathMethodHandler()).ToList()), methodHandlers);

    /// <summary>
    /// Adds an <see cref="IPathMethodHandler"/> to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandler">The method handler to add.</param>
    public void AddMethodHandler(IPathMethodHandler methodHandler)
        => UpdateMethodHandlers((dictionary, handler) => dictionary.AddMethodHandler(handler), methodHandler);

    /// <summary>
    /// Adds multiple <see cref="IPathMethodHandler"/> instances to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandlers">The method handlers to add.</param>
    public void AddMethodHandlers(IReadOnlyList<IPathMethodHandler> methodHandlers)
        => UpdateMethodHandlers((dictionary, handlers) => dictionary.AddMethodHandlers(handlers), methodHandlers);

    /// <summary>
    /// Removes a method handler for the specified path.
    /// </summary>
    /// <param name="path">The object path of the handler to remove.</param>
    public void RemoveMethodHandler(string path)
        => UpdateMethodHandlers((dictionary, path) => dictionary.RemoveMethodHandler(path), path);

    /// <summary>
    /// Removes multiple method handlers for the specified paths.
    /// </summary>
    /// <param name="paths">The object paths of the handlers to remove.</param>
    public void RemoveMethodHandlers(IEnumerable<string> paths)
        => UpdateMethodHandlers((dictionary, paths) => dictionary.RemoveMethodHandlers(paths), paths);
        
    private void UpdateMethodHandlers<T>(Action<IMethodHandlerDictionary, T> update, T state)
        => GetConnection().UpdateMethodHandlers(update, state);

    private static Connection CreateConnection(ref Connection? field, string? address)
    {
        address = address ?? "unix:";
        var connection = Volatile.Read(ref field);
        if (connection is not null)
        {
            return connection;
        }
        var newConnection = new Connection(new ClientConnectionOptions(address) { AutoConnect = true, IsShared = true });
        connection = Interlocked.CompareExchange(ref field, newConnection, null);
        if (connection != null)
        {
            newConnection.Dispose();
            return connection;
        }
        return newConnection;
    }

    /// <summary>
    /// Gets a message writer for creating D-Bus messages.
    /// </summary>
    /// <returns>A new MessageWriter instance.</returns>
    public MessageWriter GetMessageWriter() => new MessageWriter(MessageBufferPool.Shared, GetNextSerial());

    /// <summary>
    /// Sends a D-Bus message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns><see langword="true"/> if the message was sent; <see langword="false"/> if not connected.</returns>
    public bool TrySendMessage(MessageBuffer message)
    {
        bool messageSent = false;
        try
        {
            DBusConnection? connection = GetConnection(ifConnected: true);
            if (connection is not null)
            {
                RefHandles(message);
                connection.SendMessage(message);
                messageSent = true;
            }
            return messageSent;
        }
        finally
        {
            if (!messageSent)
            {
                message.ReturnToPool();
            }
        }
    }

    /// <summary>
    /// Returns a task that completes when the connection has disconnected.
    /// </summary>
    /// <returns><see cref="Exception"/> with the disconnect reason, or <see langword="null"/> if disposed normally.</returns>
    public Task<Exception?> DisconnectedAsync()
    {
        DBusConnection connection = GetConnection();
        return connection.DisconnectedAsync();
    }

    private DBusConnection GetConnection() => GetConnection(ifConnected: false)!;

    private DBusConnection? GetConnection(bool ifConnected)
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

            if (ifConnected && state != ConnectionState.Connected)
            {
                return null;
            }

            return _connection;
        }
    }

    internal uint GetNextSerial() => (uint)Interlocked.Increment(ref _nextSerial);
}
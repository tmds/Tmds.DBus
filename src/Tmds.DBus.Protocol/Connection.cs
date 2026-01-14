namespace Tmds.DBus.Protocol;

#pragma warning disable CS0618 // IMethodHandler is obsolete.

/// <summary>
/// Represents a client connection to a D-Bus message bus or peer.
/// </summary>
[Obsolete("Use DBusConnection instead.")]
public class Connection : IDisposable
{
    private readonly DBusConnection _connection;

    /// <summary>
    /// Gets a shared connection to the system bus.
    /// </summary>
    public static Connection System => DBusConnection.System.AsConnection();

    /// <summary>
    /// Gets a shared connection to the session bus.
    /// </summary>
    public static Connection Session => DBusConnection.Session.AsConnection();

    /// <summary>
    /// The D-Bus daemon object path.
    /// </summary>
    public const string DBusObjectPath = DBusConnection.DBusObjectPath;

    /// <summary>
    /// The D-Bus daemon service name.
    /// </summary>
    public const string DBusServiceName = DBusConnection.DBusServiceName;

    /// <summary>
    /// The D-Bus daemon interface name.
    /// </summary>
    public const string DBusInterface = DBusConnection.DBusInterface;

    /// <summary>
    /// Gets the unique name assigned to this connection by the bus.
    /// </summary>
    public string? UniqueName => _connection.UniqueName;

    /// <summary>
    /// Initializes a new instance of the Connection class.
    /// </summary>
    /// <param name="address">The D-Bus address to connect to.</param>
    public Connection(string address)
    {
        _connection = new DBusConnection(address);
    }

    /// <summary>
    /// Initializes a new instance of the Connection class.
    /// </summary>
    /// <param name="connectionOptions">The connection options.</param>
    public Connection(ConnectionOptions connectionOptions)
    {
        _connection = new DBusConnection(new ConnectionOptionsWrapper((ClientConnectionOptions)connectionOptions));
    }

    internal Connection(DBusConnection connection)
    {
        _connection = connection;
    }

    private sealed class ConnectionOptionsWrapper : DBusConnectionOptions
    {
        private readonly ClientConnectionOptions _options;

        public ConnectionOptionsWrapper(ClientConnectionOptions options)
        {
            _options = options;
            AutoConnect = options.AutoConnect;
            IsShared = options.IsShared;
        }

        protected internal override async ValueTask<SetupResult> SetupAsync(CancellationToken cancellationToken)
        {
            ClientSetupResult clientResult = await _options.SetupAsync(cancellationToken).ConfigureAwait(false);
            return new SetupResult(clientResult.ConnectionAddress)
            {
                TeardownToken = clientResult.TeardownToken,
                UserId = clientResult.UserId,
                MachineId = clientResult.MachineId,
                SupportsFdPassing = clientResult.SupportsFdPassing,
                ConnectionStream = clientResult.ConnectionStream
            };
        }

        protected internal override void Teardown(object? token)
        {
            _options.Teardown(token);
        }
    }

    /// <summary>
    /// Establishes the connection.
    /// </summary>
    public ValueTask ConnectAsync()
        => _connection.ConnectAsync();

    /// <summary>
    /// Disposes the connection and releases all resources.
    /// </summary>
    public void Dispose()
        => _connection.Dispose();

    /// <summary>
    /// Calls a D-Bus method asynchronously without returning a response value.
    /// </summary>
    /// <param name="message">The method call message.</param>
    public Task CallMethodAsync(MessageBuffer message)
        => _connection.CallMethodAsync(message);

    /// <summary>
    /// Calls a D-Bus method asynchronously and returns a response value.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="message">The method call message.</param>
    /// <param name="reader">The delegate to read the return value from the reply.</param>
    /// <param name="readerState">Optional state passed to the reader delegate.</param>
    /// <returns>Value read from the reply.</returns>
    public Task<T> CallMethodAsync<T>(MessageBuffer message, MessageValueReader<T> reader, object? readerState = null)
        => _connection.CallMethodAsync(message, reader, readerState);

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
        => _connection.AddMatchAsync(rule, reader, handler, flags, readerState, handlerState, emitOnCapturedContext);

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
        => _connection.AddMatchAsync(rule, reader, handler, readerState, handlerState, emitOnCapturedContext, flags);

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
    public ValueTask<IDisposable> AddMatchAsync<T>(MatchRule rule, MessageValueReader<T> reader, Action<Exception?, T, object?, object?> handler, object? readerState, object? handlerState, SynchronizationContext? synchronizationContext, ObserverFlags flags)
        => _connection.AddMatchAsync(rule, reader, handler, readerState, handlerState, synchronizationContext, flags);

    /// <summary>
    /// Adds an <see cref="IMethodHandler"/> to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandler">The method handler to add.</param>
    public void AddMethodHandler(IMethodHandler methodHandler)
        => _connection.AddMethodHandler(methodHandler);

    /// <summary>
    /// Adds multiple <see cref="IMethodHandler"/> instances to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandlers">The method handlers to add.</param>
    public void AddMethodHandlers(IReadOnlyList<IMethodHandler> methodHandlers)
        => _connection.AddMethodHandlers(methodHandlers);

    /// <summary>
    /// Adds an <see cref="IPathMethodHandler"/> to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandler">The method handler to add.</param>
    public void AddMethodHandler(IPathMethodHandler methodHandler)
        => _connection.AddMethodHandler(methodHandler);

    /// <summary>
    /// Adds multiple <see cref="IPathMethodHandler"/> instances to handle incoming method calls.
    /// </summary>
    /// <param name="methodHandlers">The method handlers to add.</param>
    public void AddMethodHandlers(IReadOnlyList<IPathMethodHandler> methodHandlers)
        => _connection.AddMethodHandlers(methodHandlers);

    /// <summary>
    /// Removes a method handler for the specified path.
    /// </summary>
    /// <param name="path">The object path of the handler to remove.</param>
    public void RemoveMethodHandler(string path)
        => _connection.RemoveMethodHandler(path);

    /// <summary>
    /// Removes multiple method handlers for the specified paths.
    /// </summary>
    /// <param name="paths">The object paths of the handlers to remove.</param>
    public void RemoveMethodHandlers(IEnumerable<string> paths)
        => _connection.RemoveMethodHandlers(paths);

    /// <summary>
    /// Gets a message writer for creating D-Bus messages.
    /// </summary>
    /// <returns>A new MessageWriter instance.</returns>
    public MessageWriter GetMessageWriter()
        => _connection.GetMessageWriter();

    /// <summary>
    /// Sends a D-Bus message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns><see langword="true"/> if the message was sent; <see langword="false"/> if not connected.</returns>
    public bool TrySendMessage(MessageBuffer message)
        => _connection.TrySendMessage(message);

    /// <summary>
    /// Returns a task that completes when the connection has disconnected.
    /// </summary>
    /// <returns><see cref="Exception"/> with the disconnect reason, or <see langword="null"/> if disposed normally.</returns>
    public Task<Exception?> DisconnectedAsync()
        => _connection.DisconnectedAsync();

    /// <summary>
    /// Lists all currently registered service names on the bus.
    /// </summary>
    /// <returns>A Task containing an array of service names.</returns>
    public Task<string[]> ListServicesAsync()
        => _connection.ListServicesAsync();

    /// <summary>
    /// Gets all service names that can be activated on the bus.
    /// </summary>
    public Task<string[]> ListActivatableServicesAsync()
        => _connection.ListActivatableServicesAsync();

    /// <summary>
    /// Becomes a monitor that receives all messages on the bus.
    /// </summary>
    /// <param name="handler">The handler invoked for each message received.</param>
    /// <param name="rules">Optional match rules to filter which messages to receive.</param>
    public Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules = null)
        => _connection.BecomeMonitorAsync(handler, rules);

    /// <summary>
    /// Monitors a D-Bus bus and returns an <see cref="IAsyncEnumerable{T}"/> for the observed messages.
    /// </summary>
    /// <param name="address">The D-Bus address to connect to.</param>
    /// <param name="rules">Optional match rules to filter which messages to receive.</param>
    /// <param name="ct">Cancellation token to stop monitoring.</param>
    public static IAsyncEnumerable<DisposableMessage> MonitorBusAsync(string address, IEnumerable<MatchRule>? rules = null, CancellationToken ct = default)
        => DBusConnection.MonitorBusAsync(address, rules, ct);

    /// <summary>
    /// Requests ownership of a name.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    public Task RequestNameAsync(string name, RequestNameOptions options)
        => _connection.RequestNameAsync(name, options);

    /// <summary>
    /// Requests ownership of a name with callback for name loss notification.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <param name="onLost">Callback invoked when the name is lost to another connection.</param>
    /// <param name="actionState">State object passed to the callback.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke the callback on the captured synchronization context.</param>
    public Task RequestNameAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
        => _connection.RequestNameAsync(name, options, onLost, actionState, emitOnCapturedContext);

    /// <summary>
    /// Tries to request ownership of a name.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <returns><see langword="true"/> if the name was acquired; <see langword="false"/> if already owned by another bus user.</returns>
    public Task<bool> TryRequestNameAsync(string name, RequestNameOptions options)
        => _connection.TryRequestNameAsync(name, options);

    /// <summary>
    /// Tries to request ownership of a name with callback for name loss notification.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <param name="onLost">Callback invoked when the name is lost to another connection.</param>
    /// <param name="actionState">State object passed to the callback.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke the callback on the captured synchronization context.</param>
    /// <returns><see langword="true"/> if the name was acquired; <see langword="false"/> if already owned by another connection.</returns>
    public Task<bool> TryRequestNameAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
        => _connection.TryRequestNameAsync(name, options, onLost, actionState, emitOnCapturedContext);

    /// <summary>
    /// Enqueues for ownership of a name.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    public Task QueueNameRequestAsync(string name, RequestNameOptions options)
        => _connection.QueueNameRequestAsync(name, options);

    /// <summary>
    /// Enqueues for ownership of a name with callbacks for acquisition and loss notifications.
    /// </summary>
    /// <param name="name">The well-known name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <param name="onAcquired">Callback invoked when the name is acquired.</param>
    /// <param name="onLost">Callback invoked when the name is lost to another bus user.</param>
    /// <param name="actionState">State object passed to the callbacks.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke callbacks on the captured synchronization context.</param>
    public Task QueueNameRequestAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onAcquired = null, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
        => _connection.QueueNameRequestAsync(name, options, onAcquired, onLost, actionState, emitOnCapturedContext);

    /// <summary>
    /// Releases ownership of a name.
    /// </summary>
    /// <param name="serviceName">The well-known name to release.</param>
    /// <returns>A Task containing <see langword="true"/> if the name was released/dequeued; <see langword="false"/> if the name was not requested by this connection.</returns>
    public Task<bool> ReleaseNameAsync(string serviceName)
        => _connection.ReleaseNameAsync(serviceName);
}

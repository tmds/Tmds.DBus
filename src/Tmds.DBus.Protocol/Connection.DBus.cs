using System.Threading.Channels;

namespace Tmds.DBus.Protocol;

public partial class Connection
{
    /// <summary>
    /// The D-Bus daemon object path.
    /// </summary>
    public const string DBusObjectPath = "/org/freedesktop/DBus";
    /// <summary>
    /// The D-Bus daemon service name.
    /// </summary>
    public const string DBusServiceName = "org.freedesktop.DBus";
    /// <summary>
    /// The D-Bus daemon interface name.
    /// </summary>
    public const string DBusInterface = "org.freedesktop.DBus";

    /// <summary>
    /// Lists all currently registered service names on the bus.
    /// </summary>
    /// <returns>A Task containing an array of service names.</returns>
    public Task<string[]> ListServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadArrayOfString());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListNames");
            return writer.CreateMessage();
        }
    }

    /// <summary>
    /// Gets all service names that can be activated on the bus.
    /// </summary>
    public Task<string[]> ListActivatableServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadArrayOfString());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListActivatableNames");
            return writer.CreateMessage();
        }
    }

    /// <summary>
    /// Becomes a monitor that receives all messages on the bus.
    /// </summary>
    /// <param name="handler">The handler invoked for each message received.</param>
    /// <param name="rules">Optional match rules to filter which messages to receive.</param>
    public async Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules = null)
    {
        if (_connectionOptions.IsShared)
        {
            throw new InvalidOperationException("Cannot become monitor on a shared connection.");
        }

        DBusConnection connection = await ConnectCoreAsync().ConfigureAwait(false);
        await connection.BecomeMonitorAsync(handler, rules).ConfigureAwait(false);
    }

    /// <summary>
    /// Monitors a D-Bus bus and returns an <see cref="IAsyncEnumerable{T}"/> for the observed messages.
    /// </summary>
    /// <param name="address">The D-Bus address to connect to.</param>
    /// <param name="rules">Optional match rules to filter which messages to receive.</param>
    /// <param name="ct">Cancellation token to stop monitoring.</param>
    public static async IAsyncEnumerable<DisposableMessage> MonitorBusAsync(string address, IEnumerable<MatchRule>? rules = null, [EnumeratorCancellation]CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var channel = Channel.CreateUnbounded<DisposableMessage>(
            new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true,
            }
        );

        using var connection = new Connection(address);
        using CancellationTokenRegistration ctr =
#if NETCOREAPP3_1_OR_GREATER
                ct.UnsafeRegister(c => ((Connection)c!).Dispose(), connection);
#else
                ct.Register(c => ((Connection)c!).Dispose(), connection);
#endif
        try
        {
            await connection.ConnectAsync().ConfigureAwait(false);

            await connection.BecomeMonitorAsync(
                (Exception? ex, DisposableMessage message) =>
                {
                    if (ex is not null)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            ex = new OperationCanceledException(ct);
                        }
                        channel.Writer.TryComplete(ex);
                        return;
                    }

                    if (!channel.Writer.TryWrite(message))
                    {
                        message.Dispose();
                    }
                },
                rules
            ).ConfigureAwait(false);
        }
        catch
        {
            ct.ThrowIfCancellationRequested();

            throw;
        }

        while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            if (channel.Reader.TryRead(out DisposableMessage msg))
            {
                yield return msg;
            }
        }
    }

    /// <summary>
    /// Requests ownership of a name.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    public Task RequestNameAsync(string name, RequestNameOptions options)
        => RequestNameAsync(name, options, null, null);

    /// <summary>
    /// Requests ownership of a name with callback for name loss notification.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <param name="onLost">Callback invoked when the name is lost to another connection.</param>
    /// <param name="actionState">State object passed to the callback.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke the callback on the captured synchronization context.</param>
    public async Task RequestNameAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
    {
        bool isOwner = await TryRequestNameAsync(name, options, onLost, actionState, emitOnCapturedContext);
        if (!isOwner)
        {
            // Model DBUS_REQUEST_NAME_REPLY_EXISTS method return reply as a DBus error reply.
            throw new DBusException("org.freedesktop.DBus.Error.NameExists", "The name already has an owner.");
        }
    }

    /// <summary>
    /// Tries to request ownership of a name.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <returns><see langword="true"/> if the name was acquired; <see langword="false"/> if already owned by another bus user.</returns>
    public Task<bool> TryRequestNameAsync(string name, RequestNameOptions options)
        => TryRequestNameAsync(name, options, null, null);

    /// <summary>
    /// Tries to request ownership of a name with callback for name loss notification.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <param name="onLost">Callback invoked when the name is lost to another connection.</param>
    /// <param name="actionState">State object passed to the callback.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke the callback on the captured synchronization context.</param>
    /// <returns><see langword="true"/> if the name was acquired; <see langword="false"/> if already owned by another connection.</returns>
    public async Task<bool> TryRequestNameAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
    {
        DBusConnection connection = GetConnection();
        ThrowIfOnLostSetWhenReplacementNotAllowed(onLost, options);

        const RequestNameOptions DoNotQueue = (RequestNameOptions)4;
        RequestNameReply reply = await connection.RequestNameAsync(name, options | DoNotQueue, onAcquired: null, onLost, actionState, emitOnCapturedContext).ConfigureAwait(false);

        switch (reply)
        {
            case RequestNameReply.PrimaryOwner:
                return true;
            case RequestNameReply.Exists:
                return false;
            case RequestNameReply.AlreadyOwner:
                throw new InvalidOperationException("Service is already registered by this connection");
            case RequestNameReply.InQueue:
            default:
                throw new ProtocolException("Unexpected reply");
        }
    }

    /// <summary>
    /// Enqueues for ownership of a name.
    /// </summary>
    /// <param name="name">The name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    public Task QueueNameRequestAsync(string name, RequestNameOptions options)
        => QueueNameRequestAsync(name, options, null, null, null);

    /// <summary>
    /// Enqueues for ownership of a name with callbacks for acquisition and loss notifications.
    /// </summary>
    /// <param name="name">The well-known name to request.</param>
    /// <param name="options">Options for requesting the name.</param>
    /// <param name="onAcquired">Callback invoked when the name is acquired.</param>
    /// <param name="onLost">Callback invoked when the name is lost to another bus user.</param>
    /// <param name="actionState">State object passed to the callbacks.</param>
    /// <param name="emitOnCapturedContext">Whether to invoke callbacks on the captured synchronization context.</param>
    public async Task QueueNameRequestAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onAcquired = null, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
    {
        DBusConnection connection = GetConnection();
        ThrowIfOnLostSetWhenReplacementNotAllowed(onLost, options);

        RequestNameReply reply = await connection.RequestNameAsync(name, options, onAcquired, onLost, actionState, emitOnCapturedContext).ConfigureAwait(false);

        switch (reply)
        {
            case RequestNameReply.PrimaryOwner:
            case RequestNameReply.InQueue:
                return;
            case RequestNameReply.AlreadyOwner:
                throw new InvalidOperationException("Service is already registered by this connection");
            case RequestNameReply.Exists:
            default:
                throw new ProtocolException("Unexpected reply");
        }
    }

    /// <summary>
    /// Releases ownership of a name.
    /// </summary>
    /// <param name="serviceName">The well-known name to release.</param>
    /// <returns>A Task containing <see langword="true"/> if the name was released/dequeued; <see langword="false"/> if the name was not requested by this connection.</returns>
    public async Task<bool> ReleaseNameAsync(string serviceName)
    {
        DBusConnection connection = GetConnection();
        ReleaseNameReply reply = await connection.ReleaseNameAsync(serviceName).ConfigureAwait(false);
        return reply == ReleaseNameReply.ReplyReleased;
    }

    static void ThrowIfOnLostSetWhenReplacementNotAllowed(Action<string, object?>? onLost, RequestNameOptions options)
    {
        if (onLost != null && (options & RequestNameOptions.AllowReplacement) == 0)
        {
            throw new ArgumentException($"{nameof(onLost)} can only be set when {nameof(RequestNameOptions.AllowReplacement)} is also set", nameof(onLost));
        }
    }
}
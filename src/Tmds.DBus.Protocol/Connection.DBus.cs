using System.Threading.Channels;

namespace Tmds.DBus.Protocol;

public partial class Connection
{
    public const string DBusObjectPath = "/org/freedesktop/DBus";
    public const string DBusServiceName = "org.freedesktop.DBus";
    public const string DBusInterface = "org.freedesktop.DBus";

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

    public async Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules = null)
    {
        if (_connectionOptions.IsShared)
        {
            throw new InvalidOperationException("Cannot become monitor on a shared connection.");
        }

        DBusConnection connection = await ConnectCoreAsync().ConfigureAwait(false);
        await connection.BecomeMonitorAsync(handler, rules).ConfigureAwait(false);
    }

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

    public Task RequestNameAsync(string name, RequestNameOptions options)
        => RequestNameAsync(name, options, null, null);

    public async Task RequestNameAsync(string name, RequestNameOptions options = RequestNameOptions.Default, Action<string, object?>? onLost = null, object? actionState = null, bool emitOnCapturedContext = true)
    {
        bool isOwner = await TryRequestNameAsync(name, options, onLost, actionState, emitOnCapturedContext);
        if (!isOwner)
        {
            // Model DBUS_REQUEST_NAME_REPLY_EXISTS method return reply as a DBus error reply.
            throw new DBusException("org.freedesktop.DBus.Error.NameExists", "The name already has an owner.");
        }
    }

    public Task<bool> TryRequestNameAsync(string name, RequestNameOptions options)
        => TryRequestNameAsync(name, options, null, null);

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

    public Task QueueNameRequestAsync(string name, RequestNameOptions options)
        => QueueNameRequestAsync(name, options, null, null, null);

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
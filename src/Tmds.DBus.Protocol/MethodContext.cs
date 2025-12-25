namespace Tmds.DBus.Protocol;

public sealed class MethodContext : IDisposable
{
    [Flags]
    private enum Flags
    {
        None = 0,
        IsDisposed = 1 << 0,
        ReplySent = 1 << 1,
        IsIntrospectableInterface = 1 << 2,
        IsPeerInterface = 1 << 3,
        IsPropertiesInterface = 1 << 4,
        DisposesAsynchronously = 1 << 5,
        CanDispose = 1 << 6, // Disallows Dispose when using IMethodHandler.
        NoReplyExpected = 1 << 7,
    }

    private Flags _flags;
    private readonly Connection _connection;
    private readonly Message _request;
    private readonly CancellationToken _requestAborted;

    internal MethodContext(Connection connection, Message request, CancellationToken requestAborted)
    {
        _connection = connection;
        _request = request;
        _requestAborted = requestAborted;
        _flags |= Flags.CanDispose;
        if (request is not null) // Tests pass in a null request...
        {
            _flags |= GetDBusInterfaceFlags(request.Interface);
            if ((request.MessageFlags & MessageFlags.NoReplyExpected) != 0)
            {
                _flags |= Flags.NoReplyExpected;
            }
        }
    }

    private Flags GetDBusInterfaceFlags(ReadOnlySpan<byte> interf)
    {
        if (interf.StartsWith("org.freedesktop.DBus"u8))
        {
            if (interf.Length == "org.freedesktop.DBus.Introspectable".Length && interf.EndsWith(".Introspectable"u8))
            {
                return Flags.IsIntrospectableInterface;
            }
            else if (interf.Length == "org.freedesktop.DBus.Peer".Length && interf.EndsWith(".Peer"u8))
            {
                return Flags.IsPeerInterface;
            }
            else if (interf.Length == "org.freedesktop.DBus.Properties".Length && interf.EndsWith(".Properties"u8))
            {
                return Flags.IsPropertiesInterface;
            }
        }
        return Flags.None;
    }

    public Message Request
    {
        get
        {
            ThrowIfDisposed();
            return _request;
        }
    }

    public Connection Connection
    {
        get
        {
            // Don't throw when Disposed for accessing the connection.
            return _connection;
        }
    }

    public CancellationToken RequestAborted
    {
        get
        {
            ThrowIfDisposed();
            return _requestAborted;
        }
    }

    private bool IsDisposed => (_flags & Flags.IsDisposed) != 0;

    public bool ReplySent => (_flags & Flags.ReplySent) != 0;

    public bool NoReplyExpected => (_flags & Flags.NoReplyExpected) != 0;

    public bool DisposesAsynchronously
    {
        get
        {
            ThrowIfDisposed();
            return (_flags & Flags.DisposesAsynchronously) != 0;
        }
        set
        {
            ThrowIfDisposed();
            ThrowIfNotCanDispose();
            bool currentValue = (_flags & Flags.DisposesAsynchronously) != 0;
            if (currentValue && !value)
            {
                throw new InvalidOperationException("Cannot set DisposesAsynchronously to false after it has been set to true.");
            }
            if (value)
                _flags |= Flags.DisposesAsynchronously;
            else
                _flags &= ~Flags.DisposesAsynchronously;
        }
    }

    internal bool CanDispose
    {
        get => (_flags & Flags.CanDispose) != 0;
        set
        {
            if (value)
                _flags |= Flags.CanDispose;
            else
                _flags &= ~Flags.CanDispose;
        }
    }

    internal bool IsPeerInterface => (_flags & Flags.IsPeerInterface) != 0;

    public bool IsPropertiesInterfaceRequest => (_flags & Flags.IsPropertiesInterface) != 0;

    public bool IsDBusIntrospectRequest =>
        (_flags & Flags.IsIntrospectableInterface) != 0 &&
        Request.Member.SequenceEqual("Introspect"u8);

    internal string[]? IntrospectChildNames { get; set; }

    public MessageWriter CreateReplyWriter(string? signature)
    {
        ThrowIfDisposed();

        var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: signature
        );
        return writer;
    }

    public void Reply(MessageBuffer message)
    {
        if ((_flags & (Flags.IsDisposed | Flags.ReplySent | Flags.NoReplyExpected)) != 0)
        {
            message.ReturnToPool();
            if (IsDisposed)
            {
                throw new ObjectDisposedException(typeof(MethodContext).FullName);
            }
            else if (ReplySent)
            {
                throw new InvalidOperationException("A reply has already been sent.");
            }
        }

        _flags |= Flags.ReplySent;
        Connection.TrySendMessage(message);
    }

    public void ReplyError(string? errorName = null,
                           string? errorMsg = null)
    {
        ThrowIfDisposed();

        // Avoid throwing on an error path when a reply was already sent.
        if (ReplySent)
        {
            return;
        }

        using var writer = Connection.GetMessageWriter();
        writer.WriteError(
            replySerial: Request.Serial,
            destination: Request.Sender,
            errorName: errorName,
            errorMsg: errorMsg
        );

        _flags |= Flags.ReplySent;
        Connection.TrySendMessage(writer.CreateMessage());
    }

    public void ReplyUnknownMethodError()
    {
        ReplyError("org.freedesktop.DBus.Error.UnknownMethod",
                   $"Method \"{Request.MemberAsString}\" with signature \"{Request.SignatureAsString}\" on interface \"{Request.InterfaceAsString}\" doesn't exist");
    }

    public void ReplyIntrospectXml(ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls)
    {
        ThrowIfDisposed();

        if (!IsDBusIntrospectRequest)
        {
            throw new InvalidOperationException($"Can not reply with introspection XML when {nameof(IsDBusIntrospectRequest)} is false.");
        }

        using var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: "s"
        );

        // Add the Peer and Introspectable interfaces.
        // Tools like D-Feet will list the paths separately as soon as there is an interface.
        // We add the base interfaces only for the paths that we want to show up.
        // Those are paths that have other interfaces.
        bool includeBaseInterfaces = !interfaceXmls.IsEmpty;
        ReadOnlySpan<ReadOnlyMemory<byte>> baseInterfaceXmls = includeBaseInterfaces ? [ IntrospectionXml.DBusIntrospectable, IntrospectionXml.DBusPeer ] : [ ];

        // Add the child names.
#if NET5_0_OR_GREATER
        ReadOnlySpan<string> childNames = IntrospectChildNames;
        IEnumerable<string>? childNamesEnumerable = null;
#else
        ReadOnlySpan<string> childNames = default;
        IEnumerable<string>? childNamesEnumerable = IntrospectChildNames;
#endif

        writer.WriteIntrospectionXml(interfaceXmls, baseInterfaceXmls, childNames, childNamesEnumerable);

        Reply(writer.CreateMessage());
    }

    public void Disconnect(Exception exception)
    {
        ThrowIfDisposed();

        Connection.Disconnect(exception);
    }

    public void Dispose()
        => Dispose(force: false);

    internal void Dispose(bool force)
    {
        if (!force)
        {
            ThrowIfNotCanDispose();
        }

        if (!IsDisposed)
        {
            if (!ReplySent && !NoReplyExpected)
            {
                ReplyUnknownMethodError();
            }

            Request.ReturnToPool();

            _flags |= Flags.IsDisposed;
        }
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(typeof(MethodContext).FullName);
        }
    }

    private void ThrowIfNotCanDispose()
    {
        if ((_flags & Flags.CanDispose) == 0)
        {
            throw new InvalidOperationException($"Disposing is not supported with 'IMethodHandler'. Use {nameof(IPathMethodHandler)} instead.");
        }
    }
}
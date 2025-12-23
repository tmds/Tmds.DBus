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
    }

    private Flags _flags;

    internal MethodContext(Connection connection, Message request, CancellationToken requestAborted)
    {
        Connection = connection;
        Request = request;
        RequestAborted = requestAborted;
        _flags |= Flags.CanDispose;
        if (request is not null) // Tests pass in a null request...
        {
            _flags |= GetDBusInterfaceFlags(request.Interface);
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

    public Message Request { get; }
    public Connection Connection { get; }
    public CancellationToken RequestAborted { get; }

    public bool ReplySent => (_flags & Flags.ReplySent) != 0;

    public bool NoReplyExpected => (Request.MessageFlags & MessageFlags.NoReplyExpected) != 0;

    public bool DisposesAsynchronously
    {
        get => (_flags & Flags.DisposesAsynchronously) != 0;
        set
        {
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
        ThrowIfDisposed();

        if (ReplySent || NoReplyExpected)
        {
            message.ReturnToPool();
            if (ReplySent)
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

        using var writer = Connection.GetMessageWriter();
        writer.WriteError(
            replySerial: Request.Serial,
            destination: Request.Sender,
            errorName: errorName,
            errorMsg: errorMsg
        );
        Reply(writer.CreateMessage());
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
        // Those are paths that have other interfaces, paths that are leaves.
        bool includeBaseInterfaces = !interfaceXmls.IsEmpty || IntrospectChildNames is null || IntrospectChildNames.Length == 0;
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

        if ((_flags & Flags.IsDisposed) == 0)
        {
            if (!ReplySent && !NoReplyExpected)
            {
                if (IsDBusIntrospectRequest && IntrospectChildNames is not null)
                {
                    ReplyIntrospectXml(interfaceXmls: []);
                }
                else
                {
                    ReplyUnknownMethodError();
                }
            }

            Request.ReturnToPool();

            _flags |= Flags.IsDisposed;
        }
    }

    private void ThrowIfDisposed()
    {
        if ((_flags & Flags.IsDisposed) != 0)
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
namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents a D-Bus method call that is being handled.
/// </summary>
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
    private readonly DBusConnection _connection;
    private readonly Message _request;
    private readonly CancellationToken _requestAborted;

    internal MethodContext(DBusConnection connection, Message request, CancellationToken requestAborted)
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
            request.IncrementRef();
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

    /// <summary>
    /// Gets the request message.
    /// </summary>
    public Message Request
    {
        get
        {
            ThrowIfDisposed();
            return _request;
        }
    }

    /// <summary>
    /// Gets the D-Bus connection associated with this method call.
    /// </summary>
    // TODO: When Connection is removed, make this return DBusConnection and mark DBusConnection property as [EditorBrowsable(EditorBrowsableState.Never)].
    [Obsolete("Use DBusConnection instead.")]
    public Connection Connection
    {
        get
        {
            // Don't throw when Disposed for accessing the connection.
            return _connection.AsConnection();
        }
    }

    /// <summary>
    /// Gets the D-Bus connection associated with this method call.
    /// </summary>
    public DBusConnection DBusConnection
    {
        get
        {
            // Don't throw when Disposed for accessing the connection.
            return _connection;
        }
    }

    /// <summary>
    /// Gets a cancellation token that is cancelled when the request is aborted.
    /// </summary>
    public CancellationToken RequestAborted
    {
        get
        {
            ThrowIfDisposed();
            return _requestAborted;
        }
    }

    private bool IsDisposed => (_flags & Flags.IsDisposed) != 0;

    /// <summary>
    /// Gets a value indicating whether a reply has been sent.
    /// </summary>
    public bool ReplySent => (_flags & Flags.ReplySent) != 0;

    /// <summary>
    /// Gets a value indicating whether no reply is expected for this method call.
    /// </summary>
    public bool NoReplyExpected => (_flags & Flags.NoReplyExpected) != 0;

    /// <summary>
    /// Gets or sets a value indicating whether the method will be handled and disposed asynchronously.
    /// </summary>
    /// <remarks>
    /// When this is set to <c>true</c>, the context must be disposed.
    /// </remarks>
    public bool DisposesAsynchronously
    {
        get
        {
            // Don't throw for IsDisposed because the async dispose may have happened before this property is read.
            return (_flags & Flags.DisposesAsynchronously) != 0;
        }
        set
        {
            // Don't throw for IsDisposed because the async dispose may have happened before this property is set.
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

    /// <summary>
    /// Gets a value indicating whether this is a request to the org.freedesktop.DBus.Properties interface.
    /// </summary>
    public bool IsPropertiesInterfaceRequest => (_flags & Flags.IsPropertiesInterface) != 0;

    /// <summary>
    /// Gets a value indicating whether this is an Introspect request to the org.freedesktop.DBus.Introspectable interface.
    /// </summary>
    public bool IsDBusIntrospectRequest =>
        (_flags & Flags.IsIntrospectableInterface) != 0 &&
        Request.Member.SequenceEqual("Introspect"u8);

    internal string[]? IntrospectChildNames { get; set; }

    /// <summary>
    /// Creates a MessageWriter for writing the method reply.
    /// </summary>
    /// <param name="signature">The signature of the reply body, or null if empty.</param>
    /// <returns>A MessageWriter for writing the reply.</returns>
    public MessageWriter CreateReplyWriter(string? signature)
    {
        ThrowIfDisposed();

        var writer = DBusConnection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: signature
        );
        return writer;
    }

    /// <summary>
    /// Sends a reply message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public void Reply(MessageBuffer message)
        => Reply(message, throwIfAlreadySent: true);

    private void Reply(MessageBuffer message, bool throwIfAlreadySent)
    {
        if ((_flags & (Flags.IsDisposed | Flags.ReplySent | Flags.NoReplyExpected)) != 0)
        {
            message.ReturnToPool();
            if (IsDisposed)
            {
                throw new ObjectDisposedException(typeof(MethodContext).FullName);
            }
            else if (ReplySent && throwIfAlreadySent)
            {
                throw new InvalidOperationException("A reply has already been sent.");
            }
            return;
        }

        _flags |= Flags.ReplySent;
        DBusConnection.TrySendMessage(message);
    }

    /// <summary>
    /// Sends an error reply.
    /// </summary>
    /// <param name="errorName">The error name, or null to use a default.</param>
    /// <param name="errorMsg">The error message, or null if none.</param>
    public void ReplyError(string? errorName = null,
                           string? errorMsg = null)
    {
        using var writer = DBusConnection.GetMessageWriter();
        writer.WriteError(
            replySerial: Request.Serial,
            destination: Request.Sender,
            errorName: errorName,
            errorMsg: errorMsg
        );

        // Avoid throwing on an error path when a reply was already sent.
        Reply(writer.CreateMessage(), throwIfAlreadySent: false);
    }

    /// <summary>
    /// Sends an "Unknown Method" error reply.
    /// </summary>
    public void ReplyUnknownMethodError()
    {
        ReplyError("org.freedesktop.DBus.Error.UnknownMethod",
                   $"Method \"{Request.MemberAsString}\" with signature \"{Request.SignatureAsString}\" on interface \"{Request.InterfaceAsString}\" doesn't exist");
    }

    /// <summary>
    /// Sends an introspection XML reply.
    /// </summary>
    /// <param name="interfaceXmls">The interface XML fragments to include in the introspection.</param>
    public void ReplyIntrospectXml(ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls)
    {
        ThrowIfDisposed();

        if (!IsDBusIntrospectRequest)
        {
            throw new InvalidOperationException($"Can not reply with introspection XML when {nameof(IsDBusIntrospectRequest)} is false.");
        }

        using var writer = DBusConnection.GetMessageWriter();
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

    /// <summary>
    /// Disconnects the D-Bus connection with the specified exception.
    /// </summary>
    /// <param name="exception">The exception indicating the reason for disconnection.</param>
    public void Disconnect(Exception exception)
    {
        ThrowIfDisposed();

        DBusConnection.Disconnect(exception);
    }

    /// <summary>
    /// Disposes the method context and sends a default reply if needed.
    /// </summary>
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

            Request.DecrementRef();

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
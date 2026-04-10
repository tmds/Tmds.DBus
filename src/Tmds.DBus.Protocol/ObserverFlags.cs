namespace Tmds.DBus.Protocol;

/// <summary>
/// Flags for <c>Connection.AddMatchAsync</c> that the message observer behavior.
/// </summary>
[Flags]
public enum ObserverFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Emit a completion notification (<see cref="NotificationType.ConnectionClosed"/>) when the connection is disposed.
    /// </summary>
    [Obsolete("Use EmitOnConnectionClosed instead.")]
    EmitOnConnectionDispose = 1,
    /// <summary>
    /// Emit a completion notification (<see cref="NotificationType.ConnectionClosed"/>) when the connection is closed.
    /// </summary>
    EmitOnConnectionClosed = 1,
    /// <summary>
    /// Emit a completion notification (<see cref="NotificationType.ObserverDisposed"/>) when the observer is disposed.
    /// </summary>
    EmitOnObserverDispose = 2,
    /// <summary>
    /// Do not subscribe to the signal on the bus.
    /// </summary>
    NoSubscribe = 4,
    /// <summary>
    /// Emit a completion notification (<see cref="NotificationType.OwnerChanged"/>) when the owner of the matched bus name changes.
    /// </summary>
    EmitOnOwnerChanged = 8,
    /// <summary>
    /// Emit a completion notification (<see cref="NotificationType.ConnectionFailed"/>) when the connection fails.
    /// </summary>
    EmitOnConnectionFailed = 16,
    /// <summary>
    /// Emit a completion notification (<see cref="NotificationType.ReaderFailed"/>) when the reader fails.
    /// </summary>
    EmitOnReaderFailed = 32,

    /// <summary>
    /// Emit a completion notification when either the connection or observer is disposed.
    /// </summary>
    [Obsolete("Use 'EmitOnConnectionClosed | EmitOnObserverDispose' instead.")]
    EmitOnDispose = EmitOnConnectionDispose | EmitOnObserverDispose,
    /// <summary>
    /// Emit completion notifications for all events.
    /// </summary>
    EmitAll = EmitOnConnectionClosed | EmitOnObserverDispose | EmitOnOwnerChanged | EmitOnConnectionFailed | EmitOnReaderFailed,
}

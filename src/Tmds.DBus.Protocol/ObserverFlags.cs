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
    /// Emit a completion exception when the connection is disposed.
    /// </summary>
    /// <remarks>Use <see cref="ObserverHandler.IsConnectionDisposed"/> to check for this exception.</remarks>
    EmitOnConnectionDispose = 1,
    /// <summary>
    /// Emit a completion exception when the observer is disposed.
    /// </summary>
    /// <remarks>Use <see cref="ObserverHandler.IsObserverDisposed"/> to check for this exception.</remarks>
    EmitOnObserverDispose = 2,
    /// <summary>
    /// Do not subscribe to the signal on the bus.
    /// </summary>
    NoSubscribe = 4,

    /// <summary>
    /// Emit a completion exception when either the connection or observer is disposed.
    /// </summary>
    /// <remarks>Use <see cref="ObserverHandler.IsDisposed"/> to check for this exception.</remarks>
    EmitOnDispose = EmitOnConnectionDispose | EmitOnObserverDispose,
}

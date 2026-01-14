namespace Tmds.DBus.Protocol;

/// <summary>
/// Provides methods for checking completion exceptions emitted by message observers.
/// </summary>
public static class ObserverHandler
{
    /// <summary>
    /// Checks if the exception indicates the observer was disposed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates the observer was disposed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnObserverDispose"/> or <see cref="ObserverFlags.EmitOnDispose"/> is set.</remarks>
    public static bool IsObserverDisposed(Exception exception)
        => object.ReferenceEquals(exception, DBusConnection.ObserverDisposedException);

    /// <summary>
    /// Checks if the exception indicates the connection was disposed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates the connection was disposed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnConnectionDispose"/> or <see cref="ObserverFlags.EmitOnDispose"/> is set.</remarks>
    public static bool IsConnectionDisposed(Exception exception)
        // note: Connection.DisposedException is only ever used as an InnerException of DisconnectedException,
        //       so we directly check for that.
        => object.ReferenceEquals(exception?.InnerException, Connection.DisposedException);

    /// <summary>
    /// Checks if the exception indicates either the observer or connection was disposed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates disposal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnDispose"/>, <see cref="ObserverFlags.EmitOnObserverDispose"/>, or <see cref="ObserverFlags.EmitOnConnectionDispose"/> is set.</remarks>
    public static bool IsDisposed(Exception exception)
        => IsObserverDisposed(exception) || IsConnectionDisposed(exception);
}

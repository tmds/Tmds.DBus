namespace Tmds.DBus.Protocol;

/// <summary>
/// Provides methods for checking completion exceptions emitted by message observers.
/// </summary>
[Obsolete("Use Notification.Type or Notification.IsCompletion instead.")]
public static class ObserverHandler
{
    /// <summary>
    /// Checks if the exception indicates the observer was disposed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates the observer was disposed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnObserverDispose"/> or <see cref="ObserverFlags.EmitOnDispose"/> is set.</remarks>
    public static bool IsObserverDisposed(Exception exception)
        => InnerConnection.DetermineNotificationType(exception, null) == NotificationType.ObserverDisposed;

    /// <summary>
    /// Checks if the exception indicates the connection was disposed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates the connection was disposed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnConnectionDispose"/> or <see cref="ObserverFlags.EmitOnDispose"/> is set.</remarks>
    public static bool IsConnectionDisposed(Exception exception)
        => InnerConnection.DetermineNotificationType(exception, null) == NotificationType.ConnectionClosed;

    /// <summary>
    /// Checks if the exception indicates either the observer or connection was disposed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates disposal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnDispose"/>, <see cref="ObserverFlags.EmitOnObserverDispose"/>, or <see cref="ObserverFlags.EmitOnConnectionDispose"/> is set.</remarks>
    public static bool IsDisposed(Exception exception)
        => InnerConnection.DetermineNotificationType(exception, null) is NotificationType.ObserverDisposed or NotificationType.ConnectionClosed;

    /// <summary>
    /// Checks if the exception indicates the owner of the matched bus name has changed.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates an owner change; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This exception is emitted when <see cref="ObserverFlags.EmitOnOwnerChanged"/> is set.</remarks>
    public static bool IsOwnerChanged(Exception exception)
        => InnerConnection.DetermineNotificationType(exception, null) == NotificationType.OwnerChanged;
}

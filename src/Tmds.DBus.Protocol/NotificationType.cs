namespace Tmds.DBus.Protocol;

/// <summary>
/// Indicates the type of notification.
/// </summary>
public enum NotificationType
{
    // '0' is not used so that uninitialized values are not considered a valid notification type.

    /// <summary>
    /// A value was successfully read.
    /// </summary>
    Value = 1,
    /// <summary>
    /// The observer was disposed.
    /// </summary>
    ObserverDisposed = 2,
    /// <summary>
    /// The connection was closed.
    /// </summary>
    ConnectionClosed = 3,
    /// <summary>
    /// The connection failed.
    /// </summary>
    ConnectionFailed = 4,
    /// <summary>
    /// The reader failed.
    /// </summary>
    ReaderFailed = 5,
    /// <summary>
    /// The owner of the matched bus name changed.
    /// </summary>
    OwnerChanged = 6,
}

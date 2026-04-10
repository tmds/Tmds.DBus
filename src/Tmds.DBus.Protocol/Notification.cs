namespace Tmds.DBus.Protocol;

/// <summary>
/// Notification that indicates a value was received or that the observer completed.
/// </summary>
/// <remarks>The completion notifications are limited to those specified with the <see cref="ObserverFlags"/>.</remarks>
public readonly struct Notification
{
    private readonly InnerConnection.Observer? _observer;
    private readonly bool _isCompletion;

    internal Notification(InnerConnection.Observer observer, bool isCompletion)
    {
        _observer = observer;
        _isCompletion = isCompletion;
    }

    /// <summary>
    /// Returns an exception that indicates why the observer completed.
    /// </summary>
    /// <exception cref="InvalidOperationException">The notification is not a completion.</exception>
    public Exception Exception
    {
        get
        {
            if (!_isCompletion)
            {
                ThrowHelper.ThrowNotificationExceptionNotAvailable();
            }
            return _observer!.Exception!;
        }
    }

    /// <summary>
    /// Gets the type of notification.
    /// </summary>
    public NotificationType Type => _observer is null ? default : (_isCompletion ? _observer.ErrorType : NotificationType.Value);

    /// <summary>
    /// Returns whether this is a completion notification.
    /// </summary>
    public bool IsCompletion => _isCompletion;

    /// <summary>
    /// Gets the optional state object.
    /// </summary>
    public object? State => _observer?.ReaderState;

    /// <summary>
    /// Stops the observer. No more notifications will be received.
    /// </summary>
    /// <remarks>No completion notification will be sent for the dispose.</remarks>
    public void Stop() => _observer?.StopByHandler();
}

/// <summary>
/// Notification that indicates a value was received or that the observer completed.
/// </summary>
/// <remarks>The completion notifications are limited to those specified with the <see cref="ObserverFlags"/>.</remarks>
/// <typeparam name="T">The type of the value that was read from the message.</typeparam>
public readonly struct Notification<T>
{
    private readonly InnerConnection.Observer? _observer;
    private readonly T _value;
    private readonly bool _isCompletion;

    internal Notification(InnerConnection.Observer observer, T value, bool isCompletion)
    {
        _observer = observer;
        _value = value;
        _isCompletion = isCompletion;
    }

    /// <summary>
    /// Returns an exception that indicates why the observer completed.
    /// </summary>
    /// <exception cref="InvalidOperationException">The notification is not a completion.</exception>
    public Exception Exception
    {
        get
        {
            if (!_isCompletion)
            {
                ThrowHelper.ThrowNotificationExceptionNotAvailable();
            }
            return _observer!.Exception!;
        }
    }

    /// <summary>
    /// Gets the type of notification.
    /// </summary>
    public NotificationType Type => _observer is null ? default : (_isCompletion ? _observer.ErrorType : NotificationType.Value);

    /// <summary>
    /// Returns whether a value was successfully read from the message.
    /// </summary>
    public bool HasValue => _observer is not null && !_isCompletion;

    /// <summary>
    /// Returns whether this is a completion notification.
    /// </summary>
    public bool IsCompletion => _isCompletion;

    /// <summary>
    /// Gets the value read from the message.
    /// </summary>
    /// <exception cref="InvalidOperationException">The notification is not a value as indicated by <see cref="HasValue"/>.</exception>
    public T Value
    {
        get
        {
            if (!HasValue)
            {
                ThrowHelper.ThrowNotificationValueNotAvailable();
            }
            return _value;
        }
    }

    /// <summary>
    /// Gets the optional state object.
    /// </summary>
    public object? State => _observer?.ReaderState;

    /// <summary>
    /// Stops the observer. No more notifications will be received.
    /// </summary>
    /// <remarks>No completion notification will be sent for the dispose.</remarks>
    public void Stop() => _observer?.StopByHandler();
}

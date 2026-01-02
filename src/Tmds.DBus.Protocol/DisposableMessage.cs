namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents a D-Bus message that must be disposed.
/// </summary>
public struct DisposableMessage : IDisposable
{
    private Message? _message;

    internal DisposableMessage(Message? message)
        => _message = message;

    /// <summary>
    /// Gets the underlying message.
    /// </summary>
    public Message Message
        => _message ?? throw new ObjectDisposedException(typeof(Message).FullName);

    /// <summary>
    /// Disposes the message.
    /// </summary>
    public void Dispose()
    {
        _message?.ReturnToPool();
        _message = null;
    }
}

namespace Tmds.DBus.Protocol;

/// <summary>
/// Tracks the current owner of a well-known name.
/// </summary>
/// <remarks>
/// <para>
/// On D-Bus, well-known names (like <c>org.freedesktop.NetworkManager</c>) can change owners when services restart or are replaced.
/// This class enables callers to detect owner changes so they can direct messages to a specific owner instance and react when that owner goes away.
/// </para>
/// <para>
/// The owner identifier string returned by <see cref="GetCurrentOwner"/> and <see cref="WaitForOwnerAsync"/> is an opaque string that combines the well-known name with the unique name of the current owner.
/// It can be used as the destination when making method calls or as the sender in a <see cref="MatchRule"/> to ensure communication is bound to a specific owner.
/// Use <see cref="GetOwnerBusName"/> to extract the unique bus name from the owner identifier.
/// </para>
/// </remarks>
public sealed class NameOwnerWatcher : IDisposable
{
    private readonly InnerConnection.Watcher _watcher;
    private readonly InnerConnection.ObservedName _senderName;
    private bool _disposed;

    internal NameOwnerWatcher(InnerConnection.Watcher watcher, InnerConnection.ObservedName senderName)
    {
        _watcher = watcher;
        _senderName = senderName;
    }

    /// <summary>
    /// Waits until the well-known name has an owner and returns the owner identifier.
    /// If the name already has an owner, returns immediately.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The owner identifier of the current owner.</returns>
    public Task<string> WaitForOwnerAsync(CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfDisposed(_disposed, this);
        return _watcher.WaitForOwnerAsync(_senderName, cancellationToken);
    }

    /// <summary>
    /// Returns the current owner identifier, or <c>null</c> if the well-known name has no owner or the connection has disconnected.
    /// </summary>
    public string? GetCurrentOwner()
    {
        ThrowHelper.ThrowIfDisposed(_disposed, this);
        return _watcher.GetCurrentOwner(_senderName);
    }

    /// <summary>
    /// Returns a <see cref="CancellationToken"/> that is cancelled when the owner changes from <paramref name="currentOwner"/>.
    /// If the owner has already changed or the connection has disconnected, the returned token is already cancelled.
    /// </summary>
    /// <param name="currentOwner">The expected current owner.</param>
    /// <returns>A cancellation token that is cancelled when the owner changes.</returns>
    public CancellationToken GetOwnerChangedCancellationToken(string currentOwner)
    {
        ThrowHelper.ThrowIfDisposed(_disposed, this);
        return _watcher.GetOwnerChangedCancellationToken(_senderName, currentOwner);
    }

    /// <summary>
    /// Extracts the unique bus name of the owner from an owner identifier string.
    /// </summary>
    /// <param name="ownerIdentifier">An owner identifier string as returned by <see cref="GetCurrentOwner"/> or <see cref="WaitForOwnerAsync"/>.</param>
    /// <returns>The unique bus name (e.g. <c>:1.42</c>).</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid owner identifier.</exception>
    public static string GetOwnerBusName(string ownerIdentifier)
    {
        if (!BusName.TrySplitOwnerIdentifier(ownerIdentifier, out ReadOnlySpan<char> uniqueId, out _))
        {
            throw new ArgumentException("The value is not a valid owner identifier.", nameof(ownerIdentifier));
        }
        return uniqueId.ToString();
    }

    /// <summary>
    /// Disposes the watcher and releases the subscription.
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe and must not be called concurrently with other instance methods.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _watcher.RemoveNameOwnerWatcherUser();
    }
}

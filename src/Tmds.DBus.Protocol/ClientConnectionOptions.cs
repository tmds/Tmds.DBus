namespace Tmds.DBus.Protocol;

/// <summary>
/// Configuration options for a D-Bus client connection.
/// </summary>
[Obsolete("Use DBusConnectionOptions instead.")]
public class ClientConnectionOptions : ConnectionOptions
{
    private string _address;

    /// <summary>
    /// Initializes a new instance of the ClientConnectionOptions class.
    /// </summary>
    /// <param name="address">The address to connect to.</param>
    public ClientConnectionOptions(string address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));
        _address = address;
    }

    /// <summary>
    /// Initializes a new instance of the ClientConnectionOptions class.
    /// </summary>
    protected internal ClientConnectionOptions()
    {
        _address = string.Empty;
    }

    /// <summary>
    /// Gets or sets a whether to automatically connect when the connection is first used.
    /// </summary>
    public bool AutoConnect { get; set; }

    internal bool IsShared { get; set; }

    /// <summary>
    /// Sets up the connection. This method may be overridden in a derived class.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A ValueTask containing the setup result.</returns>
    protected internal virtual ValueTask<ClientSetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<ClientSetupResult>(
            new ClientSetupResult(_address)
            {
                SupportsFdPassing = true,
                UserId = DBusEnvironment.UserId,
                MachineId = DBusEnvironment.MachineId
            });
    }

    /// <summary>
    /// Tears down the connection. This method may be overridden in a derived class.
    /// </summary>
    /// <param name="token">The <see cref="ClientSetupResult.TeardownToken"/> returned from <see cref="SetupAsync"/>.</param>
    protected internal virtual void Teardown(object? token)
    { }
}
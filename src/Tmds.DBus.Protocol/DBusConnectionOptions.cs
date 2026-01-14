namespace Tmds.DBus.Protocol;

/// <summary>
/// Configuration options for a D-Bus client connection.
/// </summary>
public class DBusConnectionOptions
{
    private string _address;

    /// <summary>
    /// Initializes a new instance of the DBusConnectionOptions class.
    /// </summary>
    /// <param name="address">The address to connect to.</param>
    public DBusConnectionOptions(string address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));
        _address = address;
    }

    /// <summary>
    /// Initializes a new instance of the DBusConnectionOptions class.
    /// </summary>
    protected internal DBusConnectionOptions()
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
    protected internal virtual ValueTask<SetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<SetupResult>(
            new SetupResult(_address)
            {
                SupportsFdPassing = true,
                UserId = DBusEnvironment.UserId,
                MachineId = DBusEnvironment.MachineId
            });
    }

    /// <summary>
    /// Tears down the connection. This method may be overridden in a derived class.
    /// </summary>
    /// <param name="token">The <see cref="SetupResult.TeardownToken"/> returned from <see cref="SetupAsync"/>.</param>
    protected internal virtual void Teardown(object? token)
    { }

    /// <summary>
    /// Represents the result of a D-Bus client setup operation.
    /// </summary>
    public class SetupResult
    {
        /// <summary>
        /// Initializes a new instance of the SetupResult class.
        /// </summary>
        /// <param name="address">The connection address.</param>
        public SetupResult(string address)
        {
            ConnectionAddress = address ?? throw new ArgumentNullException(nameof(address));
        }

        /// <summary>
        /// Initializes a new instance of the SetupResult class with an empty address.
        /// </summary>
        public SetupResult() :
            this("")
        { }

        /// <summary>
        /// Gets the connection address.
        /// </summary>
        public string ConnectionAddress { get;  }

        /// <summary>
        /// Gets or sets the teardown token used to clean up resources.
        /// </summary>
        public object? TeardownToken { get; set; }

        /// <summary>
        /// Gets or sets the user ID for the connection.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the machine ID for the connection.
        /// </summary>
        public string? MachineId { get; set; }

        /// <summary>
        /// Gets or sets whether the connection supports file descriptor passing.
        /// </summary>
        public bool SupportsFdPassing { get; set; }

        /// <summary>
        /// Gets or sets a connection stream.
        /// </summary>
        /// <remarks>
        /// When set, <see cref="SupportsFdPassing"/> and <see cref="ConnectionAddress"/> are ignored.
        /// The implementation assumes that it is safe to dispose the <see cref="Stream"/> while there are on-going reads/writes, and that these on-going operations will be aborted.
        /// </remarks>
        public Stream? ConnectionStream { get; set; }
    }
}

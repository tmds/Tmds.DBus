namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents the result of a D-Bus client setup operation.
/// </summary>
public class ClientSetupResult
{
    /// <summary>
    /// Initializes a new instance of the ClientSetupResult class.
    /// </summary>
    /// <param name="address">The connection address.</param>
    public ClientSetupResult(string address)
    {
        ConnectionAddress = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>
    /// Initializes a new instance of the ClientSetupResult class with an empty address.
    /// </summary>
    public ClientSetupResult() :
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
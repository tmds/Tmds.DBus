namespace Tmds.DBus.Protocol;

public class ClientSetupResult
{
    public ClientSetupResult(string address)
    {
        ConnectionAddress = address ?? throw new ArgumentNullException(nameof(address));
    }

    public ClientSetupResult() :
        this("")
    { }

    public string ConnectionAddress { get;  }

    public object? TeardownToken { get; set; }

    public string? UserId { get; set; }

    public string? MachineId { get; set; }

    public bool SupportsFdPassing { get; set; }

    // SupportsFdPassing and ConnectionAddress are ignored when this is set.
    // The implementation assumes that it is safe to Dispose the Stream
    // while there are on-going reads/writes, and that these on-going operations will be aborted.
    public Stream? ConnectionStream { get; set; }
}
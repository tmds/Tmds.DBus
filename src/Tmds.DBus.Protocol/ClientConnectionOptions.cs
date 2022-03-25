namespace Tmds.DBus.Protocol;

public class ClientConnectionOptions : ConnectionOptions
{
    private string _address;

    public ClientConnectionOptions(string address, SynchronizationContext? synchronizationContext = null)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));
        _address = address;
        SynchronizationContext = synchronizationContext;
    }

    protected ClientConnectionOptions()
    {
        _address = string.Empty;
    }

    public bool AutoConnect { get; set; }

    protected internal virtual ValueTask<ClientSetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<ClientSetupResult>(
            new ClientSetupResult(_address)
            {
                SupportsFdPassing = true,
                UserId = DBusEnvironment.UserId
            });
    }

    protected internal virtual void Teardown(object? token)
    { }

    public SynchronizationContext? SynchronizationContext { get; set; } = null;

    public bool RunContinuationsAsynchronously {get; set; } = false;
}
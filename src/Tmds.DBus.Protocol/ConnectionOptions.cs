namespace Tmds.DBus.Protocol;

public abstract class ConnectionOptions
{
    internal ConnectionOptions()
    { }

    public SynchronizationContext? SynchronizationContext { get; set; } = null;

    public bool RunContinuationsAsynchronously {get; set; } = false;
}
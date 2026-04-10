namespace Tmds.DBus.Protocol;

sealed class ObserverDisposedException : ObjectDisposedException
{
    public ObserverDisposedException() : base("Tmds.DBus.Protocol.Observer")
    { }
}

namespace Tmds.DBus.Protocol;

[Flags]
public enum AddMatchFlags
{
    None = 0,
    EmitOnConnectionDispose = 1,
    EmitOnObserverDispose = 2,
    NoSubscribe = 4
}
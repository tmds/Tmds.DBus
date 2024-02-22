namespace Tmds.DBus.Protocol;

public interface IDBusReadable
{
    void ReadFrom(ref Reader reader);
}

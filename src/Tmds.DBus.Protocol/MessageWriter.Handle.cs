namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteHandle(SafeHandle value)
    {
        int idx = _message.HandleCount;
        _message.AddHandle(value);
        WriteInt32(idx);
    }
}

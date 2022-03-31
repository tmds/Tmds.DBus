using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public T? ReadHandle<T>() where T : SafeHandle
    {
        int idx = (int)ReadUInt32();
        if (idx >= _handleCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (_handles is not null)
        {
            return _handles.RemoveHandle<T>(idx);
        }
        return null;
    }

    public IntPtr ReadHandleRaw()
    {
        int idx = (int)ReadUInt32();
        if (idx >= _handleCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (_handles is not null)
        {
            return _handles.DangerousGetHandle(idx);
        }
        return new IntPtr(-1);
    }
}

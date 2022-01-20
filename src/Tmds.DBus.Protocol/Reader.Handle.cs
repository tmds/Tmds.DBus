using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public T ReadHandle<T>() where T : notnull
    {
        throw new NotImplementedException();
    }

    public IntPtr ReadHandle(bool own)
    {
        throw new NotImplementedException();
        // int idx = (int)ReadUInt32();
        // IntPtr handle = (IntPtr)(-1);
        // if (_handles is not null)
        // {
        //     (handle, bool dispose) = _handles[idx];
        //     if (own)
        //     {
        //         _handles[idx] = (handle, false);
        //     }
        // }
        // return handle;
    }
}

namespace Tmds.DBus.Protocol;

sealed class UnixFdCollection : List<(IntPtr, bool)>, IDisposable
{
    public void Dispose()
    {
        DisposeHandles(Count);
        GC.SuppressFinalize(this);
    }

    ~UnixFdCollection()
    {
        DisposeHandles(Count);
    }

    public void DisposeHandles(int count)
    {
        for (int i = 0; i < count; i++)
        {
            (IntPtr handle, bool dispose) = this[i];
            if (dispose)
            {
                close(handle.ToInt32());
            }
        }
        RemoveRange(0, count);
    }

    [DllImport("libc")]
    private static extern void close(int fd);
}
namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    /// <summary>
    /// Reads a Unix file descriptor handle.
    /// </summary>
    /// <typeparam name="T">The SafeHandle type to read.</typeparam>
    /// <returns>The handle, or null if unavailable.</returns>
    public T? ReadHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle, new()
        => ReadHandleGeneric<T>();

    internal T? ReadHandleGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        int idx = (int)ReadUInt32();
        if (idx >= _handleCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (_handles is not null)
        {
            return _handles.ReadHandleGeneric<T>(idx);
        }
        return default(T);
    }

    /// <summary>
    /// Reads a Unix file descriptor handle as a raw IntPtr.
    /// </summary>
    /// <remarks>The handle is still owned (i.e. Disposed) by the <see cref="Message"/>.</remarks>
    public IntPtr ReadHandleRaw()
    {
        int idx = (int)ReadUInt32();
        if (idx >= _handleCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (_handles is not null)
        {
            return _handles.ReadHandleRaw(idx);
        }
        return new IntPtr(-1);
    }
}

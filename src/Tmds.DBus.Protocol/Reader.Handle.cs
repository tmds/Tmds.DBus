namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    /// <summary>
    /// Reads a Unix file descriptor handle.
    /// </summary>
    /// <typeparam name="T">The <see cref="SafeHandle"/> type to read.</typeparam>
    /// <returns>The handle, or <see langword="null"/> if <typeparamref name="T"/> is <see cref="SkipSafeHandle"/> or if file descriptor passing is not supported.</returns>
    /// <remarks>
    /// A handle can only be read once.
    /// To skip reading a handle, call <c>ReadHandle&lt;SkipSafeHandle&gt;()</c>, which will return <see langword="null"/> without consuming the underlying handle.
    /// </remarks>
    public T? ReadHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle, new()
        => ReadHandleGeneric<T>();

    internal T? ReadHandleGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        int idx = (int)ReadUInt32();
        if (_handles is null)
        {
            return default(T);
        }
        return _handles.ReadHandleGeneric<T>(idx);
    }

    /// <summary>
    /// Reads a Unix file descriptor handle as a raw IntPtr.
    /// </summary>
    /// <remarks>
    /// A handle can only be read once.
    /// The handle is still owned (i.e. Disposed) by the <see cref="Message"/>.
    /// To skip reading a handle, call <c>ReadHandle&lt;SkipSafeHandle&gt;()</c>, which will return <see langword="null"/> without consuming the underlying handle.
    /// </remarks>
    public IntPtr ReadHandleRaw()
    {
        int idx = (int)ReadUInt32();
        if (_handles is null)
        {
            return new IntPtr(-1);
        }
        return _handles.ReadHandleRaw(idx);
    }
}

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    /// <summary>
    /// Reads a Unix file descriptor handle.
    /// </summary>
    /// <typeparam name="T">The <see cref="SafeHandle"/> type to read. If <typeparamref name="T"/> is <see cref="SkipSafeHandle"/>, the method returns a disposed <see cref="SkipSafeHandle"/> instance without consuming the underlying handle.</typeparam>
    /// <remarks>
    /// A handle can only be read once. Use <see cref="SkipSafeHandle"/> to avoid consuming the handle.
    /// </remarks>
    /// <exception cref="DBusReadException">The file descriptor is not present in the message.</exception>
    /// <exception cref="DBusUnexpectedValueException">The handle was already read.</exception>
    public T ReadHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle, new()
        => ReadHandleGeneric<T>();

    internal T ReadHandleGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        int idx = (int)ReadUInt32();
        if (_handles is null)
        {
            ThrowHelper.ThrowReaderNoFileHandle();
        }
        return _handles.ReadHandleGeneric<T>(idx);
    }

    /// <summary>
    /// Reads a Unix file descriptor handle as a raw IntPtr.
    /// </summary>
    /// <remarks>
    /// A handle can only be read once.
    /// To skip reading a handle, call <c>ReadHandle&lt;SkipSafeHandle&gt;()</c>, which will return a disposed <see cref="SkipSafeHandle"/> instance without consuming the underlying handle.
    /// The handle is still owned (i.e. Disposed) by the <see cref="Message"/>.
    /// </remarks>
    /// <exception cref="DBusReadException">The file descriptor is not present in the message.</exception>
    /// <exception cref="DBusUnexpectedValueException">The handle was already read.</exception>
    public IntPtr ReadHandleRaw()
    {
        int idx = (int)ReadUInt32();
        if (_handles is null)
        {
            ThrowHelper.ThrowReaderNoFileHandle();
        }
        return _handles.ReadHandleRaw(idx);
    }
}

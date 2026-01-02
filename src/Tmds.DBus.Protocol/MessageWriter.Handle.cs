namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    /// <summary>
    /// Writes a file descriptor handle.
    /// </summary>
    /// <param name="value">The handle to write.</param>
    public void WriteHandle(SafeHandle value)
    {
        int idx = HandleCount;
        AddHandle(value);
        WriteInt32(idx);
    }

    /// <summary>
    /// Writes a variant-wrapped file descriptor handle.
    /// </summary>
    /// <param name="value">The handle to write.</param>
    public void WriteVariantHandle(SafeHandle value)
    {
        WriteSignature(Signature.UnixFd);
        WriteHandle(value);
    }

    private int HandleCount => _handles?.Count ?? 0;

    private void AddHandle(SafeHandle handle)
    {
        if (_handles is null)
        {
            _handles = new(isRawHandleCollection: false);
        }
        _handles.AddHandle(handle);
    }
}

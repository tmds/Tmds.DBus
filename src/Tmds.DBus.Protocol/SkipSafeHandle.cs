namespace Tmds.DBus.Protocol;

/// <summary>
/// A SafeHandle that can be used to skip reading a Unix file descriptor handle.
/// </summary>
/// <remarks>
/// When this type is used with handle reading methods, the handle will not be read and a <see cref="SkipSafeHandle"/>
/// instance will be returned instead. This allows skipping handles without consuming them.
/// </remarks>
public sealed class SkipSafeHandle : SafeHandle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SkipSafeHandle"/> class.
    /// </summary>
    public SkipSafeHandle() : base(new IntPtr(-1), false)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the handle is invalid.
    /// </summary>
    public override bool IsInvalid => true;

    /// <summary>
    /// Releases the handle.
    /// </summary>
    /// <returns>Always returns true.</returns>
    protected override bool ReleaseHandle()
    {
        return true;
    }
}

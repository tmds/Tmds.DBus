namespace Tmds.DBus.Protocol;

/// <summary>
/// D-Bus protocol message flags.
/// </summary>
[Flags]
public enum MessageFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The message does not expect a reply.
    /// </summary>
    NoReplyExpected = 1,
    /// <summary>
    /// Do not auto-start the service if it is not already started.
    /// </summary>
    NoAutoStart = 2,
    /// <summary>
    /// Indicates the caller is willing to wait for interactive authorization.
    /// </summary>
    AllowInteractiveAuthorization = 4
}
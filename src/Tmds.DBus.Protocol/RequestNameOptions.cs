namespace Tmds.DBus.Protocol;

/// <summary>
/// Options when requesting a D-Bus name.
/// </summary>
[Flags]
public enum RequestNameOptions : uint
{
    /// <summary>
    /// No options are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Allow other users to take over this name.
    /// </summary>
    AllowReplacement = 1,
    /// <summary>
    /// Replace the existing owner of this name.
    /// </summary>
    ReplaceExisting = 2,
    /// <summary>
    /// Default options: allow replacement and replace existing owner.
    /// </summary>
    Default = ReplaceExisting | AllowReplacement
}
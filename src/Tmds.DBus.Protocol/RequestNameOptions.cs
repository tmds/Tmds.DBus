namespace Tmds.DBus.Protocol;

[Flags]
public enum RequestNameOptions : uint
{
    None = 0,
    AllowReplacement = 1,
    ReplaceExisting = 2,
    Default = ReplaceExisting | AllowReplacement
}
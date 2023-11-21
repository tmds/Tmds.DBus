namespace Tmds.DBus.Protocol;

static class DBusEnvironment
{
    public static string? UserId
    {
        get
        {
            if (PlatformDetection.IsWindows())
            {
#if NET6_0_OR_GREATER
                return System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value;
#else
                throw new NotSupportedException("Cannot determine Windows UserId. You must manually assign it.");
#endif
            }
            else
            {
                return geteuid().ToString();
            }
        }
    }

    public static string MachineId
    {
        get
        {
            const string MachineUuidPath = @"/var/lib/dbus/machine-id";

            if (File.Exists(MachineUuidPath))
            {
                return Guid.Parse(File.ReadAllText(MachineUuidPath).Substring(0, 32)).ToString();
            }
            else
            {
                return Guid.Empty.ToString();
            }
        }
    }

    [DllImport("libc")]
    internal static extern uint geteuid();
}
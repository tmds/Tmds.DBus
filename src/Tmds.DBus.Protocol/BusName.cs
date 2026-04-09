namespace Tmds.DBus.Protocol;

static class BusName
{
    public static bool IsOwnerIdentifier(ReadOnlySpan<char> name) => name.EndsWith([')']);

    public static bool TrySplitOwnerIdentifier(ReadOnlySpan<char> name, out ReadOnlySpan<char> uniqueId, out ReadOnlySpan<char> serviceName)
    {
        // Parse "com.example.Foo(:1.42@1234567890)".
        int parenPos = name.IndexOf('(');
        if (parenPos > 0 && name.EndsWith([')']))
        {
            serviceName = name.Slice(0, parenPos);
            ReadOnlySpan<char> inner = name.Slice(parenPos + 1, name.Length - parenPos - 2);
            int atPos = inner.IndexOf('@');
            uniqueId = atPos > 0 ? inner.Slice(0, atPos) : inner;
            return uniqueId.Length > 0 && serviceName.Length > 0;
        }
        else
        {
            uniqueId = serviceName = default;
            return false;
        }
    }

    public static string FormatOwnerIdentifier(ReadOnlySpan<byte> uniqueId, string serviceName, long timestamp)
    {
        // Format "com.example.Foo(:1.42@1234567890)"
        return $"{serviceName}({Encoding.UTF8.GetString(uniqueId)}@{timestamp})";
    }
}

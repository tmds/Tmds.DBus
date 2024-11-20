namespace Tmds.DBus.Protocol;

static class ProtocolConstants
{
    public const int MaxSignatureLength = 256;
    public const int StructAlignment = 8;
    public const int UInt32Alignment = 4;

    private static ReadOnlySpan<byte> SingleTypes => new byte[] { (byte)'y', (byte)'b', (byte)'n', (byte)'q', (byte)'i', (byte)'u', (byte)'x', (byte)'t', (byte)'d', (byte)'h', (byte)'s', (byte)'o', (byte)'g', (byte)'v' };

    public static bool IsSingleCompleteType(byte b)
    {
        return SingleTypes.IndexOf(b) != -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetTypeAlignment(DBusType type)
    {
        switch (type)
        {
            case DBusType.Byte: return 1;
            case DBusType.Bool: return 4;
            case DBusType.Int16: return 2;
            case DBusType.UInt16: return 2;
            case DBusType.Int32: return 4;
            case DBusType.UInt32: return UInt32Alignment;
            case DBusType.Int64: return 8;
            case DBusType.UInt64: return 8;
            case DBusType.Double: return 8;
            case DBusType.String: return 4;
            case DBusType.ObjectPath: return 4;
            case DBusType.Signature: return 4;
            case DBusType.Array: return 4;
            case DBusType.Struct: return StructAlignment;
            case DBusType.Variant: return 1;
            case DBusType.DictEntry: return 8;
            case DBusType.UnixFd: return 4;
            default: return 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Align(int offset, int alignment)
    {
        return offset + GetPadding(offset, alignment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPadding(int offset, int alignment)
    {
        return (~offset + 1) & (alignment - 1);
    }
}
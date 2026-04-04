namespace Tmds.DBus.Protocol;

static class ThrowHelper
{
    public static void ThrowIfDisposed(bool condition, object instance)
    {
        if (condition)
        {
            ThrowObjectDisposedException(instance);
        }
    }

    [DoesNotReturn]
    private static void ThrowObjectDisposedException(object instance)
    {
        throw new ObjectDisposedException(instance?.GetType().FullName);
    }

    [DoesNotReturn]
    public static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    [DoesNotReturn]
    public static void ThrowNotSupportedException()
    {
        throw new NotSupportedException();
    }

    [DoesNotReturn]
    internal static void ThrowUnexpectedSignature(ReadOnlySpan<byte> signature, string expected)
    {
        throw new DBusReadException($"Unexpected signature: expected '{expected}', got '{SignatureToStringNoThrow(signature)}'.");
    }

    [DoesNotReturn]
    internal static void ThrowReaderUnexpectedEndOfData()
    {
        throw new DBusReadException("Unexpected end of data.");
    }

    [DoesNotReturn]
    internal static void ThrowReaderArrayLengthExceeded(uint length)
    {
        throw new DBusReadException($"Array length {length} exceeds the D-Bus maximum of {ProtocolConstants.MaxArrayLength} bytes.");
    }

    [DoesNotReturn]
    internal static void ThrowReaderInvalidUTF8()
    {
        throw new DBusReadException("Invalid UTF-8 sequence.");
    }

    [DoesNotReturn]
    internal static void ThrowReaderRecursionDepthExceeded()
    {
        throw new DBusReadException($"Variant recursion exceeds the allowed ({ProtocolConstants.MaxVariantRecursionDepth}).");
    }

    [DoesNotReturn]
    internal static void ThrowReaderNoFileHandle()
    {
        throw new DBusReadException("File handle not present.");
    }

    [DoesNotReturn]
    internal static void ThrowHandleAlreadyRead()
    {
        throw new DBusUnexpectedValueException("The handle was already read.");
    }

    [DoesNotReturn]
    internal static void ThrowConnectionClosedByPeer()
    {
        throw new IOException("Connection closed by peer");
    }

    internal static string SignatureToStringNoThrow(ReadOnlySpan<byte> signature)
    {
        try
        {
            return Encoding.UTF8.GetString(signature);
        }
        catch
        {
            return BitConverter.ToString(signature.ToArray());
        }
    }
}
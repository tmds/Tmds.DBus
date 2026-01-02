namespace Tmds.DBus.Protocol;

/// <summary>
/// D-Bus protocol data types.
/// </summary>
public enum DBusType : byte
{
    /// <summary>
    /// Invalid or unknown type.
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Unsigned 8-bit integer.
    /// </summary>
    Byte = (byte)'y',
    /// <summary>
    /// Boolean.
    /// </summary>
    Bool = (byte)'b',
    /// <summary>
    /// Signed 16-bit integer.
    /// </summary>
    Int16 = (byte)'n',
    /// <summary>
    /// Unsigned 16-bit integer.
    /// </summary>
    UInt16 = (byte)'q',
    /// <summary>
    /// Signed 32-bit integer.
    /// </summary>
    Int32 = (byte)'i',
    /// <summary>
    /// Unsigned 32-bit integer.
    /// </summary>
    UInt32 = (byte)'u',
    /// <summary>
    /// Signed 64-bit integer.
    /// </summary>
    Int64 = (byte)'x',
    /// <summary>
    /// Unsigned 64-bit integer.
    /// </summary>
    UInt64 = (byte)'t',
    /// <summary>
    /// Double-precision floating point.
    /// </summary>
    Double = (byte)'d',
    /// <summary>
    /// String.
    /// </summary>
    String = (byte)'s',
    /// <summary>
    /// D-Bus object path.
    /// </summary>
    ObjectPath = (byte)'o',
    /// <summary>
    /// D-Bus type signature.
    /// </summary>
    Signature = (byte)'g',
    /// <summary>
    /// Array.
    /// </summary>
    Array = (byte)'a',
    /// <summary>
    /// Struct.
    /// </summary>
    Struct = (byte)'(',
    /// <summary>
    /// Variant.
    /// </summary>
    Variant = (byte)'v',
    /// <summary>
    /// Dictionary entry.
    /// </summary>
    DictEntry = (byte)'{',
    /// <summary>
    /// File descriptor.
    /// </summary>
    UnixFd = (byte)'h',
}
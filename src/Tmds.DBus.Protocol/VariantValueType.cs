namespace Tmds.DBus.Protocol;

/// <summary>
/// Type of value stored in a <see cref="VariantValue"/>.
/// </summary>
public enum VariantValueType : byte
{
    /// <summary>
    /// Invalid or unknown type.
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Unsigned 8-bit integer.
    /// </summary>
    Byte = DBusType.Byte,
    /// <summary>
    /// Boolean.
    /// </summary>
    Bool = DBusType.Bool,
    /// <summary>
    /// Signed 16-bit integer.
    /// </summary>
    Int16 = DBusType.Int16,
    /// <summary>
    /// Unsigned 16-bit integer.
    /// </summary>
    UInt16 = DBusType.UInt16,
    /// <summary>
    /// Signed 32-bit integer.
    /// </summary>
    Int32 = DBusType.Int32,
    /// <summary>
    /// Unsigned 32-bit integer.
    /// </summary>
    UInt32 = DBusType.UInt32,
    /// <summary>
    /// Signed 64-bit integer.
    /// </summary>
    Int64 = DBusType.Int64,
    /// <summary>
    /// Unsigned 64-bit integer.
    /// </summary>
    UInt64 = DBusType.UInt64,
    /// <summary>
    /// Double-precision floating point.
    /// </summary>
    Double = DBusType.Double,
    /// <summary>
    /// String.
    /// </summary>
    String = DBusType.String,
    /// <summary>
    /// D-Bus object path.
    /// </summary>
    ObjectPath = DBusType.ObjectPath,
    /// <summary>
    /// D-Bus type signature.
    /// </summary>
    Signature = DBusType.Signature,
    /// <summary>
    /// Array.
    /// </summary>
    Array = DBusType.Array,
    /// <summary>
    /// Struct.
    /// </summary>
    Struct = DBusType.Struct,
    /// <summary>
    /// Nested variant value.
    /// </summary>
    Variant = DBusType.Variant,
    /// <summary>
    /// Dictionary.
    /// </summary>
    Dictionary = DBusType.DictEntry,
    /// <summary>
    /// File descriptor.
    /// </summary>
    UnixFd = DBusType.UnixFd,
}
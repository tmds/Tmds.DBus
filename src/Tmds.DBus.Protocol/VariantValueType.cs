namespace Tmds.DBus.Protocol;

public enum VariantValueType : byte
{
    Invalid = 0,
    Byte = DBusType.Byte,
    Bool = DBusType.Bool,
    Int16 = DBusType.Int16,
    UInt16 = DBusType.UInt16,
    Int32 = DBusType.Int32,
    UInt32 = DBusType.UInt32,
    Int64 = DBusType.Int64,
    UInt64 = DBusType.UInt64,
    Double = DBusType.Double,
    String = DBusType.String,
    ObjectPath = DBusType.ObjectPath,
    Signature = DBusType.Signature,
    Array = DBusType.Array,
    Struct = DBusType.Struct,
    Dictionary = DBusType.DictEntry,
    UnixFd = DBusType.UnixFd,
    Variant = DBusType.Variant,
}
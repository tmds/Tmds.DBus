namespace Tmds.DBus.Protocol;


public enum DBusValueType
{
    Invalid = 0,

    // DBusValue is used for a variant for which we read the value
    // and no longer track its signature.
    DBusValue = 1,

    //  Match the DBusType values for easy conversion.
    Byte = (byte)'y',
    Bool = (byte)'b',
    Int16 = (byte)'n',
    UInt16 = (byte)'q',
    Int32 = (byte)'i',
    UInt32 = (byte)'u',
    Int64 = (byte)'x',
    UInt64 = (byte)'t',
    Double = (byte)'d',

    String = (byte)'s',
    ObjectPath = (byte)'o',
    Signature = (byte)'g',

    Array = (byte)'a',
    Struct = (byte)'(',
    Dictionary = (byte)'{',

    UnixFd = (byte)'h',

    // We don't need this currently. Variants are resolved into the DBusValue.
    // Variant = (byte)'v',
}
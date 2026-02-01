// Global using for linked sources
global using System;
global using System.Runtime.CompilerServices;

// Required for record structs on older frameworks
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

// Not available for netstandard2.0
namespace Tmds.DBus.Tool
{
    static class EncodingExtensions
    {
        public static string GetString(this System.Text.Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            return encoding.GetString(bytes.ToArray());
        }
    }
}

// Used with nameof() in ProtocolGenerator
namespace Tmds.DBus.Protocol
{
    ref struct MessageWriter
    {
        public void WriteByte() => throw null!;
        public void WriteBool() => throw null!;
        public void WriteInt16() => throw null!;
        public void WriteUInt16() => throw null!;
        public void WriteInt32() => throw null!;
        public void WriteUInt32() => throw null!;
        public void WriteInt64() => throw null!;
        public void WriteUInt64() => throw null!;
        public void WriteDouble() => throw null!;
        public void WriteString() => throw null!;
        public void WriteObjectPath() => throw null!;
        public void WriteSignature() => throw null!;
        public void WriteVariant() => throw null!;
        public void WriteVariantByte() => throw null!;
        public void WriteVariantBool() => throw null!;
        public void WriteVariantInt16() => throw null!;
        public void WriteVariantUInt16() => throw null!;
        public void WriteVariantInt32() => throw null!;
        public void WriteVariantUInt32() => throw null!;
        public void WriteVariantInt64() => throw null!;
        public void WriteVariantUInt64() => throw null!;
        public void WriteVariantDouble() => throw null!;
        public void WriteVariantString() => throw null!;
        public void WriteVariantObjectPath() => throw null!;
        public void WriteVariantSignature() => throw null!;
        public void WriteHandle() => throw null!;
        public void WriteVariantHandle() => throw null!;
        public void WriteArray<T>() => throw null!;
        public void WriteDictionary<TKey, TValue>() => throw null!;
        public void WriteStruct<T>() => throw null!;
    }

    ref struct Reader
    {
        public static void ReadString() => throw null!;
        public static void ReadByte() => throw null!;
        public static void ReadBool() => throw null!;
        public static void ReadInt16() => throw null!;
        public static void ReadUInt16() => throw null!;
        public static void ReadInt32() => throw null!;
        public static void ReadUInt32() => throw null!;
        public static void ReadInt64() => throw null!;
        public static void ReadUInt64() => throw null!;
        public static void ReadDouble() => throw null!;
        public static void ReadObjectPath() => throw null!;
        public static void ReadSignature() => throw null!;
        public static void ReadVariantAsVariantValue() => throw null!;
        public static void ReadVariantValue() => throw null!;
        public static void ReadArrayOfByte() => throw null!;
        public static void ReadArrayOfBool() => throw null!;
        public static void ReadArrayOfInt16() => throw null!;
        public static void ReadArrayOfUInt16() => throw null!;
        public static void ReadArrayOfInt32() => throw null!;
        public static void ReadArrayOfUInt32() => throw null!;
        public static void ReadArrayOfInt64() => throw null!;
        public static void ReadArrayOfUInt64() => throw null!;
        public static void ReadArrayOfDouble() => throw null!;
        public static void ReadArrayOfString() => throw null!;
        public static void ReadArrayOfObjectPath() => throw null!;
        public static void ReadArrayOfSignature() => throw null!;
        public static void ReadArrayOfVariantValue() => throw null!;
        public static void ReadArrayOfHandle() => throw null!;
        public static void ReadDictionaryOfStringToVariantValue() => throw null!;
        public static void ReadHandle() => throw null!;
    }
}

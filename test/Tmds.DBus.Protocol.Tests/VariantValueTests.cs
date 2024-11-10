using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class VariantValueTests
{
    [Theory]
    [InlineData(byte.MinValue, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(byte.MaxValue, 0)]
    [InlineData(byte.MinValue, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(byte.MaxValue, 1)]
    public void Byte(Byte value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetByte());

        Assert.Equal(VariantValueType.Byte, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("y", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    public void Bool(bool value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetBool());

        Assert.Equal(VariantValueType.Bool, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("b", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(short.MinValue, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(short.MaxValue, 0)]
    [InlineData(short.MinValue, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(short.MaxValue, 1)]
    public void Int16(Int16 value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetInt16());

        Assert.Equal(VariantValueType.Int16, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("n", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(ushort.MinValue, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(ushort.MaxValue, 0)]
    [InlineData(ushort.MinValue, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(ushort.MaxValue, 1)]
    public void UInt16(UInt16 value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetUInt16());

        Assert.Equal(VariantValueType.UInt16, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("q", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(int.MinValue, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(int.MaxValue, 0)]
    [InlineData(int.MinValue, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(int.MaxValue, 1)]
    public void Int32(Int32 value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetInt32());

        Assert.Equal(VariantValueType.Int32, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("i", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(uint.MinValue, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(uint.MaxValue, 0)]
    [InlineData(uint.MinValue, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(uint.MaxValue, 1)]
    public void UInt32(UInt32 value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetUInt32());

        Assert.Equal(VariantValueType.UInt32, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("u", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(long.MinValue, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(long.MaxValue, 0)]
    [InlineData(long.MinValue, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(long.MaxValue, 1)]
    [InlineData(long.MinValue, 3)]
    [InlineData(-1, 3)]
    [InlineData(0, 3)]
    [InlineData(1, 3)]
    [InlineData(long.MaxValue, 3)]
    public void Int64(Int64 value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetInt64());

        Assert.Equal(VariantValueType.Int64, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("x", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(ulong.MinValue, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(ulong.MaxValue, 0)]
    [InlineData(ulong.MinValue, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(ulong.MaxValue, 1)]
    [InlineData(ulong.MinValue, 3)]
    [InlineData(0, 3)]
    [InlineData(1, 3)]
    [InlineData(ulong.MaxValue, 3)]
    public void UInt64(UInt64 value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetUInt64());

        Assert.Equal(VariantValueType.UInt64, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("t", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(double.MinValue, 0)]
    [InlineData(-1.0, 0)]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 0)]
    [InlineData(double.MaxValue, 0)]
    [InlineData(double.MinValue, 1)]
    [InlineData(-1.0, 1)]
    [InlineData(0.0, 1)]
    [InlineData(1.0, 1)]
    [InlineData(double.MaxValue, 1)]
    [InlineData(double.MinValue, 3)]
    [InlineData(-1.0, 3)]
    [InlineData(0.0, 3)]
    [InlineData(1.0, 3)]
    [InlineData(double.MaxValue, 3)]
    public void Double(Double value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetDouble());

        Assert.Equal(VariantValueType.Double, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("d", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData("test", 0)]
    [InlineData("test", 1)]
    public void String(String value, byte nesting)
    {
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(value, vv.GetString());

        Assert.Equal(VariantValueType.String, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("s", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData("/test/path", 0)]
    [InlineData("/test/path", 1)]
    public void ObjectPath(string s, byte nesting)
    {
        ObjectPath value = new ObjectPath(s);
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(s, vv.GetObjectPath());

        Assert.Equal(VariantValueType.ObjectPath, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("o", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData("sis", 0)]
    [InlineData("sis", 1)]
    public void Signature(string s, byte nesting)
    {
        Signature value = new Signature(Encoding.UTF8.GetBytes(s));
        VariantValue vv = nesting > 0 ? new(value, nesting) : new(value);
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(s, vv.GetSignature().ToString());

        Assert.Equal(VariantValueType.Signature, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("g", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Array(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(VariantValueType.String, new string[] { "1", "2" }, nesting)
                                      : new VariantValue(VariantValueType.String, new string[] { "1", "2" });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.String, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("as", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal("1", vv.GetItem(0).GetString());
        Assert.Equal("2", vv.GetItem(1).GetString());

        Assert.Equal(new[] { "1", "2" }, vv.GetArray<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfByte(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new byte[] { 1, 2 }, nesting)
                                      : new VariantValue(new byte[] { 1, 2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Byte, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("ay", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetByte());
        Assert.Equal(2, vv.GetItem(1).GetByte());

        Assert.Equal(new byte[] { 1, 2 }, vv.GetArray<byte>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfInt16(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new short[] { 1, 2 }, nesting)
                                      : new VariantValue(new short[] { 1, 2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Int16, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("an", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetInt16());
        Assert.Equal(2, vv.GetItem(1).GetInt16());

        Assert.Equal(new short[] { 1, 2 }, vv.GetArray<short>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfUInt16(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new ushort[] { 1, 2 }, nesting)
                                      : new VariantValue(new ushort[] { 1, 2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.UInt16, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("aq", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetUInt16());
        Assert.Equal(2, vv.GetItem(1).GetUInt16());

        Assert.Equal(new ushort[] { 1, 2 }, vv.GetArray<ushort>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfInt32(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new int[] { 1, 2 }, nesting)
                                      : new VariantValue(new int[] { 1, 2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Int32, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("ai", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetInt32());
        Assert.Equal(2, vv.GetItem(1).GetInt32());

        Assert.Equal(new int[] { 1, 2 }, vv.GetArray<int>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfUInt32(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new uint[] { 1, 2 }, nesting)
                                      : new VariantValue(new uint[] { 1, 2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.UInt32, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("au", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1U, vv.GetItem(0).GetUInt32());
        Assert.Equal(2U, vv.GetItem(1).GetUInt32());

        Assert.Equal(new uint[] { 1, 2 }, vv.GetArray<uint>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfInt64(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new long[] { 1, 2 }, nesting)
                                      : new VariantValue(new long[] { 1, 2 });;
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Int64, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("ax", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetInt64());
        Assert.Equal(2, vv.GetItem(1).GetInt64());

        Assert.Equal(new long[] { 1, 2 }, vv.GetArray<long>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfUInt64(byte nesting)
    {
        VariantValue vv = nesting > 0 ? new VariantValue(new ulong[] { 1, 2 }, nesting)
                                      : new VariantValue(new ulong[] { 1, 2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.UInt64, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("at", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1UL, vv.GetItem(0).GetUInt64());
        Assert.Equal(2UL, vv.GetItem(1).GetUInt64());

        Assert.Equal(new ulong[] { 1, 2 }, vv.GetArray<ulong>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ArrayOfDouble(byte nesting)
    {
        double d1 = Math.PI;
        double d2 = Math.E;

        VariantValue vv = nesting > 0 ? new VariantValue(new double[] { d1, d2 }, nesting)
                                      : new VariantValue(new double[] { d1, d2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Double, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("ad", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(d1, vv.GetItem(0).GetDouble());
        Assert.Equal(d2, vv.GetItem(1).GetDouble());

        Assert.Equal(new double[] { d1, d2 }, vv.GetArray<double>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Struct(byte nesting)
    {
        VariantValue vv = nesting > 0
                            ? new VariantValue(new VariantValue[] { new VariantValue((byte)1), new VariantValue("string") }, nesting)
                            : new VariantValue(new VariantValue[] { new VariantValue((byte)1), new VariantValue("string") });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Struct, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("(ys)", vv.Signature);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetByte());
        Assert.Equal("string", vv.GetItem(1).GetString());
    }

    [Fact]
    public void StructWithVariantFields()
    {
        VariantValue vv = new VariantValue(new VariantValue[] { new VariantValue((byte)1), VariantValue.CreateVariant(2), new VariantValue((short)3), new VariantValue("string") });

        Assert.Equal(VariantValueType.Struct, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("(yvns)", vv.Signature); // Variant field is reported as variant.

        Assert.Equal(4, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetByte());
        Assert.Equal(2, vv.GetItem(1).GetInt32()); // Variant field value is unwrapped.
        Assert.Equal(3, vv.GetItem(2).GetInt16());
        Assert.Equal("string", vv.GetItem(3).GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Dictionary(byte nesting)
    {
        var item1 = KeyValuePair.Create(new VariantValue((byte)1), new VariantValue("one"));
        var item2 = KeyValuePair.Create(new VariantValue((byte)2), new VariantValue("two"));
        VariantValue vv = nesting > 0
                            ? new VariantValue(VariantValueType.Byte, VariantValueType.String, valueSignature: null, new[] { item1, item2 }, nesting)
                            : new VariantValue(VariantValueType.Byte, VariantValueType.String, new[] { item1, item2 });
        UnwrapVariant(ref vv, nesting);

        Assert.Equal(VariantValueType.Dictionary, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Byte, vv.KeyType);
        Assert.Equal(VariantValueType.String, vv.ValueType);
        Assert.Equal("a{ys}", vv.Signature);

        Assert.Equal(2, vv.Count);

        var item = vv.GetDictionaryEntry(0);
        Assert.Equal(1, item.Key.GetByte());
        Assert.Equal("one", item.Value.GetString());

        item = vv.GetDictionaryEntry(1);
        Assert.Equal(2, item.Key.GetByte());
        Assert.Equal("two", item.Value.GetString());

        Dictionary<byte, string> dict = vv.GetDictionary<byte, string>();
        Assert.Equal(new Dictionary<byte, string>() { { 1, "one"}, { 2, "two" } }, dict);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void UnixFd(byte nesting)
    {
        byte handleIndex = 1;
        IntPtr expected = new IntPtr(-3);
        using UnixFdCollection fds = new UnixFdCollection(isRawHandleCollection: true);
        fds.AddHandle(new IntPtr(-2));
        fds.AddHandle(expected);

        VariantValue vv = nesting > 0
                            ? new VariantValue(fds, handleIndex, nesting)
                            : new VariantValue(fds, handleIndex);
        UnwrapVariant(ref vv, nesting);

        using var handle = vv.ReadHandle<SafeFileHandle>();

        Assert.Equal(expected, handle!.DangerousGetHandle());

        Assert.Equal(VariantValueType.UnixFd, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);
        Assert.Equal("h", vv.Signature);

        Assert.Equal(-1, vv.Count);
    }

    private static void UnwrapVariant(ref VariantValue vv, byte nesting)
    {
        for (int i = 0; i < nesting; i++)
        {
            Assert.Equal(VariantValueType.Variant, vv.Type);
            Assert.Equal(VariantValueType.Invalid, vv.ItemType);
            Assert.Equal(VariantValueType.Invalid, vv.KeyType);
            Assert.Equal(VariantValueType.Invalid, vv.ValueType);
            Assert.Equal("v", vv.Signature);

            vv = vv.GetVariantValue();
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class VariantValueTests
{
    [Theory]
    [InlineData(byte.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(byte.MaxValue)]
    public void Byte(Byte value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetByte());

        Assert.Equal(VariantValueType.Byte, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Bool(bool value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetBool());

        Assert.Equal(VariantValueType.Bool, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(short.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(short.MaxValue)]
    public void Int16(Int16 value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetInt16());

        Assert.Equal(VariantValueType.Int16, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(ushort.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(ushort.MaxValue)]
    public void UInt16(UInt16 value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetUInt16());

        Assert.Equal(VariantValueType.UInt16, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Int32(Int32 value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetInt32());

        Assert.Equal(VariantValueType.Int32, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(uint.MaxValue)]
    public void UInt32(UInt32 value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetUInt32());

        Assert.Equal(VariantValueType.UInt32, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(long.MaxValue)]
    public void Int64(Int64 value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetInt64());

        Assert.Equal(VariantValueType.Int64, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(ulong.MaxValue)]
    public void UInt64(UInt64 value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetUInt64());

        Assert.Equal(VariantValueType.UInt64, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData(double.MinValue)]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(double.MaxValue)]
    public void Double(Double value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetDouble());

        Assert.Equal(VariantValueType.Double, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData("test")]
    public void String(String value)
    {
        VariantValue vv = new(value);

        Assert.Equal(value, vv.GetString());

        Assert.Equal(VariantValueType.String, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData("/test/path")]
    public void ObjectPath(string s)
    {
        ObjectPath value = new ObjectPath(s);
        VariantValue vv = new(value);

        Assert.Equal(s, vv.GetObjectPath());

        Assert.Equal(VariantValueType.ObjectPath, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Theory]
    [InlineData("sis")]
    public void Signature(string s)
    {
        Signature value = new Signature(s);
        VariantValue vv = new(value);

        Assert.Equal(s, vv.GetSignature());

        Assert.Equal(VariantValueType.Signature, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Fact]
    public void Array()
    {
        VariantValue vv = new VariantValue(VariantValueType.String, new string[] { "1", "2" });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.String, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal("1", vv.GetItem(0).GetString());
        Assert.Equal("2", vv.GetItem(1).GetString());

        Assert.Equal(new[] { "1", "2" }, vv.GetArray<string>());
    }



    [Fact]
    public void ArrayOfByte()
    {
        VariantValue vv = new VariantValue(new byte[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Byte, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetByte());
        Assert.Equal(2, vv.GetItem(1).GetByte());

        Assert.Equal(new byte[] { 1, 2 }, vv.GetArray<byte>());
    }

    [Fact]
    public void ArrayOfInt16()
    {
        VariantValue vv = new VariantValue(new short[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Int16, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetInt16());
        Assert.Equal(2, vv.GetItem(1).GetInt16());

        Assert.Equal(new short[] { 1, 2 }, vv.GetArray<short>());
    }

    [Fact]
    public void ArrayOfUInt16()
    {
        VariantValue vv = new VariantValue(new ushort[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.UInt16, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetUInt16());
        Assert.Equal(2, vv.GetItem(1).GetUInt16());

        Assert.Equal(new ushort[] { 1, 2 }, vv.GetArray<ushort>());
    }

    [Fact]
    public void ArrayOfInt32()
    {
        VariantValue vv = new VariantValue(new int[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Int32, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetInt32());
        Assert.Equal(2, vv.GetItem(1).GetInt32());

        Assert.Equal(new int[] { 1, 2 }, vv.GetArray<int>());
    }

    [Fact]
    public void ArrayOfUInt32()
    {
        VariantValue vv = new VariantValue(new uint[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.UInt32, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1U, vv.GetItem(0).GetUInt32());
        Assert.Equal(2U, vv.GetItem(1).GetUInt32());

        Assert.Equal(new uint[] { 1, 2 }, vv.GetArray<uint>());
    }

    [Fact]
    public void ArrayOfInt64()
    {
        VariantValue vv = new VariantValue(new long[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Int64, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetInt64());
        Assert.Equal(2, vv.GetItem(1).GetInt64());

        Assert.Equal(new long[] { 1, 2 }, vv.GetArray<long>());
    }

    [Fact]
    public void ArrayOfUInt64()
    {
        VariantValue vv = new VariantValue(new ulong[] { 1, 2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.UInt64, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1UL, vv.GetItem(0).GetUInt64());
        Assert.Equal(2UL, vv.GetItem(1).GetUInt64());

        Assert.Equal(new ulong[] { 1, 2 }, vv.GetArray<ulong>());
    }



    [Fact]
    public void ArrayOfDouble()
    {
        double d1 = Math.PI;
        double d2 = Math.E;

        VariantValue vv = new VariantValue(new double[] { d1, d2 });

        Assert.Equal(VariantValueType.Array, vv.Type);
        Assert.Equal(VariantValueType.Double, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(d1, vv.GetItem(0).GetDouble());
        Assert.Equal(d2, vv.GetItem(1).GetDouble());

        Assert.Equal(new double[] { d1, d2 }, vv.GetArray<double>());
    }


    [Fact]
    public void Struct()
    {
        VariantValue vv = new VariantValue(new VariantValue[] { new VariantValue((byte)1), new VariantValue((byte)2) });

        Assert.Equal(VariantValueType.Struct, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(2, vv.Count);

        Assert.Equal(1, vv.GetItem(0).GetByte());
        Assert.Equal(2, vv.GetItem(1).GetByte());
    }

    [Fact]
    public void Dictionary()
    {
        VariantValue vv = new VariantValue(VariantValueType.Byte, VariantValueType.String,
            new[]
            {
                KeyValuePair.Create(new VariantValue(1), new VariantValue("one")),
                KeyValuePair.Create(new VariantValue(2), new VariantValue("two")),
            });

        Assert.Equal(VariantValueType.Dictionary, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Byte, vv.KeyType);
        Assert.Equal(VariantValueType.String, vv.ValueType);

        Assert.Equal(2, vv.Count);

        var item = vv.GetDictionaryEntry(0);
        Assert.Equal(1, item.Key.GetInt32());
        Assert.Equal("one", item.Value.GetString());

        item = vv.GetDictionaryEntry(1);
        Assert.Equal(2, item.Key.GetInt32());
        Assert.Equal("two", item.Value.GetString());

        Dictionary<byte, string> dict = vv.GetDictionary<byte, string>();
        Assert.Equal(new Dictionary<byte, string>() { { 1, "one"}, { 2, "two" } }, dict);
    }

    [Fact]
    public void UnixFd()
    {
        byte handleIndex = 1;
        IntPtr expected = new IntPtr(-3);
        using UnixFdCollection fds = new UnixFdCollection(isRawHandleCollection: true);
        fds.AddHandle(new IntPtr(-2));
        fds.AddHandle(expected);

        VariantValue vv = new VariantValue(fds, handleIndex);

        using var handle = vv.ReadHandle<SafeFileHandle>();

        Assert.Equal(expected, handle!.DangerousGetHandle());

        Assert.Equal(VariantValueType.UnixFd, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(-1, vv.Count);
    }

    [Fact]
    public void WrappedVariantValue()
    {
        VariantValue vv = new VariantValue(new VariantValue(1));

        Assert.Equal(VariantValueType.VariantValue, vv.Type);
        Assert.Equal(VariantValueType.Invalid, vv.ItemType);
        Assert.Equal(VariantValueType.Invalid, vv.KeyType);
        Assert.Equal(VariantValueType.Invalid, vv.ValueType);

        Assert.Equal(new VariantValue(1), vv.GetVariantValue());

        Assert.Equal(1, vv.GetVariantValue().GetInt32());
    }
}

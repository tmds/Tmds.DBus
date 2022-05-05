using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class WriterTests
{
    private delegate void WriteFunction<T>(ref MessageWriter writer, T value);

    [Theory]
    [InlineData(true, 4, new byte[] { 0, 0, 0, 1 },
                         new byte[] { 1, 0, 0, 0 })]
    public void WriteBool(bool value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, bool value) => writer.WriteBool(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((byte)5, 1, new byte[] { 5 },
                            new byte[] { 5 })]
    public void WriteByte(byte value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, byte value) => writer.WriteByte(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((short)0x0102, 2, new byte[] { 0x01, 0x02 },
                                  new byte[] { 0x02, 0x01 })]
    public void WriteInt16(short value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, short value) => writer.WriteInt16(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(0x01020304, 4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                               new byte[] { 0x04, 0x03, 0x02, 0x01 })]
    public void WriteInt32(int value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, int value) => writer.WriteInt32(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                       new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 })]
    public void WriteInt64(long value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, long value) => writer.WriteInt64(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(1.0, 8, new byte[] { 0x3f, 0xf0, 0, 0, 0, 0, 0, 0 },
                        new byte[] { 0, 0, 0, 0, 0, 0, 0xf0, 0x3f })]
    public void WriteDouble(double value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, double value) => writer.WriteDouble(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((ushort)0x0102, 2, new byte[] { 0x01, 0x02 },
                                   new byte[] { 0x02, 0x01 })]
    public void WriteUInt16(ushort value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, ushort value) => writer.WriteUInt16(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((uint)0x01020304, 4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                                  new byte[] { 0x04, 0x03, 0x02, 0x01 })]
    public void WriteUInt32(uint value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, uint value) => writer.WriteUInt32(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((ulong)0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                              new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 })]
    public void WriteUInt64(ulong value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, ulong value) => writer.WriteUInt64(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData("hw", 4, new byte[] { 0, 0, 0, 2, (byte)'h', (byte)'w', 0 },
                         new byte[] { 2, 0, 0, 0, (byte)'h', (byte)'w', 0 })]
    public void WriteString(string value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, string value) => writer.WriteString(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData("/a/b", 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                           new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 })]
    public void WriteObjectPath(string value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, string value) => writer.WriteObjectPath(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData("sis", 1, new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 },
                          new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 })]
    public void WriteSignature(string value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(value, (ref MessageWriter writer, string value) => writer.WriteSignature(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(new long[] { 1, 2 }, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                        new byte[] { 16, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 })]
    public void WriteArray(long[] value, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite<long[]>(value, (ref MessageWriter writer, long[] value) => writer.WriteArray(value), alignment, bigEndianData, littleEndianData);
        TestWrite<IEnumerable<long>>(value, (ref MessageWriter writer, IEnumerable<long> value) => writer.WriteArray(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0 },
                   new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0 })]
    public void WriteStructOf2(int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        var value = new ValueTuple<long, string> { Item1 = 1, Item2 = "hw" };
        TestWrite(value, (ref MessageWriter writer, (long, string) value) => writer.WriteStruct(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(4, new byte[] { 0, 0, 0, 28, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0 },
                   new byte[] { 28, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0 })]
    public void WriteDictionary(int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        var value = new Dictionary<byte, string>
        {
            { 1, "one" },
            { 2, "two" }
        };
        TestWrite<IEnumerable<KeyValuePair<byte, string>>>(value, (ref MessageWriter writer, IEnumerable<KeyValuePair<byte, string>> value) => writer.WriteDictionary(value), alignment, bigEndianData, littleEndianData);
        TestWrite<KeyValuePair<byte, string>[]>(value.AsEnumerable().ToArray(), (ref MessageWriter writer, KeyValuePair<byte, string>[] value) => writer.WriteDictionary(value), alignment, bigEndianData, littleEndianData);
        TestWrite<Dictionary<byte, string>>(value, (ref MessageWriter writer, Dictionary<byte, string> value) => writer.WriteDictionary(value), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(4, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
                   new byte[] { 0, 0, 0, 0, 1, 0, 0, 0 })]
    public void WriteHandle(int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        string filename = Path.GetTempFileName();
        try
        {
            var writer = CreateWriter();
            writer.WriteByte(0); // force alignment.

            var fs = File.OpenRead(filename);
            var handle = fs.SafeFileHandle;
            writer.WriteHandle(handle);
            writer.WriteHandle(handle);

            Assert.NotNull(writer.Handles);
            Assert.Equal(2, writer.Handles.Count);

            var data = writer.AsReadOnlySequence().Slice(alignment).ToArray();
            Assert.Equal(BitConverter.IsLittleEndian ? littleEndianData : bigEndianData, data);

            Assert.False(handle.IsClosed);
            writer.Dispose();
            Assert.True(handle.IsClosed);
        }
        finally
        {
            File.Delete(filename);
        }
    }

    [Theory, MemberData(nameof(WriteVariantTestDAta))]
    private void WriteVariant(object expected, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestWrite(expected, (ref MessageWriter writer, object value) => writer.WriteVariant(value), alignment: 0, bigEndianData, littleEndianData);
    }

    public static IEnumerable<object[]> WriteVariantTestDAta
    {
        get
        {
            var myDictionary = new Dictionary<byte, string>
            {
                { 1, "one" },
                { 2, "two" }
            };
            return new[]
            {
                new object[] {true,             new byte[] {1, 98, 0, 0, 0, 0, 0, 1},
                                                new byte[] {1, 98, 0, 0, 1, 0, 0, 0}},
                new object[] {(byte)5,          new byte[] {1, 121, 0, 5},
                                                new byte[] {1, 121, 0, 5}},
                new object[] {(short)0x0102,    new byte[] {1, 110, 0, 0, 1, 2},
                                                new byte[] {1, 110, 0, 0, 2, 1}},
                new object[] {0x01020304,       new byte[] {1, 105, 0, 0, 1, 2, 3, 4},
                                                new byte[] {1, 105, 0, 0, 4, 3, 2, 1}},
                new object[] {0x0102030405060708, new byte[] {1, 120, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                  new byte[] {1, 120, 0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1}},
                new object[] {1.0,              new byte[] {1, 100, 0, 0, 0, 0, 0, 0, 63, 240, 0, 0, 0, 0, 0, 0},
                                                new byte[] {1, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 63}},
                new object[] {(ushort)0x0102,   new byte[] {1, 113, 0, 0, 1, 2},
                                                new byte[] {1, 113, 0, 0, 2, 1}},
                new object[] {(uint)0x01020304, new byte[] {1, 117, 0, 0, 1, 2, 3, 4},
                                                new byte[] {1, 117, 0, 0, 4, 3, 2, 1}},
                new object[] {(ulong)0x0102030405060708, new byte[] {1, 116, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                         new byte[] {1, 116, 0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1}},
                new object[] {"hw",             new byte[] {1, 115, 0, 0, 0, 0, 0, 2, 104, 119, 0},
                                                new byte[] {1, 115, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                new object[] {new ObjectPath("/a/b"), new byte[] {1, 111, 0, 0, 0, 0, 0, 4, 47, 97, 47, 98, 0},
                                                      new byte[] {1, 111, 0, 0, 4, 0, 0, 0, 47, 97, 47, 98, 0}},
                new object[] {new Signature("sis"), new byte[] {1, 103, 0, 3, 115, 105, 115, 0},
                                                    new byte[] {1, 103, 0, 3, 115, 105, 115, 0}},
                new object[] {new long[] { 1, 2}, new byte[] {2, 97, 120, 0, 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2},
                                                  new byte[] {2, 97, 120, 0, 16, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0}},
                new object[] {new ValueTuple<long, string> { Item1 = 1, Item2 = "hw" }, new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                new object[] {myDictionary,     new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}},
                new object[] {((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8), new byte[] {10, 40, 121, 121, 121, 121, 121, 121, 121, 121, 41, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                                                        new byte[] {10, 40, 121, 121, 121, 121, 121, 121, 121, 121, 41, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8}},
            };
        }
    }

    private void TestWrite<T>(T value, WriteFunction<T> writeFunction, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        var writer = CreateWriter();
        if (alignment != 0)
        {
            writer.WriteByte(0); // force alignment.
        }
        writeFunction(ref writer, value);
        var data = writer.AsReadOnlySequence().Slice(alignment).ToArray();
        Assert.Equal(BitConverter.IsLittleEndian ? littleEndianData : bigEndianData, data);
        writer.Dispose();
    }

    private MessageWriter CreateWriter() => new MessageWriter(MessageBufferPool.Shared, serial: 0);
}
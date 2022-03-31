using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class ReaderTests
{
    private delegate T ReadFunction<T>(ref Reader reader);

    [Theory]
    [InlineData(true, 4, new byte[] { 0, 0, 0, 1 },
                         new byte[] { 1, 0, 0, 0 })]
    public void ReadBool(bool expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadBool(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(new long[] { 1, 2 }, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                       new byte[] { 16, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 })]
    public void ReadArray(long[] expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadArray<long>(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((byte)5, 1, new byte[] { 5 },
                            new byte[] { 5 })]
    public void ReadByte(byte expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadByte(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((ushort)0x0102, 2, new byte[] { 0x01, 0x02 },
                                   new byte[] { 0x02, 0x01 })]
    public void ReadUInt16(ushort expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadUInt16(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((short)0x0102, 2, new byte[] { 0x01, 0x02 },
                                  new byte[] { 0x02, 0x01 })]
    public void ReadInt16(short expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadInt16(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((uint)0x01020304, 4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                                     new byte[] { 0x04, 0x03, 0x02, 0x01 })]
    public void ReadUInt32(uint expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadUInt32(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(0x01020304, 4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                               new byte[] { 0x04, 0x03, 0x02, 0x01 })]
    public void ReadInt32(int expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadInt32(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData((ulong)0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                              new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 })]
    public void ReadUInt64(ulong expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadUInt64(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                       new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 })]
    public void ReadInt64(long expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadInt64(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(1.0, 8, new byte[] { 0x3f, 0xf0, 0, 0, 0, 0, 0, 0 },
                        new byte[] { 0, 0, 0, 0, 0, 0, 0xf0, 0x3f })]
    public void ReadDouble(double expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadDouble(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData("sis", 1, new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 },
                          new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 })]
    public void ReadSignature(string expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadSignature().ToString(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData("/a/b", 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                              new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 })]
    public void ReadObjectPath(string expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadObjectPathAsSpan().ToString(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData("hw", 4, new byte[] { 0, 0, 0, 2, (byte)'h', (byte)'w', 0 },
                         new byte[] { 2, 0, 0, 0, (byte)'h', (byte)'w', 0 })]
    public void ReadString(string expected, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadString(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(8, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0 },
                   new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0 })]
    public void ReadStructOf2(int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        var expected = new ValueTuple<long, string> { Item1 = 1, Item2 = "hw" };
        TestRead(expected, (ref Reader reader) => reader.ReadStruct<long, string>(), alignment, bigEndianData, littleEndianData);
    }

    [Theory]
    [InlineData(4, new byte[] { 0, 0, 0, 28, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0 },
                   new byte[] { 28, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0 })]
    public void ReadDictionary(int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        Dictionary<byte, string> expected = new()
        {
            { 1, "one" },
            { 2, "two" }
        };
        TestRead(expected, (ref Reader reader) => reader.ReadDictionary<byte, string>(), alignment, bigEndianData, littleEndianData);
    }

    [Fact]
    public void ReadHandle()
    {
        byte handleIndex = 1;
        IntPtr expected = new IntPtr(-3);
        using UnixFdCollection fds = new UnixFdCollection(isRawHandleCollection: true);
        fds.AddHandle(new IntPtr(-2));
        fds.AddHandle(expected);
        byte[] bigEndianData = new byte[] { 0, 0, 0, handleIndex };
        Assert.Equal(expected, fds.DangerousGetHandle(handleIndex));

        Reader reader = new Reader(isBigEndian: true, new System.Buffers.ReadOnlySequence<byte>(bigEndianData), handles: fds);
        using var handle = reader.ReadHandle<SafeFileHandle>();

        Assert.Equal(expected, handle!.DangerousGetHandle());
    }

    [Theory, MemberData(nameof(ReadVariantTestData))]
    private void ReadVariant(object expected, byte[] bigEndianData, byte[] littleEndianData)
    {
        TestRead(expected, (ref Reader reader) => reader.ReadVariant(), alignment: 0, bigEndianData, littleEndianData);
    }

    public static IEnumerable<object[]> ReadVariantTestData
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

    private void TestRead<T>(T expected, ReadFunction<T> readFunction, int alignment, byte[] bigEndianData, byte[] littleEndianData)
    {
        foreach (bool isBigEndian in new[] { true, false })
        {
            byte[] data = isBigEndian ? bigEndianData : littleEndianData;
            data = new byte[alignment].Concat(data).ToArray();

            Reader reader = new Reader(isBigEndian, new System.Buffers.ReadOnlySequence<byte>(data), handles: null);

            // Require the read function to align.
            if (alignment != 0)
            {
                reader.ReadByte();
            }

            // Check expected value.
            T value = readFunction(ref reader);
            Assert.Equal(expected, value);

            // Check all data was read.
            try
            {
                reader.ReadByte();
                Assert.False(true, "Not all data was read.");
            }
            catch (IndexOutOfRangeException)
            { }
        }
    }
}
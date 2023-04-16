using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class WriterTests
    {
        public enum MyEnum : short
        {
            Value = 0x3145
        }
        public class MyDBusObject : IDBusObject
        {
            public ObjectPath ObjectPath { get { return new ObjectPath("/a/b"); } }
        }
        public struct MyStruct
        {
            public long   v1;
            public string v2;
        }
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public class MyClass
        {
            public long   v1;
            public string v2;
        }

        struct EmptyStruct
        {}

        private static MethodInfo s_createWriteDelegateMethod = typeof(WriteMethodFactory)
            .GetMethod(nameof(WriteMethodFactory.CreateWriteMethodDelegate), BindingFlags.Static | BindingFlags.Public);

        [InlineData(typeof(sbyte))]
        [InlineData(typeof(EmptyStruct))]
        [InlineData(typeof(MyDBusObject))]
        [Theory]
        public void Invalid(Type type)
        {
            var method = s_createWriteDelegateMethod.MakeGenericMethod(type);
            Exception exception = null;
            try
            {
                method.Invoke(null, null);
            }
            catch (TargetInvocationException tie)
            {
                exception = tie.InnerException;
            }
            Assert.IsAssignableFrom<ArgumentException>(exception);
            Assert.Throws<ArgumentException>(() => WriteMethodFactory.CreateWriteMethodForType(type, true));
        }

        [Theory, MemberData(nameof(WriteTestData))]
        public void BigEndian(
            Type   type,
            object writeValue,
            int    alignment,
            byte[] bigEndianData,
            byte[] littleEndianData)
        {
            // ignore
            _ = alignment;
            _ = littleEndianData;

            // via Delegate
            {
                MessageWriter writer = new MessageWriter(EndianFlag.Big);
                var method = s_createWriteDelegateMethod.MakeGenericMethod(type);
                var writeMethodDelegate = (Delegate)method.Invoke(null, null);
                writeMethodDelegate.DynamicInvoke(new object[] { writer, writeValue });
                var bytes = writer.ToArray();
                Assert.Equal(bigEndianData, bytes);
            }
            // via WriteMethod
            {
                MessageWriter writer = new MessageWriter(EndianFlag.Big);
                var writeMethodInfo = WriteMethodFactory.CreateWriteMethodForType(type, true);
                if (writeMethodInfo.IsStatic)
                {
                    writeMethodInfo.Invoke(null, new object[] { writer, writeValue });
                }
                else
                {
                    writeMethodInfo.Invoke(writer, new object[] { writeValue });
                }
                var bytes = writer.ToArray();
                Assert.Equal(bigEndianData, bytes);
            }
        }

        [Theory, MemberData(nameof(WriteTestData))]
        public void LittleEndian(
            Type   type,
            object writeValue,
            int    alignment,
            byte[] bigEndianData,
            byte[] littleEndianData)
        {
            // ignore
            _ = alignment;
            _ = bigEndianData;

            // via Delegate
            {
                MessageWriter writer = new MessageWriter(EndianFlag.Little);
                var method = s_createWriteDelegateMethod.MakeGenericMethod(type);
                var writeMethodDelegate = (Delegate)method.Invoke(null, null);
                writeMethodDelegate.DynamicInvoke(new object[] { writer, writeValue });
                var bytes = writer.ToArray();
                Assert.Equal(littleEndianData, bytes);
            }
            // via WriteMethod
            {
                MessageWriter writer = new MessageWriter(EndianFlag.Little);
                var writeMethodInfo = WriteMethodFactory.CreateWriteMethodForType(type, true);
                if (writeMethodInfo.IsStatic)
                {
                    writeMethodInfo.Invoke(null, new object[] { writer, writeValue });
                }
                else
                {
                    writeMethodInfo.Invoke(writer, new object[] { writeValue });
                }
                var bytes = writer.ToArray();
                Assert.Equal(littleEndianData, bytes);
            }
        }

        public enum Gender
        {
            Male,
            Femable
        }

        [Dictionary]
        public class PersonProperties
        {
            public string Name;
            public int? Age;
            public Gender? Gender;
            public bool IsMarried;
        }

        public static IEnumerable<object[]> WriteTestData
        {
            get
            {
                var myDictionary = new Dictionary<byte, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                var john = new PersonProperties()
                {
                    Name = "John",
                    Age = 32,
                    Gender = Gender.Male,
                    IsMarried = true
                };
                var unknownPerson = new PersonProperties()
                {};
                return new []
                {
                    new object[] {typeof(MyEnum), MyEnum.Value,     2, new byte[] { 0x31, 0x45 },
                                                                    new byte[] { 0x45, 0x31 }},
                    new object[] {typeof(bool), true,               4, new byte[] { 0, 0, 0, 1 },
                                                                    new byte[] { 1, 0, 0, 0 }},
                    new object[] {typeof(byte), (byte)5,            1, new byte[] { 5 },
                                                                    new byte[] { 5 }},
                    new object[] {typeof(short), (short)0x0102,     2, new byte[] { 0x01, 0x02 },
                                                                    new byte[] { 0x02, 0x01 }},
                    new object[] {typeof(int), 0x01020304,          4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                                                                    new byte[] { 0x04, 0x03, 0x02, 0x01 }},
                    new object[] {typeof(long), 0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                                                    new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }},
                    new object[] {typeof(double), 1.0,              8, new byte[] { 0x3f, 0xf0, 0, 0, 0, 0, 0, 0 },
                                                                    new byte[] { 0, 0, 0, 0, 0, 0, 0xf0, 0x3f }},
                    new object[] {typeof(float), (float)1.0,        4, new byte[] { 0x3f, 0x80, 0, 0},
                                                                    new byte[] { 0, 0, 0x80, 0x3f }},
                    new object[] {typeof(ushort), (ushort)0x0102,   2, new byte[] { 0x01, 0x02 },
                                                                    new byte[] { 0x02, 0x01 }},
                    new object[] {typeof(uint), (uint)0x01020304,   4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                                                                    new byte[] { 0x04, 0x03, 0x02, 0x01 }},
                    new object[] {typeof(ulong), (ulong)0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                                                            new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }},
                    new object[] {typeof(string), "hw",             4, new byte[] { 0, 0, 0, 2, (byte)'h', (byte)'w', 0 },
                                                                    new byte[] { 2, 0, 0, 0, (byte)'h', (byte)'w', 0 }},
                    new object[] {typeof(ObjectPath), new ObjectPath("/a/b"), 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                                new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 }},
                    new object[] {typeof(Signature), new Signature("sis"), 1, new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 },
                                                                            new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 }},
                    new object[] {typeof(IDBusObject), new MyDBusObject(), 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                            new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 }},
                    new object[] {typeof(IEnumerable<long>), new long[] { 1, 2}, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                                                                    new byte[] { 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 }},
                    new object[] {typeof(long[]), new long[] { 1, 2}, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                                                        new byte[] { 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 }},
                    new object[] {typeof(MyStruct), new MyStruct { v1 = 1, v2 = "hw" }, 8, new byte[] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                    new object[] {typeof(MyClass), new MyClass { v1 = 1, v2 = "hw" }, 8, new byte[] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                    new object[] {typeof(IEnumerable<KeyValuePair<byte, string>>), myDictionary, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                                    new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}},
                    new object[] {typeof(IDictionary<byte, string>), myDictionary, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                    new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}},
                    new object[] {typeof(PersonProperties), john, 4,   new byte[] {0, 0, 0, 88, 0, 0, 0, 0, 0, 0, 0, 4, 78, 97, 109, 101, 0, 1, 115, 0, 0, 0, 0, 4, 74, 111, 104, 110, 0, 0, 0, 0, 0, 0, 0, 3, 65, 103, 101, 0, 1, 105, 0,
                                                                                   0, 0, 0, 0, 32, 0, 0, 0, 6, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 1},
                                                                       new byte[] {88, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 78, 97, 109, 101, 0, 1, 115, 0, 4, 0, 0, 0, 74, 111, 104, 110, 0, 0, 0, 0, 3, 0, 0, 0, 65, 103, 101, 0, 1, 105, 0,
                                                                                   0, 32, 0, 0, 0, 6, 0, 0, 0, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 1, 0, 0, 0}},
                    new object[] {typeof(PersonProperties), unknownPerson, 4, new byte[] {0, 0, 0, 24, 0, 0, 0, 0, 0, 0, 0, 9, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 0},
                                                                              new byte[] {24, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 0}},
                    new object[] {typeof(ValueTuple<int, int>), (5, 4), 8, new byte[] {0, 0, 0, 5, 0, 0, 0, 4}, new byte[] {5, 0, 0, 0, 4, 0, 0, 0}},
                    new object[] {typeof((byte, byte, byte, byte, byte, byte, byte, byte)), ((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8), 8, new byte[] {1, 2, 3, 4, 5, 6, 7, 8}, new byte[] {1, 2, 3, 4, 5, 6, 7, 8}},
                    new object[] {typeof(object), MyEnum.Value,     2, new byte[] {1, 110, 0, 0, 49, 69},
                                                                    new byte[] {1, 110, 0, 0, 69, 49}},
                    new object[] {typeof(object), true,             4, new byte[] {1, 98, 0, 0, 0, 0, 0, 1},
                                                                    new byte[] {1, 98, 0, 0, 1, 0, 0, 0}},
                    new object[] {typeof(object), (byte)5,          1, new byte[] {1, 121, 0, 5},
                                                                    new byte[] {1, 121, 0, 5}},
                    new object[] {typeof(object), (short)0x0102,    2, new byte[] {1, 110, 0, 0, 1, 2},
                                                                    new byte[] {1, 110, 0, 0, 2, 1}},
                    new object[] {typeof(object), 0x01020304,       4, new byte[] {1, 105, 0, 0, 1, 2, 3, 4},
                                                                    new byte[] {1, 105, 0, 0, 4, 3, 2, 1}},
                    new object[] {typeof(object), 0x0102030405060708, 8, new byte[] {1, 120, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                        new byte[] {1, 120, 0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1}},
                    new object[] {typeof(object), 1.0,              8, new byte[] {1, 100, 0, 0, 0, 0, 0, 0, 63, 240, 0, 0, 0, 0, 0, 0},
                                                                    new byte[] {1, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 63}},
                    new object[] {typeof(object), (float)1.0,       4, new byte[] {1, 102, 0, 0, 63, 128, 0, 0},
                                                                    new byte[] {1, 102, 0, 0, 0, 0, 128, 63}},
                    new object[] {typeof(object), (ushort)0x0102,   2, new byte[] {1, 113, 0, 0, 1, 2},
                                                                    new byte[] {1, 113, 0, 0, 2, 1}},
                    new object[] {typeof(object), (uint)0x01020304, 4, new byte[] {1, 117, 0, 0, 1, 2, 3, 4},
                                                                    new byte[] {1, 117, 0, 0, 4, 3, 2, 1}},
                    new object[] {typeof(object), (ulong)0x0102030405060708, 8, new byte[] {1, 116, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                                new byte[] {1, 116, 0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1}},
                    new object[] {typeof(object), "hw",             4, new byte[] {1, 115, 0, 0, 0, 0, 0, 2, 104, 119, 0},
                                                                    new byte[] {1, 115, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                    new object[] {typeof(object), new ObjectPath("/a/b"), 4, new byte[] {1, 111, 0, 0, 0, 0, 0, 4, 47, 97, 47, 98, 0},
                                                                            new byte[] {1, 111, 0, 0, 4, 0, 0, 0, 47, 97, 47, 98, 0}},
                    new object[] {typeof(object), new Signature("sis"), 1, new byte[] {1, 103, 0, 3, 115, 105, 115, 0},
                                                                        new byte[] {1, 103, 0, 3, 115, 105, 115, 0}},
                    new object[] {typeof(object), new MyDBusObject(), 4, new byte[] {1, 111, 0, 0, 0, 0, 0, 4, 47, 97, 47, 98, 0},
                                                                        new byte[] {1, 111, 0, 0, 4, 0, 0, 0, 47, 97, 47, 98, 0}},
                    new object[] {typeof(object), new long[] { 1, 2}, 4, new byte[] {2, 97, 120, 0, 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2},
                                                                        new byte[] {2, 97, 120, 0, 16, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0}},
                    new object[] {typeof(object), new MyStruct { v1 = 1, v2 = "hw" }, 8, new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                    new object[] {typeof(object), new MyClass { v1 = 1, v2 = "hw" }, 8, new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}},
                    new object[] {typeof(object), myDictionary,     4, new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                    new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}},
                    new object[] {typeof(object), myDictionary,     4, new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                    new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}},

                    new object[] {typeof(object), john, 4,          new byte[] {5, 97, 123, 115, 118, 125, 0, 0, 0, 0, 0, 88, 0, 0, 0, 0, 0, 0, 0, 4, 78, 97, 109, 101, 0, 1, 115, 0, 0, 0, 0, 4, 74, 111, 104, 110, 0, 0, 0, 0, 0, 0, 0, 3,
                                                                                65, 103, 101, 0, 1, 105, 0, 0, 0, 0, 0, 32, 0, 0, 0, 6, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 1},
                                                                    new byte[] {5, 97, 123, 115, 118, 125, 0, 0, 88, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 78, 97, 109, 101, 0, 1, 115, 0, 4, 0, 0, 0, 74, 111, 104, 110, 0, 0, 0, 0, 3, 0, 0, 0,
                                                                                65, 103, 101, 0, 1, 105, 0, 0, 32, 0, 0, 0, 6, 0, 0, 0, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 1, 0, 0, 0}},
                    new object[] {typeof(object), unknownPerson, 4, new byte[] {5, 97, 123, 115, 118, 125, 0, 0, 0, 0, 0, 24, 0, 0, 0, 0, 0, 0, 0, 9, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 0},
                                                                    new byte[] {5, 97, 123, 115, 118, 125, 0, 0, 24, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 0}},
                    new object[] {typeof(object), (5, 4), 8, new byte[] {4, 40, 105, 105, 41, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 4}, new byte[] {4, 40, 105, 105, 41, 0, 0, 0, 5, 0, 0, 0, 4, 0, 0, 0}},
                    new object[] {typeof(object), ((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8), 8, new byte[] {10, 40, 121, 121, 121, 121, 121, 121, 121, 121, 41, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                                                                               new byte[] {10, 40, 121, 121, 121, 121, 121, 121, 121, 121, 41, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8}},
                };
            }
        }
    }
}
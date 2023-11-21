using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class ReaderTests
    {
        private class MyProxyFactory : IProxyFactory
        {
            public T CreateProxy<T>(string serviceName, ObjectPath objectPath)
            {
                if (typeof(T) == typeof(IStringOperations))
                {
                    return (T)(object)new StringOperations();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
        public class MyDBusObject : IDBusObject
        {
            private ObjectPath _path;
            public MyDBusObject(ObjectPath path)
            {
                _path = path;
            }
            public MyDBusObject()
            {
                _path = new ObjectPath("/a/b");
            }
            public ObjectPath ObjectPath { get { return _path; } }
        }
        public class DBusObjectComparer : IEqualityComparer<object>
        {
            public new bool Equals(object lhs, object rhs)
            {
                var busObjectLhs = lhs as IDBusObject;
                var busObjectRhs = rhs as IDBusObject;
                if ((busObjectLhs == null) || (busObjectRhs == null))
                {
                    return false;
                }
                return busObjectLhs.ObjectPath.Equals(busObjectRhs.ObjectPath);
            }
            public int GetHashCode(object o)
            {
                var busObject = o as IDBusObject;
                if (busObject == null)
                {
                    return 0;
                }
                return busObject.ObjectPath.GetHashCode();
            }
        }
        public class ObjectPathComparer : IEqualityComparer<object>
        {
            public new bool Equals(object lhs, object rhs)
            {
                var busObjectLhs = lhs as IDBusObject;
                var objectPathRhs = rhs as ObjectPath?;
                if ((busObjectLhs == null) || (objectPathRhs == null))
                {
                    return false;
                }
                return busObjectLhs.ObjectPath.Equals(objectPathRhs);
            }
            public int GetHashCode(object o)
            {
                var busObject = o as IDBusObject;
                if (busObject == null)
                {
                    return 0;
                }
                return busObject.ObjectPath.GetHashCode();
            }
        }
        public enum MyEnum : short
        {
            Value = 0x3145
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
            public override bool Equals(object rhs)
            {
                var other = rhs as MyClass;
                if (other == null)
                {
                    return false;
                }
                return (v1 == other.v1) &&
                       (v2 == other.v2);
            }

            public override int GetHashCode()
            {
                return v1.GetHashCode() ^ v2.GetHashCode();
            }
        }

        private static MethodInfo s_createReadDelegateMethod = typeof(ReadMethodFactory)
            .GetMethod(nameof(ReadMethodFactory.CreateReadMethodDelegate), BindingFlags.Static | BindingFlags.Public);

        private MessageReader CreateMessageReader(EndianFlag endianFlag, byte[] data)
        {
            var message = new Message(
                new Header(MessageType.Invalid, endianFlag)
                {
                    Sender = string.Empty
                },
                body: data,
                unixFds: null
            );
            return new MessageReader(message, new MyProxyFactory());
        }

        struct EmptyStruct
        {}

        [InlineData(typeof(sbyte))]
        [InlineData(typeof(EmptyStruct))]
        [InlineData(typeof(MyDBusObject))]
        [Theory]
        public void Invalid(Type type)
        {
            var method = s_createReadDelegateMethod.MakeGenericMethod(type);
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
            Assert.Throws<ArgumentException>(() => ReadMethodFactory.CreateReadMethodForType(type));
        }

        [Theory, MemberData(nameof(ReadTestData))]
        public void BigEndian(
            Type   type,
            object expectedValue,
            int    alignment,
            byte[] bigEndianData,
            byte[] littleEndianData,
            IEqualityComparer<object> returnComparer)
        {
            // ignore
            _ = alignment;
            _ = littleEndianData;

            // via Delegate
            {
                MessageReader reader = CreateMessageReader(EndianFlag.Big, bigEndianData);
                var method = s_createReadDelegateMethod.MakeGenericMethod(type);
                var readMethodDelegate = (Delegate)method.Invoke(null, null);
                var readValue = readMethodDelegate.DynamicInvoke(new object[] { reader });
                Assert.IsAssignableFrom(type, readValue);
                if (returnComparer != null)
                {
                    Assert.Equal(expectedValue, readValue, returnComparer);
                }
                else
                {
                    Assert.Equal(expectedValue, readValue);
                }
            }
            // via ReadMethod
            {
                if (type.GetTypeInfo().IsEnum)
                {
                    // TODO
                    return;
                }
                MessageReader reader = CreateMessageReader(EndianFlag.Big, bigEndianData);
                var ReadMethodInfo = ReadMethodFactory.CreateReadMethodForType(type);
                object readValue;
                if (ReadMethodInfo.IsStatic)
                {
                    readValue = ReadMethodInfo.Invoke(null, new object[] { reader });
                }
                else
                {
                    readValue = ReadMethodInfo.Invoke(reader, null);
                }
                Assert.IsAssignableFrom(type, readValue);
                if (returnComparer != null)
                {
                    Assert.Equal(expectedValue, readValue, returnComparer);
                }
                else
                {
                    Assert.Equal(expectedValue, readValue);
                }
            }
        }

        [Theory, MemberData(nameof(ReadTestData))]
        public void LittleEndian(
            Type   type,
            object expectedValue,
            int    alignment,
            byte[] bigEndianData,
            byte[] littleEndianData,
            IEqualityComparer<object> returnComparer)
        {
            // ignore
            _ = alignment;
            _ = bigEndianData;

            // via Delegate
            {
                MessageReader reader = CreateMessageReader(EndianFlag.Little, littleEndianData);
                var method = s_createReadDelegateMethod.MakeGenericMethod(type);
                var readMethodDelegate = (Delegate)method.Invoke(null, null);
                var readValue = readMethodDelegate.DynamicInvoke(new object[] { reader });
                Assert.IsAssignableFrom(type, readValue);
                if (returnComparer != null)
                {
                    Assert.Equal(expectedValue, readValue, returnComparer);
                }
                else
                {
                    Assert.Equal(expectedValue, readValue);
                }
            }
            // via ReadMethod
            {
                if (type.GetTypeInfo().IsEnum)
                {
                    // TODO
                    return;
                }
                MessageReader reader = CreateMessageReader(EndianFlag.Little, littleEndianData);
                var ReadMethodInfo = ReadMethodFactory.CreateReadMethodForType(type);
                object readValue;
                if (ReadMethodInfo.IsStatic)
                {
                    readValue = ReadMethodInfo.Invoke(null, new object[] { reader });
                }
                else
                {
                    readValue = ReadMethodInfo.Invoke(reader, null);
                }
                Assert.IsAssignableFrom(type, readValue);
                if (returnComparer != null)
                {
                    Assert.Equal(expectedValue, readValue, returnComparer);
                }
                else
                {
                    Assert.Equal(expectedValue, readValue);
                }
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
            public override bool Equals(object rhs)
            {
                var other = rhs as PersonProperties;
                if (other == null)
                {
                    return false;
                }
                return (Name == other.Name) &&
                       (Age == other.Age) &&
                       (Gender == other.Gender);
            }
            public override int GetHashCode()
            {
                return Name?.GetHashCode() ?? 0;
            }
        }

        [Dictionary]
        public class DictionaryWithDash
        {
            public int f_d;

            public override bool Equals(object rhs)
            {
                var other = rhs as DictionaryWithDash;
                if (other == null)
                {
                    return false;
                }
                return (f_d == other.f_d);
            }

            public override int GetHashCode()
            {
                return f_d.GetHashCode();
            }
        }

        [Dictionary]
        public class PersonProperties2
        {
            public string Name;
            public string City;
            public int? PostalCode;

            // This field is added to check the reader does not throw when deserializing 'Name'
            // https://github.com/tmds/Tmds.DBus/issues/43
            public string PrefixName;

            public override bool Equals(object rhs)
            {
                var other = rhs as PersonProperties2;
                if (other == null)
                {
                    return false;
                }
                return (Name == other.Name) &&
                       (City == other.City) &&
                       (PostalCode == other.PostalCode);
            }
            public override int GetHashCode()
            {
                return Name?.GetHashCode() ?? 0;
            }
        }

        public static IEnumerable<object[]> ReadTestData
        {
            get
            {
                var myDictionary = new Dictionary<byte, string>
                {
                    { 1, "one" },
                    { 2, "two" }
                };
                var myArray = myDictionary.ToArray();
                var john = new PersonProperties()
                {
                    Name = "John",
                    Age = 32,
                    Gender = Gender.Male,
                    IsMarried = true
                };
                var john2 = new PersonProperties2()
                {
                    Name = john.Name,
                    City = null
                };
                return new []
                {
                    new object[] {typeof(MyEnum), MyEnum.Value,     2, new byte[] { 0x31, 0x45 },
                                                                    new byte[] { 0x45, 0x31 }, null},
                    new object[] {typeof(bool), true,               4, new byte[] { 0, 0, 0, 1 },
                                                                    new byte[] { 1, 0, 0, 0 }, null},
                    new object[] {typeof(byte), (byte)5,            1, new byte[] { 5 },
                                                                    new byte[] { 5 }, null},
                    new object[] {typeof(short), (short)0x0102,     2, new byte[] { 0x01, 0x02 },
                                                                    new byte[] { 0x02, 0x01 }, null},
                    new object[] {typeof(int), 0x01020304,          4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                                                                    new byte[] { 0x04, 0x03, 0x02, 0x01 }, null},
                    new object[] {typeof(long), 0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                                                    new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }, null},
                    new object[] {typeof(double), 1.0,              8, new byte[] { 0x3f, 0xf0, 0, 0, 0, 0, 0, 0 },
                                                                    new byte[] { 0, 0, 0, 0, 0, 0, 0xf0, 0x3f }, null},
                    new object[] {typeof(float), (float)1.0,        4, new byte[] { 0x3f, 0x80, 0, 0},
                                                                    new byte[] { 0, 0, 0x80, 0x3f }, null},
                    new object[] {typeof(ushort), (ushort)0x0102,   2, new byte[] { 0x01, 0x02 },
                                                                    new byte[] { 0x02, 0x01 }, null},
                    new object[] {typeof(uint), (uint)0x01020304,   4, new byte[] { 0x01, 0x02, 0x03, 0x04 },
                                                                    new byte[] { 0x04, 0x03, 0x02, 0x01 }, null},
                    new object[] {typeof(ulong), (ulong)0x0102030405060708, 8, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 },
                                                                            new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 }, null},
                    new object[] {typeof(string), "hw",             4, new byte[] { 0, 0, 0, 2, (byte)'h', (byte)'w', 0 },
                                                                    new byte[] { 2, 0, 0, 0, (byte)'h', (byte)'w', 0 }, null},
                    new object[] {typeof(ObjectPath), new ObjectPath("/a/b"), 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                                new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 }, null},
                    new object[] {typeof(Signature), new Signature("sis"), 1, new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 },
                                                                            new byte[] { 3, (byte)'s', (byte)'i', (byte)'s', 0 }, null},
                    new object[] {typeof(IDBusObject), new MyDBusObject(), 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                            new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                            new DBusObjectComparer()},
                    new object[] {typeof(IEnumerable<long>), new long[] { 1, 2}, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                                                                new byte[] { 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 }, null},
                    new object[] {typeof(ICollection<long>), new long[] { 1, 2}, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                                                                new byte[] { 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 }, null},
                    new object[] {typeof(IList<long>), new long[] { 1, 2}, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                                                            new byte[] { 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 }, null},
                    new object[] {typeof(long[]), new long[] { 1, 2}, 4, new byte[] { 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2 },
                                                                        new byte[] { 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 }, null},
                    new object[] {typeof(MyStruct), new MyStruct { v1 = 1, v2 = "hw" }, 8, new byte[] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}, null},
                    new object[] {typeof(MyClass), new MyClass { v1 = 1, v2 = "hw" }, 8, new byte[] {0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                        new byte[] {1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}, null},
                    new object[] {typeof(IEnumerable<KeyValuePair<byte, string>>), myArray, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                            new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}, null},
                    new object[] {typeof(IList<KeyValuePair<byte, string>>), myArray, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                        new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}, null},
                    new object[] {typeof(ICollection<KeyValuePair<byte, string>>), myArray, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                            new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}, null},
                    new object[] {typeof(IDictionary<byte, string>), myDictionary, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                    new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}, null},
                    new object[] {typeof(KeyValuePair<byte, string>[]), myArray, 4, new byte[] {0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                                new byte[] {28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}, null},
                    new object[] {typeof(IStringOperations), new MyDBusObject(StringOperations.Path), 4, new byte[] { 0, 0, 0, 4, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                                                        new byte[] { 4, 0, 0, 0, (byte)'/', (byte)'a', (byte)'/', (byte)'b', 0 },
                                                                                                        new DBusObjectComparer()},
                    new object[] {typeof(PersonProperties), john, 4, new byte[] {0, 0, 0, 88, 0, 0, 0, 0, 0, 0, 0, 4, 78, 97, 109, 101, 0, 1, 115, 0, 0, 0, 0, 4, 74, 111, 104, 110, 0, 0, 0, 0, 0, 0, 0, 3, 65, 103, 101, 0, 1, 105, 0, 0, 0, 0, 0, 32, 0, 0, 0, 6, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 1},
                                                                     new byte[] {88, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 78, 97, 109, 101, 0, 1, 115, 0, 4, 0, 0, 0, 74, 111, 104, 110, 0, 0, 0, 0, 3, 0, 0, 0, 65, 103, 101, 0, 1, 105, 0, 0, 32, 0, 0, 0, 6, 0, 0, 0, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 1, 0, 0, 0}, null},
                    new object[] {typeof(PersonProperties2), john2, 4, new byte[] {0, 0, 0, 88, 0, 0, 0, 0, 0, 0, 0, 4, 78, 97, 109, 101, 0, 1, 115, 0, 0, 0, 0, 4, 74, 111, 104, 110, 0, 0, 0, 0, 0, 0, 0, 3, 65, 103, 101, 0, 1, 105, 0, 0, 0, 0, 0, 32, 0, 0, 0, 6, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 0, 0, 0, 1},
                                                                       new byte[] {88, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 78, 97, 109, 101, 0, 1, 115, 0, 4, 0, 0, 0, 74, 111, 104, 110, 0, 0, 0, 0, 3, 0, 0, 0, 65, 103, 101, 0, 1, 105, 0, 0, 32, 0, 0, 0, 6, 0, 0, 0, 71, 101, 110, 100, 101, 114, 0, 1, 105, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0, 0, 73, 115, 77, 97, 114, 114, 105, 101, 100, 0, 1, 98, 0, 0, 0, 0, 1, 0, 0, 0}, null},
                    new object[] {typeof(DictionaryWithDash), new DictionaryWithDash { f_d = 5 }, 4, new byte[] {0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 3, 102, (byte)'-', 100, 0, 1, 105, 0, 0, 0, 0, 0, 5},
                                                                       new byte[] {16, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 102, (byte)'-', 100, 0, 1, 105, 0, 0, 5, 0, 0, 0}, null},
                    new object[] {typeof((byte, byte, byte, byte, byte, byte, byte, byte)), ((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8), 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8}, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8}, null},
                    new object[] {typeof(object), true,             4, new byte[] {1, 98, 0, 0, 0, 0, 0, 1},
                                                                    new byte[] {1, 98, 0, 0, 1, 0, 0, 0}, null},
                    new object[] {typeof(object), (byte)5,          1, new byte[] {1, 121, 0, 5},
                                                                    new byte[] {1, 121, 0, 5}, null},
                    new object[] {typeof(object), (short)0x0102,    2, new byte[] {1, 110, 0, 0, 1, 2},
                                                                    new byte[] {1, 110, 0, 0, 2, 1}, null},
                    new object[] {typeof(object), 0x01020304,       4, new byte[] {1, 105, 0, 0, 1, 2, 3, 4},
                                                                    new byte[] {1, 105, 0, 0, 4, 3, 2, 1}, null},
                    new object[] {typeof(object), 0x0102030405060708, 8, new byte[] {1, 120, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                        new byte[] {1, 120, 0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1}, null},
                    new object[] {typeof(object), 1.0,              8, new byte[] {1, 100, 0, 0, 0, 0, 0, 0, 63, 240, 0, 0, 0, 0, 0, 0},
                                                                    new byte[] {1, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 63}, null},
                    new object[] {typeof(object), (float)1.0,       4, new byte[] {1, 102, 0, 0, 63, 128, 0, 0},
                                                                    new byte[] {1, 102, 0, 0, 0, 0, 128, 63}, null},
                    new object[] {typeof(object), (ushort)0x0102,   2, new byte[] {1, 113, 0, 0, 1, 2},
                                                                    new byte[] {1, 113, 0, 0, 2, 1}, null},
                    new object[] {typeof(object), (uint)0x01020304, 4, new byte[] {1, 117, 0, 0, 1, 2, 3, 4},
                                                                    new byte[] {1, 117, 0, 0, 4, 3, 2, 1}, null},
                    new object[] {typeof(object), (ulong)0x0102030405060708, 8, new byte[] {1, 116, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                            new byte[] {1, 116, 0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1}, null},
                    new object[] {typeof(object), "hw",             4, new byte[] {1, 115, 0, 0, 0, 0, 0, 2, 104, 119, 0},
                                                                    new byte[] {1, 115, 0, 0, 2, 0, 0, 0, 104, 119, 0}, null},
                    new object[] {typeof(object), new ObjectPath("/a/b"), 4, new byte[] {1, 111, 0, 0, 0, 0, 0, 4, 47, 97, 47, 98, 0},
                                                                            new byte[] {1, 111, 0, 0, 4, 0, 0, 0, 47, 97, 47, 98, 0}, null},
                    new object[] {typeof(object), new Signature("sis"), 1, new byte[] {1, 103, 0, 3, 115, 105, 115, 0},
                                                                        new byte[] {1, 103, 0, 3, 115, 105, 115, 0}, null},
                    new object[] {typeof(object), new MyDBusObject(), 4, new byte[] {1, 111, 0, 0, 0, 0, 0, 4, 47, 97, 47, 98, 0},
                                                                        new byte[] {1, 111, 0, 0, 4, 0, 0, 0, 47, 97, 47, 98, 0},
                                                                        new ObjectPathComparer()},
                    new object[] {typeof(object), new long[] { 1, 2}, 4, new byte[] {2, 97, 120, 0, 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2},
                                                                        new byte[] {2, 97, 120, 0, 16, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0}, null},
                    new object[] {typeof(object), new ValueTuple<long, string> { Item1 = 1, Item2 = "hw" }, 8, new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 104, 119, 0},
                                                                                                            new byte[] {4, 40, 120, 115, 41, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 104, 119, 0}, null},
                    new object[] {typeof(object), myDictionary,     4, new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 0, 0, 0, 28, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 3, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 3, 116, 119, 111, 0},
                                                                    new byte[] {5, 97, 123, 121, 115, 125, 0, 0, 28, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 0, 0, 0, 111, 110, 101, 0, 0, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 116, 119, 111, 0}, null},
                    new object[] {typeof(object), ((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8), 8, new byte[] {10, 40, 121, 121, 121, 121, 121, 121, 121, 121, 41, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8},
                                                                                                            new byte[] {10, 40, 121, 121, 121, 121, 121, 121, 121, 121, 41, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8}, null},
                };
            }
        }
    }
}
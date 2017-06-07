using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class IntrospectionTests
    {
        [DBusInterface("org.freedesktop.DBus.Introspectable")]
        public interface IIntrospectable : IDBusObject
        {
            Task<string> IntrospectAsync();
        }

        [Dictionary]
        public class PersonProperties
        {
            public string Name;
            public int? Age;
            public bool IsMarried;
            public (int, int, int, int, int, int, int, int) Tuple;
        }

        [DBusInterface("tmds.dbus.tests.personproperties1")]
        interface IPersonProperties1 : IDBusObject
        {
            Task<PersonProperties> GetAllAsync();
            Task<(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)> GetTupleAsync();
        }

        class PropertyObject : IPersonProperties1
        {
            public static readonly ObjectPath Path = new ObjectPath("/person/object");
            public ObjectPath ObjectPath
            {
                get
                {
                    return Path;
                }
            }

            public Task<PersonProperties> GetAllAsync()
            {
                return Task.FromResult(new PersonProperties());
            }

            public Task<(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)> GetTupleAsync()
            {
                return Task.FromResult((0, 0, 0, 0, 0, 0, 0, 0));
            }
        }

        [Theory, MemberData(nameof(IntrospectData))]
        public static async Task Introspect(IDBusObject[] objects, ObjectPath path, string xml)
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            await conn2.RegisterObjectsAsync(objects);
            var introspectable = conn1.CreateProxy<IIntrospectable>("servicename", path);
            var reply = await introspectable.IntrospectAsync();
            Assert.Equal(xml, reply);
        }

        public static IEnumerable<object[]> IntrospectData
        {
            get
            {
                return new[]
                {
                    new object[] { new[] { new StringOperations() }, StringOperations.Path, s_stringOperationsIntrospection },
                    new object[] { new[] { new StringOperations("/tmds/dbus/tests/stringoperations"),
                                           new StringOperations("/tmds/dbus/tests/stringoperations/child1"),
                                           new StringOperations("/tmds/dbus/tests/stringoperations/child2")}, new ObjectPath("/tmds/dbus/tests/stringoperations"), s_parentWithChildrenIntrospection },
                    new object[] { new[] { new StringOperations("/tmds/dbus/tests/stringoperations"),
                                           new StringOperations("/tmds/dbus/tests/otherstringoperations")}, new ObjectPath("/tmds/dbus/tests"), s_emptyObjectWithChildNodesIntrospection },
                    new object[] { new[] { new PropertyObject() }, PropertyObject.Path, s_propertyObjectIntrospection }
                };
            }
        }

        private static string FormatUnixLineEndings(string value)
        {
            return value.Replace("\r\n", "\n");
        }

        private static string s_stringOperationsIntrospection = FormatUnixLineEndings(
@"<!DOCTYPE node PUBLIC ""-//freedesktop//DTD D-BUS Object Introspection 1.0//EN""
""http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd"">
<node name=""/tmds/dbus/tests/stringoperations"">
  <interface name=""tmds.dbus.tests.StringOperations"">
    <method name=""Concat"">
      <arg direction=""in"" name=""s1"" type=""s""/>
      <arg direction=""in"" name=""s2"" type=""s""/>
      <arg direction=""out"" name=""value"" type=""s""/>
    </method>
  </interface>
</node>
");

        private static string s_parentWithChildrenIntrospection = FormatUnixLineEndings(
@"<!DOCTYPE node PUBLIC ""-//freedesktop//DTD D-BUS Object Introspection 1.0//EN""
""http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd"">
<node name=""/tmds/dbus/tests/stringoperations"">
  <interface name=""tmds.dbus.tests.StringOperations"">
    <method name=""Concat"">
      <arg direction=""in"" name=""s1"" type=""s""/>
      <arg direction=""in"" name=""s2"" type=""s""/>
      <arg direction=""out"" name=""value"" type=""s""/>
    </method>
  </interface>
  <node name=""child1""/>
  <node name=""child2""/>
</node>
");

        private static string s_emptyObjectWithChildNodesIntrospection = FormatUnixLineEndings(
@"<!DOCTYPE node PUBLIC ""-//freedesktop//DTD D-BUS Object Introspection 1.0//EN""
""http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd"">
<node name=""/tmds/dbus/tests"">
  <node name=""stringoperations""/>
  <node name=""otherstringoperations""/>
</node>
");

        private static string s_propertyObjectIntrospection = FormatUnixLineEndings(
@"<!DOCTYPE node PUBLIC ""-//freedesktop//DTD D-BUS Object Introspection 1.0//EN""
""http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd"">
<node name=""/person/object"">
  <interface name=""tmds.dbus.tests.personproperties1"">
    <method name=""GetTuple"">
      <arg direction=""out"" name=""i1"" type=""i""/>
      <arg direction=""out"" name=""i2"" type=""i""/>
      <arg direction=""out"" name=""i3"" type=""i""/>
      <arg direction=""out"" name=""i4"" type=""i""/>
      <arg direction=""out"" name=""i5"" type=""i""/>
      <arg direction=""out"" name=""i6"" type=""i""/>
      <arg direction=""out"" name=""i7"" type=""i""/>
      <arg direction=""out"" name=""i8"" type=""i""/>
    </method>
    <property name=""Name"" type=""s"" access=""readwrite""/>
    <property name=""Age"" type=""i"" access=""readwrite""/>
    <property name=""IsMarried"" type=""b"" access=""readwrite""/>
    <property name=""Tuple"" type=""(iiiiiiii)"" access=""readwrite""/>
  </interface>
</node>
");

    }
}

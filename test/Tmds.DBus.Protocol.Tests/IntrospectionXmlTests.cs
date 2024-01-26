using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class IntrospectionXmlTests
{
    [Theory, MemberData(nameof(IntrospectionXmlTestData))]
    public void Interfaces(ReadOnlyMemory<byte> propertyValue, string expected)
    {
        // Validate the expected XML.
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(expected);
        Assert.EndsWith("\n", expected);

        Assert.Equal(expected, Encoding.UTF8.GetString(propertyValue.Span));
    }

    public static IEnumerable<object[]> IntrospectionXmlTestData
    {
        get
        {
            yield return new object[]
            {
                IntrospectionXml.DBusProperties,
                """
                <interface name="org.freedesktop.DBus.Properties">
                  <method name="Get">
                    <arg direction="in" type="s"/>
                    <arg direction="in" type="s"/>
                    <arg direction="out" type="v"/>
                  </method>
                  <method name="GetAll">
                    <arg direction="in" type="s"/>
                    <arg direction="out" type="a{sv}"/>
                  </method>
                  <method name="Set">
                    <arg direction="in" type="s"/>
                    <arg direction="in" type="s"/>
                    <arg direction="in" type="v"/>
                  </method>
                  <signal name="PropertiesChanged">
                    <arg type="s" name="interface_name"/>
                    <arg type="a{sv}" name="changed_properties"/>
                    <arg type="as" name="invalidated_properties"/>
                  </signal>
                </interface>

                """
            };

            yield return new object[]
            {
                IntrospectionXml.DBusIntrospectable,
                """
                <interface name="org.freedesktop.DBus.Introspectable">
                  <method name="Introspect">
                    <arg type="s" name="xml_data" direction="out"/>
                  </method>
                </interface>

                """
            };

            yield return new object[]
            {
                IntrospectionXml.DBusPeer,
                """
                <interface name="org.freedesktop.DBus.Peer">
                  <method name="Ping"/>
                  <method name="GetMachineId">
                    <arg type="s" name="machine_uuid" direction="out"/>
                  </method>
                </interface>

                """
            };
        }
    }
}
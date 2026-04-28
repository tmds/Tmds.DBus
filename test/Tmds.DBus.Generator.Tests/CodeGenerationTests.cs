using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Generator.Tests;

public class CodeGenerationTests : TestsBase
{
    private const string XmlContent = """
        <?xml version="1.0" encoding="UTF-8"?>
        <node>
          <interface name="org.example.Calculator">
            <method name="Clear"/>
            <method name="GetResult">
              <arg name="result" direction="out" type="d"/>
            </method>
            <method name="GetStats">
              <arg name="count" direction="out" type="u"/>
              <arg name="total" direction="out" type="d"/>
            </method>
            <method name="Store">
              <arg name="value" direction="in" type="d"/>
            </method>
            <method name="Square">
              <arg name="input" direction="in" type="d"/>
              <arg name="result" direction="out" type="d"/>
            </method>
            <method name="DivMod">
              <arg name="dividend" direction="in" type="i"/>
              <arg name="quotient" direction="out" type="i"/>
              <arg name="remainder" direction="out" type="i"/>
            </method>
            <method name="LogOperation">
              <arg name="operation" direction="in" type="s"/>
              <arg name="value" direction="in" type="d"/>
            </method>
            <method name="Add">
              <arg name="a" direction="in" type="d"/>
              <arg name="b" direction="in" type="d"/>
              <arg name="result" direction="out" type="d"/>
            </method>
            <method name="Compare">
              <arg name="a" direction="in" type="d"/>
              <arg name="b" direction="in" type="d"/>
              <arg name="equal" direction="out" type="b"/>
              <arg name="difference" direction="out" type="d"/>
            </method>
            <method name="GetEntry">
              <arg name="key" direction="in" type="s"/>
              <arg name="entry" direction="out" type="(sd)"/>
            </method>
            <method name="SetEntry">
              <arg name="entry" direction="in" type="(sd)"/>
            </method>
            <signal name="Reset"/>
            <signal name="Error">
              <arg name="message" type="s"/>
            </signal>
            <signal name="OperationComplete">
              <arg name="operation" type="s"/>
              <arg name="result" type="d"/>
            </signal>
            <property name="LastResult" type="d" access="read"/>
            <property name="CurrentPath" type="o" access="read"/>
            <property name="CurrentEntry" type="(sd)" access="read"/>
          </interface>
          <interface name="org.example.Settings">
            <method name="Save"/>
            <signal name="Changed"/>
            <property name="Theme" type="s" access="read"/>
            <property name="Volume" type="d" access="write"/>
            <property name="Language" type="s" access="readwrite"/>
          </interface>
          <interface name="org.example.Marker">
          </interface>
        </node>
        """;

    [Fact]
    public async Task VerifyGeneratedOutput_ProxyOnly()
    {
        var additionalFiles = new List<AdditionalFile>
        {
            new("interfaces.xml", XmlContent, "TestNamespace", DBusGeneratorMode: "Proxy")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.Empty(diagnostics);

        await Verify(string.Join("\n", generatedSources.Select(s => s.SourceText.ToString())));

        AssertCompiles(generatedSources);
    }

    [Fact]
    public async Task VerifyGeneratedOutput_HandlerOnly()
    {
        var additionalFiles = new List<AdditionalFile>
        {
            new("interfaces.xml", XmlContent, "TestNamespace", DBusGeneratorMode: "Handler")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.Empty(diagnostics);

        await Verify(string.Join("\n", generatedSources.Select(s => s.SourceText.ToString())));

        AssertCompiles(generatedSources);
    }

    [Fact]
    public async Task VerifyGeneratedOutput_ProxyAndHandlerSameNamespace()
    {
        string proxyXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <node>
              <interface name="org.example.Calculator">
                <method name="Add">
                  <arg name="a" direction="in" type="d"/>
                  <arg name="b" direction="in" type="d"/>
                  <arg name="result" direction="out" type="d"/>
                </method>
              </interface>
            </node>
            """;

        string handlerXml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <node>
              <interface name="org.example.Settings">
                <method name="Save"/>
                <property name="Theme" type="s" access="read"/>
              </interface>
            </node>
            """;

        var additionalFiles = new List<AdditionalFile>
        {
            new("proxy.xml", proxyXml, "TestNamespace", DBusGeneratorMode: "Proxy"),
            new("handler.xml", handlerXml, "TestNamespace", DBusGeneratorMode: "Handler")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.Empty(diagnostics);

        await Verify(string.Join("\n", generatedSources.Select(s => s.SourceText.ToString())));

        AssertCompiles(generatedSources);
    }

    [Fact]
    public async Task VerifyGeneratedOutput_ProxyAndHandlerDifferentNameSpace()
    {
        var additionalFiles = new List<AdditionalFile>
        {
            new("proxy.xml", XmlContent, "ProxyNamespace", DBusGeneratorMode: "Proxy"),
            new("handler.xml", XmlContent, "HandlerNamespace", DBusGeneratorMode: "Handler")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.Empty(diagnostics);

        await Verify(string.Join("\n", generatedSources.Select(s => s.SourceText.ToString())));

        AssertCompiles(generatedSources);
    }

}

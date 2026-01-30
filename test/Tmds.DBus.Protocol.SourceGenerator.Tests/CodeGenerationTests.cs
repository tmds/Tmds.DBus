using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.SourceGenerator.Tests;

public class CodeGenerationTests : TestsBase
{
    [Fact]
    public async Task VerifyGeneratedOutput()
    {
        string xmlContent = """
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
                <signal name="Reset"/>
                <signal name="Error">
                  <arg name="message" type="s"/>
                </signal>
                <signal name="OperationComplete">
                  <arg name="operation" type="s"/>
                  <arg name="result" type="d"/>
                </signal>
                <property name="LastResult" type="d" access="read"/>
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

        var additionalFiles = new List<AdditionalFile>
        {
            new("interfaces.xml", xmlContent, "TestNamespace")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.Empty(diagnostics);
        Assert.Single(generatedSources);

        await Verify(generatedSources[0].SourceText.ToString());

        AssertCompiles(generatedSources);
    }
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Tmds.DBus.Generator.Tests;

public class DiagnosticsTests : TestsBase
{
    [Fact]
    public void GenerateCode_WithMissingNamespace_ReportsDiagnostic()
    {
        string xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <node>
              <interface name="org.freedesktop.DBus.Test">
                <method name="DoSomething">
                  <arg name="value" direction="in" type="s"/>
                </method>
              </interface>
            </node>
            """;

        var additionalFiles = new List<AdditionalFile>
        {
            new("test.xml", xmlContent, Namespace: null, DBusGeneratorMode: "Proxy")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.NotEmpty(diagnostics);
        var diagnostic = diagnostics.First();
        Assert.Equal("DBUS1001", diagnostic.Id);
        Assert.Contains("missing the required 'Namespace' metadata", diagnostic.GetMessage());
    }

    [Fact]
    public void GenerateCode_WithInvalidXml_ReportsDiagnostic()
    {
        string xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <node>
              <interface name="org.freedesktop.DBus.Test">
                <method name="DoSomething">
            """; // Intentionally invalid XML

        var additionalFiles = new List<AdditionalFile>
        {
            new("test.xml", xmlContent, "TestNamespace", DBusGeneratorMode: "Proxy")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.NotEmpty(diagnostics);
        var diagnostic = diagnostics.First();
        Assert.Equal("DBUS1004", diagnostic.Id);
    }

    [Fact]
    public void GenerateCode_WithInvalidRootElement_ReportsDiagnostic()
    {
        string xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <invalid>
              <interface name="org.freedesktop.DBus.Test">
                <method name="DoSomething">
                  <arg name="value" direction="in" type="s"/>
                </method>
              </interface>
            </invalid>
            """;

        var additionalFiles = new List<AdditionalFile>
        {
            new("test.xml", xmlContent, "TestNamespace", DBusGeneratorMode: "Proxy")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.NotEmpty(diagnostics);
        var diagnostic = diagnostics.First();
        Assert.Equal("DBUS1006", diagnostic.Id);
        Assert.Contains("must have a <node> element", diagnostic.GetMessage());
    }

    [Fact]
    public void GenerateCode_WithMissingGeneratorMode_ReportsWarning()
    {
        string xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <node>
              <interface name="org.freedesktop.DBus.Test">
                <method name="DoSomething">
                  <arg name="value" direction="in" type="s"/>
                </method>
              </interface>
            </node>
            """;

        var additionalFiles = new List<AdditionalFile>
        {
            new("test.xml", xmlContent, "TestNamespace")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("DBUS1011", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.NotEmpty(generatedSources);
    }

    [Fact]
    public void GenerateCode_WithInvalidGeneratorMode_ReportsDiagnostic()
    {
        string xmlContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <node>
              <interface name="org.freedesktop.DBus.Test">
                <method name="DoSomething">
                  <arg name="value" direction="in" type="s"/>
                </method>
              </interface>
            </node>
            """;

        var additionalFiles = new List<AdditionalFile>
        {
            new("test.xml", xmlContent, "TestNamespace", DBusGeneratorMode: "Invalid")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("DBUS1012", diagnostic.Id);
        Assert.Contains("Invalid", diagnostic.GetMessage());
        Assert.Empty(generatedSources);
    }
}

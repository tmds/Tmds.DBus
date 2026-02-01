using System.Collections.Generic;
using System.Linq;
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
            new("test.xml", xmlContent, Namespace: null)
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
            new("test.xml", xmlContent, "TestNamespace")
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
            new("test.xml", xmlContent, "TestNamespace")
        };

        var (diagnostics, generatedSources) = RunGenerator(additionalFiles);

        Assert.NotEmpty(diagnostics);
        var diagnostic = diagnostics.First();
        Assert.Equal("DBUS1006", diagnostic.Id);
        Assert.Contains("must have a <node> element", diagnostic.GetMessage());
    }
}

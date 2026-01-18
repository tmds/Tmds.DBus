using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Tmds.DBus.Protocol.SourceGenerator
{
    internal enum DBusGeneratorMode
    {
        Proxy,
        Handler
    }

    [Generator]
    public class DBusSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find AdditionalFiles xml files that have a DBusGeneratorMode.
            IncrementalValuesProvider<AdditionalFile> additionalFiles =
                context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, ct) =>
                {
                    var (file, options) = pair;
                    var fileOptions = options.GetOptions(file);
                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.DBusGeneratorMode", out string? generatorMode);
                    if (string.IsNullOrEmpty(generatorMode))
                    {
                        return new AdditionalFile?();
                    }

                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.Namespace", out string? ns);
                    return new AdditionalFile(file, ns ?? string.Empty, generatorMode ?? string.Empty);
                })
                .Where(static file => file.HasValue)
                .Select(static (file, ct) => file!.Value);
            IncrementalValueProvider<ImmutableArray<AdditionalFile>> collectedFiles = additionalFiles.Collect();

            context.RegisterSourceOutput(collectedFiles, GenerateSource);
        }

        private static void GenerateSource(SourceProductionContext spc, ImmutableArray<AdditionalFile> files)
        {
            var xmlFiles = new List<ParsedXmlFile>();

            foreach (AdditionalFile additionalFile in files)
            {
                ParsedXmlFile? parsed = ParseXmlFile(additionalFile, spc, CancellationToken.None);

                if (parsed != null)
                {
                    xmlFiles.Add(parsed);
                }
            }

            // Group by namespace and generate code
            foreach (var group in xmlFiles.GroupBy(x => x.Namespace))
            {
                string ns = group.Key;
                IEnumerable<Tool.InterfaceDescription> interfaces = group.SelectMany(x => x.Interfaces);

                // Check for duplicate interface names within the namespace
                var duplicateInterfaces = interfaces
                    .GroupBy(i => i.InterfaceName)
                    .Where(g => g.Count() > 1)
                    .ToList();
                if (duplicateInterfaces.Any())
                {
                    foreach (var duplicate in duplicateInterfaces)
                    {
                        string sourceFiles = string.Join(", ", duplicate.Select(i => i.SourceFile ?? "unknown").Distinct());
                        spc.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateInterface,
                            Location.None,
                            duplicate.Key,
                            ns,
                            sourceFiles));
                    }
                    continue; // Skip code generation for this namespace
                }

                // Check for duplicate class names within the namespace
                var duplicateClassNames = interfaces
                    .GroupBy(i => i.Name)
                    .Where(g => g.Count() > 1)
                    .ToList();
                if (duplicateClassNames.Any())
                {
                    foreach (var duplicate in duplicateClassNames)
                    {
                        string interfaceNames = string.Join(", ", duplicate.Select(i => i.InterfaceName).Distinct());
                        string sourceFiles = string.Join(", ", duplicate.Select(i => i.SourceFile ?? "unknown").Distinct());
                        spc.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateClassName,
                            Location.None,
                            duplicate.Key,
                            ns,
                            interfaceNames,
                            sourceFiles));
                    }
                    continue; // Skip code generation for this namespace
                }

                try
                {
                    var settings = new Tool.ProtocolGeneratorSettings
                    {
                        Namespace = ns,
                        GeneratorDescription = "Tmds.DBus.Protocol.SourceGenerator"
                    };
                    var generator = new Tool.ProtocolGenerator(settings);

                    string sourceCode = generator.Generate(interfaces);

                    string hintName = $"{ns}.g.cs";
                    spc.AddSource(hintName, SourceText.From(sourceCode, Encoding.UTF8));
                }
                catch (Tool.InterfaceGenerationException ex)
                {
                    var interfaceDesc = ex.InterfaceDescription;
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.CodeGenerationFailed,
                        Location.None,
                        interfaceDesc.InterfaceName,
                        interfaceDesc.Name,
                        ns,
                        interfaceDesc.SourceFile ?? "unknown",
                        ex.InnerException?.ToString() ?? ex.Message));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.CodeGenerationFailedGeneric,
                        Location.None,
                        ns,
                        ex.ToString()));
                }
            }
        }

        private static ParsedXmlFile? ParseXmlFile(AdditionalFile additionalFile, SourceProductionContext spc, CancellationToken ct)
        {
            string fileName = System.IO.Path.GetFileName(additionalFile.Text.Path);

            // Check for required namespace
            if (string.IsNullOrWhiteSpace(additionalFile.Namespace))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MissingNamespace,
                    Location.None,
                    fileName));
                return null;
            }

            // Validate generator mode - only Proxy is supported
            if (!additionalFile.GeneratorMode.Equals("Proxy", StringComparison.OrdinalIgnoreCase))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedGeneratorMode,
                    Location.None,
                    fileName,
                    additionalFile.GeneratorMode));
                return null;
            }

            SourceText? text = additionalFile.Text.GetText(ct);
            if (text == null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnableToReadFile,
                    Location.None,
                    fileName));
                return null;
            }

            XDocument doc;
            try
            {

                doc = XDocument.Parse(text.ToString());
            }
            catch (Exception ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidXml,
                    Location.None,
                    fileName,
                    ex.Message));
                return null;
            }

            // Validate root element is "node"
            if (doc.Root?.Name.LocalName != "node")
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidRootElement,
                    Location.None,
                    fileName,
                    doc.Root?.Name.LocalName ?? "null"));
                return null;
            }

            // Extract interfaces
            var interfaces = new List<Tool.InterfaceDescription>();
            foreach (var interfaceElement in doc.Descendants("interface"))
            {
                string interfaceName = interfaceElement.Attribute("name")?.Value ?? string.Empty;

                if (string.IsNullOrEmpty(interfaceName))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MissingInterfaceName,
                        Location.None,
                        fileName));
                    continue;
                }

                string className = interfaceName.Split('.').Last();
                interfaces.Add(new Tool.InterfaceDescription
                {
                    InterfaceXml = interfaceElement,
                    Name = className,
                    SourceFile = additionalFile.Text.Path
                });
            }

            return new ParsedXmlFile(additionalFile.Namespace, fileName, interfaces);
        }

        private class ParsedXmlFile
        {
            public ParsedXmlFile(string ns, string fileName, List<Tool.InterfaceDescription> interfaces)
            {
                Namespace = ns;
                FileName = fileName;
                Interfaces = interfaces;
            }

            public string Namespace { get; }
            public string FileName { get; }
            public List<Tool.InterfaceDescription> Interfaces { get; }
        }

        private readonly record struct AdditionalFile(AdditionalText Text, string Namespace, string GeneratorMode);
    }
}

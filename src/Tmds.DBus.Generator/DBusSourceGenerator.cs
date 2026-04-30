using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Tmds.DBus.Generator
{
    [Generator]
    public class DBusSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<AdditionalFile> additionalFiles =
                context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select(static (pair, ct) =>
                {
                    var (file, options) = pair;
                    var fileOptions = options.GetOptions(file);
                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.GenerateDBusTypes", out string? generateDBusTypes);

                    bool enabled = string.IsNullOrEmpty(generateDBusTypes) ||
                                   string.Equals(generateDBusTypes, "true", StringComparison.OrdinalIgnoreCase);
                    if (!enabled)
                    {
                        return new AdditionalFile?();
                    }

                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.DBusGeneratorMode", out string? generatorMode);
                    fileOptions.TryGetValue("build_metadata.AdditionalFiles.Namespace", out string? ns);
                    return new AdditionalFile(file, ns, generatorMode);
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
                string? generatorMode = additionalFile.GeneratorMode;
                bool generateProxy, generateHandler;
                if (string.IsNullOrEmpty(generatorMode))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MissingGeneratorMode,
                        Location.None,
                        System.IO.Path.GetFileName(additionalFile.Text.Path)));
                    generateProxy = true;
                    generateHandler = false;
                }
                else if (string.Equals(generatorMode, "Proxy", StringComparison.OrdinalIgnoreCase))
                {
                    generateProxy = true;
                    generateHandler = false;
                }
                else if (string.Equals(generatorMode, "Handler", StringComparison.OrdinalIgnoreCase))
                {
                    generateProxy = false;
                    generateHandler = true;
                }
                else
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidGeneratorMode,
                        Location.None,
                        System.IO.Path.GetFileName(additionalFile.Text.Path),
                        generatorMode));
                    continue;
                }

                ParsedXmlFile? parsed = ParseXmlFile(additionalFile, generateProxy, generateHandler, spc, CancellationToken.None);

                if (parsed != null)
                {
                    xmlFiles.Add(parsed);
                }
            }

            var settings = new Tool.ProtocolGeneratorSettings
            {
                GeneratorDescription = GetGeneratorDescription()
            };
            var generator = new Tool.ProtocolGenerator(settings);
            bool generateSharedCode = false;
            // Group by namespace and generate code
            foreach (var group in xmlFiles.GroupBy(x => x.Namespace))
            {
                string ns = group.Key;

                // Group by interface name: merge complementary proxy+handler entries, flag true duplicates.
                var interfaces = new List<Tool.InterfaceDescription>();
                bool hasDuplicates = false;
                foreach (var interfaceGroup in group.SelectMany(x => x.Interfaces).GroupBy(i => i.InterfaceName))
                {
                    Tool.InterfaceDescription? merged = null;
                    foreach (var entry in interfaceGroup)
                    {
                        if (merged is null)
                        {
                            merged = entry;
                        }
                        else if (!merged.GenerateProxy && entry.GenerateProxy)
                        {
                            merged.GenerateProxy = true;
                        }
                        else if (!merged.GenerateHandler && entry.GenerateHandler)
                        {
                            merged.GenerateHandler = true;
                        }
                        else
                        {
                            string sourceFiles = string.Join(", ", interfaceGroup.Select(i => i.SourceFile ?? "unknown").Distinct());
                            spc.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.DuplicateInterface,
                                Location.None,
                                interfaceGroup.Key,
                                ns,
                                sourceFiles));
                            hasDuplicates = true;
                            break;
                        }
                    }
                    if (merged is not null)
                    {
                        interfaces.Add(merged);
                    }
                }
                if (hasDuplicates)
                {
                    continue;  // Skip code generation for this namespace
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
                    string sourceCode = generator.Generate(ns, interfaces);

                    string hintName = $"{ns}.g.cs";
                    spc.AddSource(hintName, SourceText.From(sourceCode, Encoding.UTF8));
                    generateSharedCode = true;
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

            // Generate shared code
            if (generateSharedCode)
            {
                try
                {
                    string sharedCode = generator.GenerateShared();
                    spc.AddSource("DBus.SourceGenerator.Common.g.cs", SourceText.From(sharedCode, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.CodeGenerationFailedGeneric,
                        Location.None,
                        "Shared",
                        ex.ToString()));
                }
            }
        }

        private static ParsedXmlFile? ParseXmlFile(AdditionalFile additionalFile, bool generateProxy, bool generateHandler, SourceProductionContext spc, CancellationToken ct)
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
                    SourceFile = additionalFile.Text.Path,
                    GenerateProxy = generateProxy,
                    GenerateHandler = generateHandler
                });
            }

            return new ParsedXmlFile(additionalFile.Namespace, fileName, interfaces);
        }

        private static string GetGeneratorDescription()
        {
            var assembly = typeof(DBusSourceGenerator).Assembly;

            string generatorVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
            // strip the metadata (commit sha)
            int plusIndex = generatorVersion.IndexOf('+');
            if (plusIndex >= 0)
            {
                generatorVersion = generatorVersion.Substring(0, plusIndex);
            }

            return $"Tmds.DBus.Generator v{generatorVersion}";
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

        private readonly record struct AdditionalFile(AdditionalText Text, string? Namespace, string? GeneratorMode);
    }
}

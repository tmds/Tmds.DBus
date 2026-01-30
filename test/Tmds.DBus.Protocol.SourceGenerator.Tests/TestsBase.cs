using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Tmds.DBus.Protocol.SourceGenerator;
using VerifyXunit;
using Xunit;

namespace Tmds.DBus.Protocol.SourceGenerator.Tests;

public record AdditionalFile(string FileName, string Content, string? Namespace = null);

public record GeneratedSourceResult(SyntaxTree Tree, SourceText SourceText, string HintName);

public abstract class TestsBase : VerifyBase
{
    protected TestsBase() : base()
    {
    }

    protected void AssertCompiles(ImmutableArray<GeneratedSourceResult> generatedSources)
    {
        // This verifies if the code compiles when targeting netstandard2.0/C#11 with a reference to 'PolySharp'.

        // Create a temporary directory for a .NET project.
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dbustest_{System.Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            // Write all generated source files
            int fileIndex = 0;
            foreach (var generatedSource in generatedSources)
            {
                var fileName = $"Generated{fileIndex++}.cs";
                var filePath = System.IO.Path.Combine(tempDir, fileName);
                System.IO.File.WriteAllText(filePath, generatedSource.SourceText.ToString());
            }

            // Find the repository root and locate Tmds.DBus.Protocol project
            var repoRoot = FindRepositoryRoot();
            var protocolProjectPath = System.IO.Path.Combine(repoRoot, "src", "Tmds.DBus.Protocol", "Tmds.DBus.Protocol.csproj");

            // Create a .csproj file
            var csprojContent = $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
                    <LangVersion>11</LangVersion>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="PolySharp" Version="*" PrivateAssets="all" />
                  </ItemGroup>

                  <ItemGroup>
                    <ProjectReference Include="{{protocolProjectPath}}" />
                  </ItemGroup>
                </Project>
                """;

            var csprojPath = System.IO.Path.Combine(tempDir, "GeneratedCodeTest.csproj");
            System.IO.File.WriteAllText(csprojPath, csprojContent);

            // Build the project using dotnet build
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build --configuration Release",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            Assert.NotNull(process);

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var errorMessage = $"Generated code failed to build.\n\nExit code: {process.ExitCode}\n\nOutput:\n{output}\n\nError:\n{error}\n\nProject directory: {tempDir}";
                Assert.Fail(errorMessage);
            }
        }
        finally
        {
            try
            {
                System.IO.Directory.Delete(tempDir, true);
            }
            catch
            { }
        }
    }

    private static string FindRepositoryRoot()
    {
        var currentAssembly = typeof(TestsBase).Assembly;
        var directory = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(currentAssembly.Location)!);

        // Walk up the directory tree looking for .git dir.
        while (directory != null)
        {
            if (System.IO.Directory.Exists(System.IO.Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new System.InvalidOperationException("Could not find repository root (no .git or solution file found)");
    }

    protected (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<GeneratedSourceResult> GeneratedSources) RunGenerator(
        List<AdditionalFile> additionalFiles)
    {
        // Create a compilation with an empty source file
        var sourceCode = "// Empty source file";
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create additional texts from the provided files
        var additionalTexts = additionalFiles.Select(f => new InMemoryAdditionalText(f.FileName, f.Content)).ToArray();

        // Create analyzer config options provider
        var optionsProvider = new InMemoryAnalyzerConfigOptionsProvider(additionalFiles);

        // Create and run the generator
        var generator = new DBusSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();

        return (runResult.Diagnostics, runResult.GeneratedTrees.Select(t =>
            new GeneratedSourceResult(t, SourceText.From(t.ToString(), System.Text.Encoding.UTF8), "test")).ToImmutableArray());
    }

    // Helper classes for testing
    private class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, System.Text.Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
            => _text;
    }

    private class InMemoryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly Dictionary<string, string?> _fileOptions;
        private readonly InMemoryAnalyzerConfigOptions _globalOptions;

        public InMemoryAnalyzerConfigOptionsProvider(List<AdditionalFile> additionalFiles)
        {
            _fileOptions = additionalFiles.ToDictionary(f => f.FileName, f => f.Namespace);
            _globalOptions = new InMemoryAnalyzerConfigOptions(null);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            var fileName = System.IO.Path.GetFileName(textFile.Path);
            _fileOptions.TryGetValue(fileName, out var ns);
            return new InMemoryAnalyzerConfigOptions(ns);
        }

        private class InMemoryAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly string? _namespace;

            public InMemoryAnalyzerConfigOptions(string? ns)
            {
                _namespace = ns;
            }

            public override bool TryGetValue(string key, out string? value)
            {
                if (key == "build_metadata.AdditionalFiles.GenerateDBusTypes")
                {
                    value = "true";
                    return true;
                }
                if (key == "build_metadata.AdditionalFiles.Namespace" && _namespace != null)
                {
                    value = _namespace;
                    return true;
                }
                value = null;
                return false;
            }
        }
    }
}

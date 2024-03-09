using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Tmds.DBus.Tool.Diagnostics;

namespace Tmds.DBus.Tool.Tests;

public class GeneratorTests
{
    private const string NameOfFaultyInterface = "org.bluez.SimAccessTest1";

    [Fact]
    public void TestCodeGeneratorForHighLevelApi_WithSuccess()
    {
        //Prepare generator
        var generator =
            new Generator(
                new GeneratorSettings
                {
                    Namespace = "bluez.DBus",
                    NoInternalsVisibleTo = false,
                    TypesAccessModifier = Accessibility.NotApplicable
                });

        var faultyDescriptions = GetInterfaceDescriptions(includeMalformedDefinition: false);

        string sourceCode;
        using (new ConsoleSuppressor())
        {
            sourceCode = generator.Generate(faultyDescriptions);
        }

        Assert.False(string.IsNullOrWhiteSpace(sourceCode));
    }

    [Fact]
    public void TestCodeGeneratorForHighLevelApi_WithFault()
    {
        //Prepare generator
        var generator =
            new Generator(
                new GeneratorSettings
                {
                    Namespace = "bluez.DBus",
                    NoInternalsVisibleTo = false,
                    TypesAccessModifier = Accessibility.NotApplicable
                });

        var faultyDescriptions = GetInterfaceDescriptions(includeMalformedDefinition: true);

        GenerationException exception = Assert.Throws<GenerationException>(() =>
        {
            using var suppressor = new ConsoleSuppressor();
            var sourceCode = generator.Generate(faultyDescriptions);
        });
        var attr = Assert.IsType<XAttribute>(exception.FaultyElement);
        Assert.Equal(NameOfFaultyInterface, exception.InterfaceDescription?.FullName);
        Assert.Equal("type", attr.Name.LocalName);
        Assert.Equal("", attr.Value);
    }


    [Fact]
    public void TestCodeGeneratorForProtocolApi_WithSuccess()
    {
        //Prepare generator
        var generator = (IGenerator)new ProtocolGenerator(
            new ProtocolGeneratorSettings
            {
                Namespace = "bluez.DBus",
                TypesAccessModifier = Accessibility.NotApplicable,
                ServiceName = "bluez"
            });

        var faultyDescriptions = GetInterfaceDescriptions(includeMalformedDefinition: false);

        string sourceCode;
        using (new ConsoleSuppressor())
        {
            sourceCode = generator.Generate(faultyDescriptions);
        }

        Assert.False(string.IsNullOrWhiteSpace(sourceCode));
    }

    [Fact]
    public void TestCodeGeneratorForProtocolApi_WithFault()
    {
        //Prepare generator
        var generator = (IGenerator)new ProtocolGenerator(
            new ProtocolGeneratorSettings
            {
                Namespace = "bluez.DBus",
                TypesAccessModifier = Accessibility.NotApplicable,
                ServiceName = "bluez"
            });

        var faultyDescriptions = GetInterfaceDescriptions(includeMalformedDefinition: true);

        GenerationException exception = Assert.Throws<GenerationException>(() =>
        {
            using var suppressor = new ConsoleSuppressor();
            var sourceCode = generator.Generate(faultyDescriptions);
        });
        var attr = Assert.IsType<XAttribute>(exception.FaultyElement);
        Assert.Equal(NameOfFaultyInterface, exception.InterfaceDescription?.FullName);
        Assert.Equal("type", attr.Name.LocalName);
        Assert.Equal("", attr.Value);
    }

    private List<InterfaceDescription> GetInterfaceDescriptions(bool includeMalformedDefinition)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNamePrefix = assembly.GetName().Name + ".";
        var descriptions = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourceNamePrefix))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name) ?? throw new NullReferenceException();
                var doc = XDocument.Load(stream);
                var path = name.Substring(resourceNamePrefix.Length).Replace('_', '/');
                stream.Position = 0;
                var encoding = Encoding.GetEncoding(doc.Declaration?.Encoding ?? Encoding.UTF8.WebName);
                using var reader = new StreamReader(stream, encoding);
                var fullXml = reader.ReadToEnd();
                return new
                {
                    Path = path,
                    FullXml = fullXml,
                    Doc = doc
                };
            })
            .SelectMany(item =>
                item.Doc.Element("node")?.Elements("interface")?.Select(interfaceElement => new InterfaceDescription
                {
                    Path = item.Path,
                    FullXml = item.FullXml,
                    FullName = interfaceElement.Attribute("name")?.Value ?? throw new NullReferenceException(),
                    InterfaceXml = interfaceElement,
                    Name = "ProposedNamesAreNotSetInUnitTests" //Proposed names are not set in unit tests
                })
                ?? throw new NullReferenceException())
            .GroupBy(desc => desc.FullName)
            .Select(gr => gr.First())
            .Where(desc => includeMalformedDefinition || desc.FullName != NameOfFaultyInterface)
            .ToList();

        return descriptions;
    }
}
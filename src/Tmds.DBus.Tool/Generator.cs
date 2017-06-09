using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tmds.DBus.Tool
{
    class GeneratorSettings
    {
        public string Namespace { get; set; } = "DBus";
    }

    class Generator
    {
        private readonly AdhocWorkspace _workspace;
        private readonly SyntaxGenerator _generator;
        private readonly SyntaxNode _iDBusObject;
        private readonly string _dBusInterfaceAttribute;
        private readonly SyntaxNode _dictionaryAttribute;
        private readonly SyntaxNode _task;
        private readonly SyntaxNode _objectPath;
        private readonly SyntaxNode _action;
        private readonly SyntaxNode _taskOfIDisposable;
        private readonly GeneratorSettings _settings;

        public Generator() : this(new GeneratorSettings())
        {}

        public Generator(GeneratorSettings settings)
        {
            _settings = settings;
            _workspace = new AdhocWorkspace();
            _generator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);
            _iDBusObject = _generator.IdentifierName("IDBusObject");
            _dBusInterfaceAttribute = "DBusInterface";
            _dictionaryAttribute = _generator.Attribute(_generator.IdentifierName("Dictionary"));
            _task = _generator.IdentifierName("Task");
            _action = _generator.IdentifierName("Action");
            _objectPath = _generator.IdentifierName("ObjectPath");
            _taskOfIDisposable = _generator.GenericName("Task", _generator.IdentifierName("IDisposable"));
        }

        private SyntaxNode[] ImportNamespaceDeclarations()
        {
            return new[] {
                _generator.NamespaceImportDeclaration("System"),
                _generator.NamespaceImportDeclaration(_generator.DottedName("System.Collections.Generic")),
                _generator.NamespaceImportDeclaration(_generator.DottedName("System.Runtime.CompilerServices")),
                _generator.NamespaceImportDeclaration(_generator.DottedName("System.Threading.Tasks")),
                _generator.NamespaceImportDeclaration(_generator.DottedName("Tmds.DBus")),
            };
        }

        public string Generate(IEnumerable<InterfaceDescription> interfaceDescriptions)
        {
            var importDeclarations = ImportNamespaceDeclarations();
            var namespaceDeclarations = new List<SyntaxNode>();
            var internalsVisibleTo = _generator.Attribute("InternalsVisibleTo", _generator.DottedName("Tmds.DBus.Connection.DynamicAssemblyName"));
            foreach (var interfaceDescription in interfaceDescriptions)
            {
                namespaceDeclarations.AddRange(DBusInterfaceDeclaration(interfaceDescription.Name, interfaceDescription.InterfaceXml));
            }
            var namespaceDeclaration = _generator.NamespaceDeclaration(_generator.DottedName(_settings.Namespace), namespaceDeclarations);
            var compilationUnit = _generator.AddAttributes(_generator.CompilationUnit(importDeclarations.Concat(new[] { namespaceDeclaration })), internalsVisibleTo);
            return compilationUnit.NormalizeWhitespace().ToFullString();
        }

        private IEnumerable<SyntaxNode> DBusInterfaceDeclaration(string name, XElement interfaceXml)
        {
            yield return InterfaceDeclaration(name, interfaceXml);
            if (HasProperties(interfaceXml))
            {
                yield return PropertiesClassDeclaration(name, interfaceXml);
            }
        }

        private bool HasProperties(XElement interfaceXml)
            => interfaceXml.Elements("property").Any();

        private SyntaxNode InterfaceDeclaration(string name, XElement interfaceXml)
        {
            string fullName = interfaceXml.Attribute("name").Value;

            var methodDeclarations = interfaceXml.Elements("method").Select(MethodDeclaration);
            var signalDeclarations = interfaceXml.Elements("signal").Select(SignalDeclaration);
            var propertiesDeclarations = HasProperties(interfaceXml) ? PropertiesDeclaration(name) : Array.Empty<SyntaxNode>();

            var dbusInterfaceAttribute = _generator.Attribute(_dBusInterfaceAttribute, _generator.LiteralExpression(fullName));
            var interfaceDeclaration = _generator.InterfaceDeclaration($"I{name}", null, Accessibility.NotApplicable,
                interfaceTypes: new[] { _iDBusObject },
                members: methodDeclarations.Concat(signalDeclarations).Concat(propertiesDeclarations));

            return _generator.AddAttributes(interfaceDeclaration, dbusInterfaceAttribute);
        }

        private SyntaxNode PropertiesClassDeclaration(string name, XElement interfaceXml)
        {
            var propertiesXml = interfaceXml.Elements("property");
            var fields = propertiesXml.Select(PropertyToField);
            var propClass = _generator.ClassDeclaration($"{name}Properties", null, Accessibility.NotApplicable, DeclarationModifiers.None, null, null, fields);
            return _generator.AddAttributes(propClass, _dictionaryAttribute);
        }

        private SyntaxNode PropertyToField(XElement propertyXml)
        {
            string name = propertyXml.Attribute("name").Value;
            string dbusType = propertyXml.Attribute("type").Value;
            SyntaxNode type = ParseType(dbusType);
            return _generator.FieldDeclaration(name, type, Accessibility.Public, DeclarationModifiers.None, _generator.DefaultExpression(type));
        }

        private SyntaxNode[] PropertiesDeclaration(string name)
        {
             return new [] {
                 _generator.MethodDeclaration("GetAsync",
                    new[] { _generator.ParameterDeclaration("prop", _generator.TypeExpression(SpecialType.System_String)) },
                    new[] { "T" }, _generator.GenericName("Task", _generator.IdentifierName("T"))),
                 _generator.MethodDeclaration("GetAllAsync",
                    null,
                    null, _generator.GenericName("Task", _generator.IdentifierName($"{name}Properties"))),
                 _generator.MethodDeclaration("SetAsync",
                    new[] { _generator.ParameterDeclaration("prop", _generator.TypeExpression(SpecialType.System_String)), _generator.ParameterDeclaration("val", _generator.TypeExpression(SpecialType.System_Object)) },
                    null, _task),
                 _generator.MethodDeclaration("WatchPropertiesAsync",
                    new[] { _generator.ParameterDeclaration("handler", _generator.GenericName("Action", _generator.IdentifierName("PropertyChanges"))) },
                    null, _taskOfIDisposable),
             };
        }

        private SyntaxNode SignalDeclaration(XElement signalXml)
        {
            string name = signalXml.Attribute("name").Value;
            var args = signalXml.Elements("arg");
            var inArgType = args.Count() == 0 ? _action : _generator.GenericName("Action", MultyArgsToType(args));
            var inParameters = new[] { _generator.ParameterDeclaration("action", inArgType) };
            var methodDeclaration = _generator.MethodDeclaration($"Watch{name}Async", inParameters, null, _taskOfIDisposable);
            return methodDeclaration;
        }

        private SyntaxNode MethodDeclaration(XElement methodXml)
        {
            string name = methodXml.Attribute("name").Value;
            var inArgs = methodXml.Elements("arg").Where(arg => (arg.Attribute("direction")?.Value ?? "in") == "in");
            var outArgs = methodXml.Elements("arg").Where(arg => arg.Attribute("direction")?.Value == "out");
            var returnType = outArgs.Count() == 0 ? _task :
                             _generator.GenericName("Task", new[] { MultyArgsToType(outArgs) });

            var methodDeclaration = _generator.MethodDeclaration($"{name}Async", inArgs.Select(InArgToParameter), null, returnType);
            return methodDeclaration;
        }

        private SyntaxNode MultyArgsToType(IEnumerable<XElement> outArgs)
        {
            var dbusType = string.Join(string.Empty, outArgs.Select(arg => arg.Attribute("type").Value));
            var elementNames = outArgs.Select(element => (string)element.Attribute("name")).ToList();
            if (outArgs.Count() > 1)
            {
                dbusType = $"({dbusType})";
            }
            return ParseType(dbusType, elementNames);
        }

        private SyntaxNode ParseType(string dbusType, List<string> elementNames = null)
        {
            int index = 0;
            var type = ParseType(dbusType, ref index, elementNames);
            if (index != dbusType.Length)
            {
                throw new InvalidOperationException($"Unable to parse dbus type: {dbusType}");
            }
            return type;
        }

        private SyntaxNode ParseType(string dbusType, ref int index, List<string> elementNames = null)
        {
            switch (dbusType[index++])
            {
                case 'y': return _generator.TypeExpression(SpecialType.System_Byte);
                case 'b': return _generator.TypeExpression(SpecialType.System_Boolean);
                case 'n': return _generator.TypeExpression(SpecialType.System_Int16);
                case 'q': return _generator.TypeExpression(SpecialType.System_UInt16);
                case 'i': return _generator.TypeExpression(SpecialType.System_Int32);
                case 'u': return _generator.TypeExpression(SpecialType.System_UInt32);
                case 'x': return _generator.TypeExpression(SpecialType.System_Int64);
                case 't': return _generator.TypeExpression(SpecialType.System_UInt64);
                case 'd': return _generator.TypeExpression(SpecialType.System_Double);
                case 's': return _generator.TypeExpression(SpecialType.System_String);
                case 'o': return _objectPath;
                case 'f': return _generator.TypeExpression(SpecialType.System_Single);
                case 'a': // array
                    if (dbusType[index] == '{')
                    {
                        index++;
                        // 'a{..} // dictionary
                        var keyType = ParseType(dbusType, ref index);
                        var valueType = ParseType(dbusType, ref index);
                        if (dbusType[index++] != '}')
                        {
                            throw new InvalidOperationException($"Unable to parse dbus type: {dbusType}");
                        }
                        return _generator.GenericName("IDictionary", new[] { keyType, valueType } );
                    }
                    else
                    {
                        var arrayType = ParseType(dbusType, ref index);
                        return _generator.ArrayTypeExpression(arrayType);
                    }
                case '(': // struct
                    var elements = new List<TupleElementSyntax>();
                    var memberTypes = new List<SyntaxNode>();
                    SyntaxNode type = null;
                    int idx = 0;
                    do
                    {
                        type = ParseType(dbusType, ref index);
                        if (type != null)
                        {
                            memberTypes.Add(type);
                            var name = elementNames != null && idx < elementNames.Count ? elementNames[idx] : null;
                            idx++;
                            elements.Add(SyntaxFactory.TupleElement((TypeSyntax)type, name != null ? SyntaxFactory.Identifier(name) : default(SyntaxToken)));
                        }
                    } while (type != null);
                    if (memberTypes.Count < 2)
                    {
                        return _generator.GenericName("ValueTuple", memberTypes);
                    }
                    else
                    {
                        return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
                    }
                case 'v': return _generator.TypeExpression(SpecialType.System_Object);
                case ')':
                case '}':
                    return null;
                default:
                    throw new NotSupportedException($"Unexpected character '{dbusType[index]}' while parsing dbus type '{dbusType}'");
            }
        }

        private SyntaxNode InArgToParameter(XElement argXml, int idx)
        {
            var type = ParseType(argXml.Attribute("type").Value);
            var name = (string)argXml.Attribute("name");
            return _generator.ParameterDeclaration(name ?? $"arg{idx}", type);
        }
    }
}
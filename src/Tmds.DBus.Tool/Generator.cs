using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Tmds.DBus.Tool.Diagnostics;

namespace Tmds.DBus.Tool
{
    public class GeneratorSettings
    {
        public string Namespace { get; set; } = "DBus";
        public bool NoInternalsVisibleTo = false;
        public Accessibility TypesAccessModifier = Accessibility.NotApplicable;
    }

    public class Generator : IGenerator
    {
        private readonly AdhocWorkspace _workspace;
        private readonly SyntaxGenerator _generator;
        private readonly SyntaxNode _iDBusObject;
        private readonly string _dBusInterfaceAttribute;
        private readonly SyntaxNode _dictionaryAttribute;
        private readonly SyntaxNode _task;
        private readonly SyntaxNode _objectPath;
        private readonly SyntaxNode _signature;
        private readonly SyntaxNode _closeSafeHandle;
        private readonly SyntaxNode _action;
        private readonly SyntaxNode _taskOfIDisposable;
        private readonly SyntaxNode _actionOfException;
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
            _signature = _generator.IdentifierName("Signature");
            _closeSafeHandle = _generator.IdentifierName("CloseSafeHandle");
            _taskOfIDisposable = _generator.GenericName("Task", _generator.IdentifierName("IDisposable"));
            _actionOfException = _generator.GenericName("Action", _generator.IdentifierName("Exception"));
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
                try
                {
                    namespaceDeclarations.AddRange(DBusInterfaceDeclaration(interfaceDescription.Name, interfaceDescription.InterfaceXml));
                }
                catch (GenerationException ge)
                {
                    ge.Inform(interfaceDescription);
                    throw;
                }
                catch (Exception e)
                {
                    throw new GenerationException("Failed to generate code for D-Bus interface", interfaceDescription.InterfaceXml, interfaceDescription, e);
                }
            }
            var namespaceDeclaration = _generator.NamespaceDeclaration(_generator.DottedName(_settings.Namespace), namespaceDeclarations);
            var compilationUnit = _generator.CompilationUnit(importDeclarations.Concat(new[] { namespaceDeclaration }));
            if (!_settings.NoInternalsVisibleTo)
            {
                compilationUnit = _generator.AddAttributes(compilationUnit, internalsVisibleTo);
            }
            return compilationUnit.NormalizeWhitespace().ToFullString();
        }

        private IEnumerable<SyntaxNode> DBusInterfaceDeclaration(string name, XElement interfaceXml)
        {
            yield return InterfaceDeclaration(name, interfaceXml);
            if (Properties(interfaceXml).Any())
            {
                yield return PropertiesClassDeclaration(name, ReadableProperties(interfaceXml));
                yield return PropertiesExtensionMethodClassDeclaration(name, interfaceXml);
            }
        }

        private IEnumerable<XElement> Properties(XElement interfaceXml)
            => interfaceXml.Elements("property");

        private IEnumerable<XElement> ReadableProperties(XElement interfaceXml)
            => Properties(interfaceXml).Where(p => p.Attribute("access").Value.StartsWith("read", StringComparison.Ordinal));

        private IEnumerable<XElement> WritableProperties(XElement interfaceXml)
            => Properties(interfaceXml).Where(p => p.Attribute("access").Value.EndsWith("write", StringComparison.Ordinal));

        private SyntaxNode InterfaceDeclaration(string name, XElement interfaceXml)
        {
            string fullName = interfaceXml.Attribute("name").Value;

            var methodDeclarations = interfaceXml.Elements("method").Select(MethodDeclaration);
            var signalDeclarations = interfaceXml.Elements("signal").Select(SignalDeclaration);
            var propertiesDeclarations = Properties(interfaceXml).Any() ? PropertiesDeclaration(name) : Array.Empty<SyntaxNode>();

            var dbusInterfaceAttribute = _generator.Attribute(_dBusInterfaceAttribute, _generator.LiteralExpression(fullName));
            var interfaceDeclaration = _generator.InterfaceDeclaration($"I{name}", null, _settings.TypesAccessModifier,
                interfaceTypes: new[] { _iDBusObject },
                members: methodDeclarations.Concat(signalDeclarations).Concat(propertiesDeclarations));

            return _generator.AddAttributes(interfaceDeclaration, dbusInterfaceAttribute);
        }

        private SyntaxNode PropertiesClassDeclaration(string name, IEnumerable<XElement> propertyXmls)
        {
            var propertyDeclarations = propertyXmls.Select(PropertyToDeclarations);
            var propClass = _generator.ClassDeclaration($"{name}Properties", null, _settings.TypesAccessModifier, DeclarationModifiers.None, null, null, propertyDeclarations.SelectMany(d => d));
            return _generator.AddAttributes(propClass, _dictionaryAttribute);
        }

        private SyntaxNode PropertiesExtensionMethodClassDeclaration(string name, XElement interfaceXml)
        {
            List<SyntaxNode> methods = new List<SyntaxNode>();
            var interfaceName = $"I{name}";
            foreach (var propertyXml in ReadableProperties(interfaceXml))
            {
                methods.Add(PropertyToGet(interfaceName, propertyXml));
            }
            foreach (var propertyXml in WritableProperties(interfaceXml))
            {
                methods.Add(PropertyToSet(interfaceName, propertyXml));
            }
            return _generator.ClassDeclaration($"{name}Extensions", null, _settings.TypesAccessModifier, DeclarationModifiers.Static, null, null, methods);
        }

        private SyntaxNode PropertyToGet(string interfaceName, XElement propertyXml)
        {
            string name = propertyXml.Attribute("name").Value;
            XAttribute dbusType = propertyXml.Attribute("type");
            var returnType = (TypeSyntax)ParseType(dbusType);
            return SyntaxFactory.MethodDeclaration(
                attributeLists: default(SyntaxList<AttributeListSyntax>),
                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                returnType: SyntaxFactory.GenericName(SyntaxFactory.Identifier("Task"), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(returnType))),
                explicitInterfaceSpecifier: default(ExplicitInterfaceSpecifierSyntax),
                identifier: ToIdentifierToken($"Get{Prettify(name)}Async"),
                typeParameterList: null,
                parameterList: SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Parameter(
                        attributeLists: default(SyntaxList<AttributeListSyntax>),
                        modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)),
                        type: SyntaxFactory.IdentifierName(interfaceName),
                        identifier: SyntaxFactory.Identifier("o"),
                        @default: null))),
                constraintClauses: default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                body: null,
                expressionBody: SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.InvocationExpression(
                        expression: SyntaxFactory.MemberAccessExpression(
                            kind: SyntaxKind.SimpleMemberAccessExpression,
                            expression: SyntaxFactory.IdentifierName("o"),
                            name: SyntaxFactory.GenericName(SyntaxFactory.Identifier("GetAsync")).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(returnType)))),
                        argumentList: SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name))))))
                ),
                semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private SyntaxNode PropertyToSet(string interfaceName, XElement propertyXml)
        {
            string name = propertyXml.Attribute("name").Value;
            XAttribute dbusType = propertyXml.Attribute("type");
            var returnType = (TypeSyntax)ParseType(dbusType);
            return SyntaxFactory.MethodDeclaration(
                attributeLists: default(SyntaxList<AttributeListSyntax>),
                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                returnType: SyntaxFactory.IdentifierName("Task"),
                explicitInterfaceSpecifier: default(ExplicitInterfaceSpecifierSyntax),
                identifier: ToIdentifierToken($"Set{Prettify(name)}Async"),
                typeParameterList: null,
                parameterList: SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList<ParameterSyntax>(new[] {
                        SyntaxFactory.Parameter(
                            attributeLists: default(SyntaxList<AttributeListSyntax>),
                            modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)),
                            type: SyntaxFactory.IdentifierName(interfaceName),
                            identifier: SyntaxFactory.Identifier("o"),
                            @default: null),
                    SyntaxFactory.Parameter(
                            attributeLists: default(SyntaxList<AttributeListSyntax>),
                            modifiers: default(SyntaxTokenList),
                            type: returnType,
                            identifier: SyntaxFactory.Identifier("val"),
                            @default: null),
                            })),
                constraintClauses: default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                body: null,
                expressionBody: SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.InvocationExpression(
                        expression: SyntaxFactory.MemberAccessExpression(
                            kind: SyntaxKind.SimpleMemberAccessExpression,
                            expression: SyntaxFactory.IdentifierName("o"),
                            name: SyntaxFactory.IdentifierName("SetAsync")),
                        argumentList: SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new [] {
                            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name))),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("val")) })))
                ),
                semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private SyntaxNode[] PropertyToDeclarations(XElement propertyXml)
        {
            string name = propertyXml.Attribute("name").Value;
            var fieldName = $"_{name.Replace('-', '_')}";
            XAttribute dbusType = propertyXml.Attribute("type");
            SyntaxNode type = ParseType(dbusType);
            var field = _generator.FieldDeclaration(fieldName, type, Accessibility.Private, DeclarationModifiers.None, _generator.DefaultExpression(type));
            var property = _generator.PropertyDeclaration(Prettify(name), type, Accessibility.Public,
                getAccessorStatements: new SyntaxNode[] { _generator.ReturnStatement(_generator.IdentifierName(fieldName)) },
                setAccessorStatements:new SyntaxNode[]  { _generator.AssignmentStatement(_generator.IdentifierName(fieldName), _generator.IdentifierName("value"))});
            return new [] { field, property };
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
            var inParameters = new[] { _generator.ParameterDeclaration("handler", inArgType), _generator.ParameterDeclaration("onError", _actionOfException, _generator.NullLiteralExpression()) };
            var methodDeclaration = _generator.MethodDeclaration($"Watch{name}Async", inParameters, null, _taskOfIDisposable);
            return methodDeclaration;
        }

        private SyntaxNode MethodDeclaration(XElement methodXml)
        {
            try
            {
                string name = methodXml.Attribute("name").Value;
                var inArgs = methodXml.Elements("arg").Where(arg => (arg.Attribute("direction")?.Value ?? "in") == "in");
                var outArgs = methodXml.Elements("arg").Where(arg => arg.Attribute("direction")?.Value == "out");
                var returnType = outArgs.Count() == 0 ? _task : _generator.GenericName("Task", new[] { MultyArgsToType(outArgs) });

                var methodDeclaration = _generator.MethodDeclaration($"{name}Async", inArgs.Select(InArgToParameter), null, returnType);
                return methodDeclaration;
            }
            catch (GenerationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new GenerationException("Failed to generate code for method", methodXml, innerException: ex);
            }
        }

        private SyntaxNode MultyArgsToType(IEnumerable<XElement> argElements)
        {
            var args = argElements
                .Select(arg => new
                {
                    DbusType = ParseType(arg.Attribute("type")),
                    Name = Prettify((string)arg.Attribute("name"), startWithUpper: false)
                })
                .ToList();

            if (args.Count < 2)
            {
                return args.Single().DbusType;
            }

            var elements = args
                .Select(arg => SyntaxFactory.TupleElement((TypeSyntax)arg.DbusType, ToIdentifierToken(arg.Name)))
                .ToList();

            return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
        }

        private SyntaxNode ParseType(XAttribute dbusTypeAttribute)
        {
            try
            {
                string dbusType = dbusTypeAttribute.Value;
                if (string.IsNullOrEmpty(dbusType)) throw new GenerationException("Missing D-Bus type definition", dbusTypeAttribute);
                int index = 0;
                var type = ParseType(dbusType, ref index);
                if (index != dbusType.Length) throw new GenerationException("Malformed D-Bus type definition", dbusTypeAttribute);

                return type;
            }
            catch (GenerationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GenerationException("Failed to generate code for D-Bus type", dbusTypeAttribute, innerException: e);
            }
        }

        private SyntaxNode ParseType(string dbusType, ref int index)
        {
            char c;
            switch (c = dbusType[index++])
            {
                case 'y': return _generator.TypeExpression(SpecialType.System_Byte);
                case 'b': return _generator.TypeExpression(SpecialType.System_Boolean);
                case 'n': return _generator.TypeExpression(SpecialType.System_Int16);
                case 'q': return _generator.TypeExpression(SpecialType.System_UInt16);
                case 'i': return _generator.TypeExpression(SpecialType.System_Int32);
                case 'h': return _closeSafeHandle;
                case 'u': return _generator.TypeExpression(SpecialType.System_UInt32);
                case 'x': return _generator.TypeExpression(SpecialType.System_Int64);
                case 't': return _generator.TypeExpression(SpecialType.System_UInt64);
                case 'd': return _generator.TypeExpression(SpecialType.System_Double);
                case 's': return _generator.TypeExpression(SpecialType.System_String);
                case 'o': return _objectPath;
                case 'g': return _signature;
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
                    do
                    {
                        type = ParseType(dbusType, ref index);
                        if (type != null)
                        {
                            memberTypes.Add(type);
                            elements.Add(SyntaxFactory.TupleElement((TypeSyntax)type, default));
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
                    throw new NotSupportedException($"Unexpected character '{c}' while parsing dbus type '{dbusType}'");
            }
        }

        private SyntaxNode InArgToParameter(XElement argXml, int idx)
        {
            try
            {
                var type = ParseType(argXml.Attribute("type"));
                var name = Prettify((string)argXml.Attribute("name"));
                return _generator.ParameterDeclaration(name ?? $"arg{idx}", type);
            }
            catch (GenerationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new GenerationException("Failed to generate code for argument", argXml, innerException: e);
            }
        }

       private static string EscapeIdentifier(string identifier)
       {
            var nullIndex = identifier.IndexOf('\0');
            if (nullIndex >= 0)
            {
                identifier = identifier.Substring(0, nullIndex);
            }

            var needsEscaping = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None;

            return needsEscaping ? "@" + identifier : identifier;
        }

        private static SyntaxToken ToIdentifierToken(string identifier)
        {
            var escaped = EscapeIdentifier(identifier);

            if (escaped.Length == 0 || escaped[0] != '@')
            {
                return SyntaxFactory.Identifier(escaped);
            }

            var unescaped = identifier.StartsWith("@", StringComparison.Ordinal)
                ? identifier.Substring(1)
                : identifier;

            var token = SyntaxFactory.Identifier(
                default(SyntaxTriviaList), SyntaxKind.None, "@" + unescaped, unescaped, default(SyntaxTriviaList));

            if (!identifier.StartsWith("@", StringComparison.Ordinal))
            {
                token = token.WithAdditionalAnnotations(Simplifier.Annotation);
            }

            return token;
        }

        public static string Prettify(string name, bool startWithUpper = true)
        {
            if (name == null)
            {
                return null;
            }
            bool upper = startWithUpper;
            var sb = new StringBuilder(name.Length);
            bool first = true;
            foreach (char c in name)
            {
                if (c == '_' || c == '-')
                {
                    upper = true;
                    continue;
                }
                sb.Append(upper ? char.ToUpper(c) : first && !startWithUpper ? char.ToLower(c) : c);
                upper = false;
                first = false;
            }
            return sb.ToString();
        }
    }
}
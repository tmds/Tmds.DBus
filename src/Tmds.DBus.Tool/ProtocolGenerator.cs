using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Tool
{
    class ProtocolGeneratorSettings
    {
        public string Namespace { get; set; } = "DBus";
        public Accessibility TypesAccessModifier = Accessibility.NotApplicable;
    }

    class ProtocolGenerator : IGenerator
    {
        private const string ConnectionType = "Connection";
        private readonly ProtocolGeneratorSettings _settings;
        private readonly StringBuilder _sb;
        private int _indentation = 0;

        public ProtocolGenerator(ProtocolGeneratorSettings settings)
        {
            _settings = settings;
            _sb = new StringBuilder();
        }

        private void StartBlock()
        {
            AppendLine("{");
            _indentation++;
        }

        private void EndBlock()
        {
            _indentation--;
            AppendLine("}");
        }

        private void AppendLine(string line)
        {
            if (line.Length != 0)
            {
                _sb.Append(' ', _indentation * 4);
            }
            _sb.AppendLine(line);
        }

        public string Generate(IEnumerable<InterfaceDescription> interfaceDescriptions)
        {
            _sb.Clear();
            AppendLine($"namespace {_settings.Namespace}");
            StartBlock();

            AppendLine($"using Tmds.DBus.Protocol;");
            AppendLine($"using SafeHandle = System.Runtime.InteropServices.SafeHandle;");

            AppendLine("");
            AppendLine($"class DBusObject");
            StartBlock();
            AppendLine("public Connection Connection { get; }");
            AppendLine("public string Service { get; }");
            AppendLine("public ObjectPath Path { get; }");
            AppendLine("");
            AppendLine("protected DBusObject(Connection connection, string service, ObjectPath path)");
            StartBlock();
            AppendLine("(Connection, Service, Path) = (connection, service, path);");
            EndBlock();
            EndBlock();

            foreach (var interf in interfaceDescriptions)
            {
                AppendLine("");
                AppendInterface(interf.Name, interf.InterfaceXml);
            }

            EndBlock();

            return _sb.ToString();
        }

        private void AppendInterface(string name, XElement interfaceXml)
        {
            AppendLine($"partial class {name} : DBusObject");
            StartBlock();

            string interfaceName = (string)interfaceXml.Attribute("name");
            AppendLine($"private const string Interface = \"{interfaceName}\";");

            AppendLine("");
            AppendLine($"public {name}({ConnectionType} connection, string service, ObjectPath path) : base(connection, service, path)");
            AppendLine("{ }");

            foreach (var method in interfaceXml.Elements("method"))
            {
                AppendLine("");
                AppendMethod(method);
            }

            EndBlock();
        }

        private static string TupleOf(IEnumerable<string> elements)
            => $"({string.Join(", ", elements)})";

        private void AppendMethod(XElement methodXml)
        {
            // System.Console.WriteLine(methodXml);
            string dbusMethodName = (string)methodXml.Attribute("name");
            var inArgs = methodXml.Elements("arg").Where(arg => (arg.Attribute("direction")?.Value ?? "in") == "in").Select(ToArgument).ToArray();
            var outArgs = methodXml.Elements("arg").Where(arg => arg.Attribute("direction")?.Value == "out").Select(ToArgument).ToArray();

            string dotnetReturnType = outArgs.Length == 0 ? null : outArgs.Length == 1 ? outArgs[0].DotnetType : $"({string.Join(", ", outArgs.Select(arg => $"{arg.DotnetType} {arg.NameUpper}"))})";

            string retType = dotnetReturnType == null ? "Task" : $"Task<{dotnetReturnType}>";

            string args = TupleOf(inArgs.Select(arg => $"{arg.DotnetType} {arg.NameLower}"));

            string dotnetMethodName = Prettify(dbusMethodName) + "Async";
            AppendLine($"public {retType} {dotnetMethodName}{args}");
            StartBlock();
            if (dotnetReturnType != null)
            {
                AppendLine($"return Connection.CallMethodAsync(CreateMessage(), (in Message m, object? s) => ReadResponse(in m, ({ConnectionType})s!), Connection);");
            }
            else
            {
                AppendLine($"return Connection.CallMethodAsync(CreateMessage());");
            }
            AppendLine("");

            AppendLine("MessageBuffer CreateMessage()");
            StartBlock();
            AppendLine("using var writer = Connection.GetMessageWriter();");
            AppendLine("");
            AppendLine("writer.WriteMethodCallHeader(");
            AppendLine($"    destination: Service,");
            AppendLine($"    path: Path,");
            AppendLine($"    @interface: Interface,");
            if (inArgs.Length > 0)
            {
                string signature = string.Join("", inArgs.Select(a => a.Type));
                AppendLine($"    signature: \"{signature}\",");
            }
            AppendLine($"    member: \"{dbusMethodName}\");");
            if (inArgs.Length > 0)
            {
                AppendLine("");
            }
            foreach (var inArg in inArgs)
            {
                string writeMethod = inArg.DBusType switch
                {
                    DBusType.Byte => "WriteByte",
                    DBusType.Bool => "WriteBool",
                    DBusType.Int16 => "WriteInt16",
                    DBusType.UInt16 => "WriteUInt16",
                    DBusType.Int32 => "WriteInt32",
                    DBusType.UInt32 => "WriteUInt32",
                    DBusType.Int64 => "WriteInt64",
                    DBusType.UInt64 => "WriteUInt64",
                    DBusType.Double => "WriteDouble",
                    DBusType.String => "WriteString",
                    DBusType.ObjectPath => "WriteObjectPath",
                    DBusType.Signature => "WriteSignature",
                    DBusType.Array => "WriteArray",
                    DBusType.Struct => "WriteStruct",
                    DBusType.Variant => "WriteVariant",
                    DBusType.DictEntry => "WriteDictionary",
                    DBusType.UnixFd => "WriteHandle",
                    _ => throw new IndexOutOfRangeException("Unknown type")
                };
                AppendLine($"writer.{writeMethod}({inArg.NameLower});");
            }
            AppendLine("");
            AppendLine("return writer.CreateMessage();");
            EndBlock();

            if (dotnetReturnType != null)
            {
                AppendLine("");
                AppendLine($"static {dotnetReturnType} ReadResponse(in Message message, {ConnectionType} connection)");
                StartBlock();
                AppendLine("var reader = message.GetBodyReader();");
                foreach (var outArg in outArgs)
                {
                    string readMethod = outArg.DBusType switch
                    {
                        DBusType.Byte => "ReadByte",
                        DBusType.Bool => "ReadBool",
                        DBusType.Int16 => "ReadInt16",
                        DBusType.UInt16 => "ReadUInt15",
                        DBusType.Int32 => "ReadInt32",
                        DBusType.UInt32 => "ReadUInt32",
                        DBusType.Int64 => "ReadInt64",
                        DBusType.UInt64 => "ReadUInt64",
                        DBusType.Double => "ReadDouble",
                        DBusType.String => "ReadString",
                        DBusType.ObjectPath => "ReadObjectPath",
                        DBusType.Signature => "ReadSignature",
                        DBusType.Array => $"ReadArray<{outArg.DotnetInnerTypes[0]}>",
                        DBusType.Struct => $"ReadStruct<{string.Join(", ", outArg.DotnetInnerTypes)}>",
                        DBusType.Variant => "ReadVariant",
                        DBusType.DictEntry => $"ReadDictionary<{outArg.DotnetInnerTypes[0]}, {outArg.DotnetInnerTypes[1]}>",
                        DBusType.UnixFd => "ReadHandle<SafeHandle>",
                        _ => throw new IndexOutOfRangeException("Unknown type")
                    };
                    AppendLine($"var {outArg.NameLower} = reader.{readMethod}();");
                }
                if (outArgs.Length == 1)
                {
                    AppendLine($"return {outArgs[0].NameLower};");
                }
                else
                {
                    AppendLine($"return {TupleOf(outArgs.Select(a => a.NameLower))};");
                }
                EndBlock();
            }

            EndBlock();
        }

        private Argument ToArgument(XElement argXml)
        {
            return new Argument(argXml);
        }

        class Argument
        {
            public Argument(XElement argXml)
            {
                Name = (string)argXml.Attribute("name");
                Type = (string)argXml.Attribute("type");
                (DotnetType, DotnetInnerTypes, DBusType) = DetermineType(Type);
            }

            public string Name { get; }
            public string NameUpper => Prettify(Name, startWithUpper: true);
            public string NameLower => Prettify(Name, startWithUpper: false);
            public string Type { get; }
            public string DotnetType { get; }
            public string[] DotnetInnerTypes { get; }
            public DBusType DBusType { get; }

            private (string, string[], DBusType) DetermineType(string signature)
            {
                DBusType dbusType = (DBusType)signature[0];

                Func<DBusType, (string, string[])[], (string, string[])> map = (dbusType, inner) =>
                {
                    string[] innerTypes = inner.Select(s => s.Item1).ToArray();
                    switch (dbusType)
                    {
                        case DBusType.Byte: return ("byte", innerTypes);
                        case DBusType.Bool: return ("bool", innerTypes);
                        case DBusType.Int16: return ("short", innerTypes);
                        case DBusType.UInt16: return ("ushort", innerTypes);
                        case DBusType.Int32: return ("int", innerTypes);
                        case DBusType.UInt32: return ("uint", innerTypes);
                        case DBusType.Int64: return ("long", innerTypes);
                        case DBusType.UInt64: return ("ulong", innerTypes);
                        case DBusType.Double: return ("double", innerTypes);
                        case DBusType.String: return ("string", innerTypes);
                        case DBusType.ObjectPath: return ("ObjectPath", innerTypes);
                        case DBusType.Signature: return ("Signature", innerTypes);
                        case DBusType.Variant: return ("object", innerTypes);
                        case DBusType.UnixFd: return ("SafeHandle", innerTypes);
                        case DBusType.Array: return ($"{innerTypes[0]}[]", innerTypes);
                        case DBusType.DictEntry: return ($"Dictionary<{innerTypes[0]}, {innerTypes[1]}>", innerTypes);
                        case DBusType.Struct: return ($"({string.Join(", ", innerTypes)})", innerTypes);
                    }
                    throw new IndexOutOfRangeException($"Invalid type {dbusType}");
                };
                (string dotnetType, string[] dotnetInnerTypes) = Tmds.DBus.Protocol.SignatureReader.Transform<(string, string[])>(Encoding.ASCII.GetBytes(signature), map);

                return (dotnetType, dotnetInnerTypes, dbusType);
            }
        }

        private static string Prettify(string name, bool startWithUpper = true)
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

            name = sb.ToString();
            if (Array.IndexOf(s_keywords, name) != -1)
            {
                return "@" + name;
            }
            return name;
        }

        private static readonly string[] s_keywords = new[] {
            "yield",
            "partial",
            "from",
            "group",
            "join",
            "into",
            "let",
            "by",
            "where",
            "select",
            "get",
            "set",
            "add",
            "remove",
            "orderby",
            "alias",
            "on",
            "equals",
            "ascending",
            "descending",
            "assembly",
            "module",
            "type",
            "field",
            "method",
            "param",
            "property",
            "typevar",
            "global",
            "async",
            "await",
            "when",
            "nameof",
            "_",
            "var",
            "and",
            "or",
            "not",
            "with",
            "init",
            "record",
            "managed",
            "unmanaged",
            "bool",
            "byte",
            "sbyte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "double",
            "float",
            "decimal",
            "string",
            "char",
            "void",
            "object",
            "typeof",
            "sizeof",
            "null",
            "true",
            "false",
            "if",
            "else",
            "while",
            "for",
            "foreach",
            "do",
            "switch",
            "case",
            "default",
            "lock",
            "try",
            "throw",
            "catch",
            "finally",
            "goto",
            "break",
            "continue",
            "public",
            "private",
            "internal",
            "protected",
            "static",
            "readonly",
            "sealed",
            "const",
            "fixed",
            "stackalloc",
            "volatile",
            "new",
            "override",
            "abstract",
            "virtual",
            "event",
            "extern",
            "ref",
            "out",
            "in",
            "is",
            "as",
            "params",
            "__arglist",
            "__makeref",
            "__reftype",
            "__refvalue",
            "this",
            "base",
            "namespace",
            "using",
            "class",
            "struct",
            "interface",
            "enum",
            "delegate",
            "checked",
            "unchecked",
            "unsafe",
            "operator",
            "implicit",
            "explicit" };
    }
}
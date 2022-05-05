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
        public string ServiceName { get; set; }
        public Accessibility TypesAccessModifier = Accessibility.NotApplicable;
    }

    class ProtocolGenerator : IGenerator
    {
        private readonly ProtocolGeneratorSettings _settings;
        private readonly StringBuilder _sb;
        private readonly Dictionary<string, (bool, Argument[])> _messageReadMethods;
        private readonly string _objectName;
        private readonly string _serviceClassName;
        private int _indentation = 0;

        public ProtocolGenerator(ProtocolGeneratorSettings settings)
        {
            _settings = settings;
            _sb = new StringBuilder();
            _messageReadMethods = new Dictionary<string, (bool, Argument[])>();
            _objectName = $"{settings.ServiceName}Object";
            _serviceClassName = $"{settings.ServiceName}Service";
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
            if (line.Length == 0)
            {
                return;
            }
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

            AppendLine($"using System;");
            AppendLine($"using Tmds.DBus.Protocol;");
            AppendLine($"using SafeHandle = System.Runtime.InteropServices.SafeHandle;");
            AppendLine($"using System.Collections.Generic;");
            AppendLine("using System.Threading.Tasks;");

            foreach (var interf in interfaceDescriptions)
            {
                AppendLine("");
                AppendInterface(interf.Name, interf.InterfaceXml);
            }

            AppendServiceClass(interfaceDescriptions);
            AppendObjectClass();

            AppendPropertiesChangedClass();

            EndBlock();

            return _sb.ToString();
        }

        private void AppendPropertiesChangedClass()
        {
            AppendLine("");
            AppendLine("class PropertyChanges<TProperties>");
            StartBlock();
            AppendLine("public PropertyChanges(TProperties properties, string[] invalidated, string[] changed)");
            AppendLine("	=> (Properties, Invalidated, Changed) = (properties, invalidated, changed);");
            AppendLine("public TProperties Properties { get; }");
            AppendLine("public string[] Invalidated { get; }");
            AppendLine("public string[] Changed { get; }");
            AppendLine("public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;");
            AppendLine("public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;");
            EndBlock();
        }

        private void AppendServiceClass(IEnumerable<InterfaceDescription> interfaceDescriptions)
        {
            AppendLine($"partial class {_serviceClassName}");
            StartBlock();
            AppendLine("public Tmds.DBus.Protocol.Connection Connection { get; }");
            AppendLine("public string Destination { get; }");
            AppendLine($"public {_serviceClassName}(Tmds.DBus.Protocol.Connection connection, string destination)");
            AppendLine("    => (Connection, Destination) = (connection, destination);");
            foreach (var interf in interfaceDescriptions)
            {
                string interfaceName = interf.Name;
                AppendLine($"public {interfaceName} Create{interfaceName}(string path) => new {interfaceName}(this, path);");
            }
            EndBlock();
        }

        private void AppendObjectClass()
        {
            AppendLine("");
            AppendLine($"class {_objectName}");
            StartBlock();
            AppendLine($"public {_serviceClassName} Service {{ get; }}");
            AppendLine("public ObjectPath Path { get; }");
            AppendLine("protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;");
            AppendLine("");
            AppendLine($"protected {_objectName}({_serviceClassName} service, ObjectPath path)");
            AppendLine("    => (Service, Path) = (service, path);");

            AppendLine("");
            AppendLine("protected MessageBuffer CreateGetPropertyMessage(string @interface, string property)");
            StartBlock();
            AppendLine("using var writer = this.Connection.GetMessageWriter();");
            AppendLine("");
            AppendLine("writer.WriteMethodCallHeader(");
            AppendLine("    destination: Service.Destination,");
            AppendLine("    path: Path,");
            AppendLine("    @interface: \"org.freedesktop.DBus.Properties\",");
            AppendLine("    signature: \"ss\",");
            AppendLine("    member: \"Get\");");
            AppendLine("");
            AppendLine("writer.WriteString(@interface);");
            AppendLine("writer.WriteString(property);");
            AppendLine("");
            AppendLine("return writer.CreateMessage();");
            EndBlock();

            AppendLine("");
            AppendLine("protected MessageBuffer CreateGetAllPropertiesMessage(string @interface)");
            StartBlock();
            AppendLine("using var writer = this.Connection.GetMessageWriter();");
            AppendLine("");
            AppendLine("writer.WriteMethodCallHeader(");
            AppendLine("    destination: Service.Destination,");
            AppendLine("    path: Path,");
            AppendLine("    @interface: \"org.freedesktop.DBus.Properties\",");
            AppendLine("    signature: \"s\",");
            AppendLine("    member: \"GetAll\");");
            AppendLine("");
            AppendLine("writer.WriteString(@interface);");
            AppendLine("");
            AppendLine("return writer.CreateMessage();");
            EndBlock();

            AppendLine("");
            AppendLine("protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext)");
            StartBlock();
            AppendLine("var rule = new MatchRule");
            AppendLine("{");
            AppendLine("    Type = MessageType.Signal,");
            AppendLine("    Sender = Service.Destination,");
            AppendLine("    Path = Path,");
            AppendLine("    Interface = \"org.freedesktop.DBus.Properties\",");
            AppendLine("    Member = \"PropertiesChanged\",");
            AppendLine("    Arg0 = @interface");
            AppendLine("};");
            AppendLine("return this.Connection.AddMatchAsync(rule, reader,");
            AppendLine("                                        (Exception? ex, PropertyChanges<TProperties> changes, object? rs, object? hs) => ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),");
            AppendLine("                                        this, handler, emitOnCapturedContext);");
            EndBlock();

            AppendLine("");
            AppendLine("public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext)");
            StartBlock();
            AppendLine("var rule = new MatchRule");
            AppendLine("{");
            AppendLine("    Type = MessageType.Signal,");
            AppendLine("    Sender = sender,");
            AppendLine("    Path = path,");
            AppendLine("    Member = signal");
            AppendLine("};");
            AppendLine("return this.Connection.AddMatchAsync(rule, reader,");
            AppendLine("                                        (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),");
            AppendLine("                                        this, handler, emitOnCapturedContext);");
            EndBlock();

            AppendLine("");
            AppendLine("public ValueTask<IDisposable> WatchSignalAsync(string sender, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext)");
            StartBlock();
            AppendLine("var rule = new MatchRule");
            AppendLine("{");
            AppendLine("    Type = MessageType.Signal,");
            AppendLine("    Sender = sender,");
            AppendLine("    Path = path,");
            AppendLine("    Member = signal");
            AppendLine("};");
            AppendLine("return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,");
            AppendLine("                                                (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);");
            EndBlock();

            foreach (var readMethod in _messageReadMethods)
            {
                AppendReadMessageMethod(readMethod.Key, readMethod.Value.Item1, readMethod.Value.Item2);
            }

            EndBlock();
        }

        private void AppendInterface(string name, XElement interfaceXml)
        {
            var readableProperties = ReadableProperties(interfaceXml).Select(ToArgument);
            var writableProperties = ReadableProperties(interfaceXml).Select(ToArgument);

            string propertiesClassName = $"{name}Properties";

            if (readableProperties.Any())
            {
                AppendLine("");
                AppendLine($"record {propertiesClassName}");
                StartBlock();
                foreach (var property in readableProperties)
                {
                    AppendLine($"public {property.DotnetType} {property.NameUpper} {{ get; set; }} = default!;");
                }
                EndBlock();
            }

            AppendLine($"partial class {name} : {_objectName}");
            StartBlock();

            string interfaceName = (string)interfaceXml.Attribute("name");
            AppendLine($"private const string __Interface = \"{interfaceName}\";");

            AppendLine("");
            AppendLine($"public {name}({_serviceClassName} service, ObjectPath path) : base(service, path)");
            AppendLine("{ }");

            foreach (var method in interfaceXml.Elements("method"))
            {
                AppendLine("");
                AppendMethod(method);
            }

            foreach (var signal in interfaceXml.Elements("signal"))
            {
                AppendLine("");
                AppendSignal(name, signal);
            }

            foreach (var property in writableProperties)
            {
                AppendPropertySetMethod(property);
            }

            if (readableProperties.Any())
            {
                foreach (var property in readableProperties)
                {
                    AppendLine($"public Task<{property.DotnetType}> Get{property.NameUpper}Async()");
                    _indentation++;
                    string readMessageName = GetReadMessageMethodName(new[] { property }, variant: true);
                    AppendLine($"=> this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, \"{property.Name}\"), (Message m, object? s) => {readMessageName}(m, ({_objectName})s!), this);");
                    _indentation--;
                }

                // GetPropertiesAsync
                AppendLine($"public Task<{propertiesClassName}> GetPropertiesAsync()");
                StartBlock();
                AppendLine($"return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, ({_objectName})s!), this);");
                
                AppendLine($"static {propertiesClassName} ReadMessage(Message message, {_objectName} _)");
                StartBlock();
                AppendLine("var reader = message.GetBodyReader();");
                AppendLine("return ReadProperties(ref reader);");
                EndBlock(); // ReadMessage

                EndBlock(); // method

                // WatchPropertiesChangedAsync
                AppendLine($"public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<{propertiesClassName}>> handler, bool emitOnCapturedContext = true)");
                StartBlock();
                AppendLine($"return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, ({_objectName})s!), handler, emitOnCapturedContext);");
                AppendLine("");
                AppendLine($"static PropertyChanges<{propertiesClassName}> ReadMessage(Message message, {_objectName} _)");
                StartBlock();
                AppendLine("var reader = message.GetBodyReader();");
                AppendLine("reader.ReadString(); // interface");
                AppendLine("List<string> changed = new(), invalidated = new();");
                AppendLine($"return new PropertyChanges<{propertiesClassName}>(ReadProperties(ref reader, changed), changed.ToArray(), ReadInvalidated(ref reader));");
                EndBlock();
                AppendLine($"static string[] ReadInvalidated(ref Reader reader)");
                StartBlock();
                AppendLine("List<string>? invalidated = null;");
                AppendLine("ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.String);");
                AppendLine("while (reader.HasNext(headersEnd))");
                StartBlock();
                AppendLine("invalidated ??= new();");
                AppendLine("var property = reader.ReadString();");
                AppendLine("switch (property)");
                StartBlock();
                foreach (var property in readableProperties)
                {
                    AppendLine($"case \"{property.Name}\": invalidated.Add(\"{property.NameUpper}\"); break;");
                }
                EndBlock();
                EndBlock();
                AppendLine("return invalidated?.ToArray() ?? Array.Empty<string>();");
                EndBlock();
                EndBlock();

                // ReadProperties
                AppendLine($"private static {propertiesClassName} ReadProperties(ref Reader reader, List<string>? changedList = null)");
                StartBlock();
                AppendLine($"var props = new {propertiesClassName}();");
                AppendLine("ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);");
                AppendLine("while (reader.HasNext(headersEnd))");
                StartBlock();

                AppendLine("var property = reader.ReadString();");
                AppendLine("switch (property)");
                StartBlock();

                foreach (var property in readableProperties)
                {
                    AppendLine($"case \"{property.Name}\":");
                    _indentation++;
                    AppendLine($"reader.ReadSignature(\"{property.Type}\");");
                    AppendLine($"props.{property.NameUpper} = reader.{GetArgumentReadMethodName(property)}();");
                    AppendLine($"changedList?.Add(\"{property.NameUpper}\");");
                    AppendLine("break;");
                    _indentation--;
                }
                AppendLine("default:");
                _indentation++;
                AppendLine($"reader.ReadVariant();");
                AppendLine("break;");
                _indentation--;

                EndBlock(); // switch

                EndBlock(); // while

                AppendLine("return props;");
                EndBlock();
            }

            EndBlock();
        }

        private void AppendPropertySetMethod(Argument property)
        {
            string methodName = $"Set{property.NameUpper}Async";
            AppendLine($"public Task {methodName}({property.DotnetType} value)");
            StartBlock();
            AppendLine($"return this.Connection.CallMethodAsync(CreateMessage());");
            AppendLine("");
            AppendLine("MessageBuffer CreateMessage()");
            StartBlock();
            AppendLine("using var writer = this.Connection.GetMessageWriter();");
            AppendLine("");
            AppendLine("writer.WriteMethodCallHeader(");
            AppendLine("    destination: Service.Destination,");
            AppendLine("    path: Path,");
            AppendLine("    @interface: \"org.freedesktop.DBus.Properties\",");
            AppendLine("    signature: \"ssv\",");
            AppendLine("    member: \"Set\");");
            AppendLine("");
            AppendLine("writer.WriteString(__Interface);");
            AppendLine($"writer.WriteString(\"{property.Name}\");");
            AppendLine($"writer.WriteSignature(\"{property.Type}\");");
            AppendLine($"writer.{GetArgumentWriteMethodName(property)}(value);");
            AppendLine("");
            AppendLine("return writer.CreateMessage();");
            EndBlock();
            EndBlock();
        }

        private IEnumerable<XElement> Properties(XElement interfaceXml)
            => interfaceXml.Elements("property");

        private IEnumerable<XElement> ReadableProperties(XElement interfaceXml)
            => Properties(interfaceXml).Where(p => p.Attribute("access").Value.StartsWith("read", StringComparison.Ordinal));

        private IEnumerable<XElement> WritableProperties(XElement interfaceXml)
            => Properties(interfaceXml).Where(p => p.Attribute("access").Value.EndsWith("write", StringComparison.Ordinal));

        private void AppendSignal(string className, XElement signalXml)
        {
            string dbusSignalName = (string)signalXml.Attribute("name");

            var args = signalXml.Elements("arg").Select(ToArgument).ToArray();
            string watchType = args.Length == 0 ? null : args.Length == 1 ? args[0].DotnetType : TupleOf(args.Select(arg => $"{arg.DotnetType} {arg.NameUpper}"));
            string methodArg = watchType == null ? $"Action<Exception?>" : $"Action<Exception?, {watchType}>";
            string dotnetMethodName = "Watch" + Prettify(dbusSignalName) + "Async";
            AppendLine($"public ValueTask<IDisposable> {dotnetMethodName}({methodArg} handler, bool emitOnCapturedContext = true)");
            if (watchType == null)
            {
                AppendLine($"    => base.WatchSignalAsync(Service.Destination, Path, \"{dbusSignalName}\", handler, emitOnCapturedContext);");
            }
            else
            {
                string readMessageName = GetReadMessageMethodName(args, variant: false);
                AppendLine($"    => base.WatchSignalAsync(Service.Destination, Path, \"{dbusSignalName}\", (Message m, object? s) => {readMessageName}(m, ({_objectName})s!), handler, emitOnCapturedContext);");
            }
        }

        private string GetReadMessageMethodName(Argument[] args, bool variant)
        {
            string mangle = string.Join("", args.Select(arg => arg.Type)).Replace('{', 'e').Replace('(', 'r').Replace("}", "").Replace(")", "z");
            if (variant)
            {
                mangle = "v_" + mangle;
            }
            string methodName = "ReadMessage_" + mangle;
            if (!_messageReadMethods.ContainsKey(methodName))
            {
                _messageReadMethods.Add(methodName, (variant, args));
            }
            return methodName;
        }

        private static string TupleOf(IEnumerable<string> elements)
            => $"({string.Join(", ", elements)})";

        private void AppendMethod(XElement methodXml)
        {
            // System.Console.WriteLine(methodXml);
            string dbusMethodName = (string)methodXml.Attribute("name");
            var inArgs = methodXml.Elements("arg").Where(arg => (arg.Attribute("direction")?.Value ?? "in") == "in").Select(ToArgument).ToArray();
            var outArgs = methodXml.Elements("arg").Where(arg => arg.Attribute("direction")?.Value == "out").Select(ToArgument).ToArray();
            string dotnetReturnType = outArgs.Length == 0 ? null : outArgs.Length == 1 ? outArgs[0].DotnetType : TupleOf(outArgs.Select(arg => $"{arg.DotnetType} {arg.NameUpper}"));
            string retType = dotnetReturnType == null ? "Task" : $"Task<{dotnetReturnType}>";

            string args = TupleOf(inArgs.Select(arg => $"{arg.DotnetType} {arg.NameLower}"));

            string dotnetMethodName = Prettify(dbusMethodName) + "Async";
            AppendLine($"public {retType} {dotnetMethodName}{args}");
            StartBlock();
            if (dotnetReturnType != null)
            {
                string readMessageName = GetReadMessageMethodName(outArgs, variant: false);
                AppendLine($"return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => {readMessageName}(m, ({_objectName})s!), this);");
            }
            else
            {
                AppendLine($"return this.Connection.CallMethodAsync(CreateMessage());");
            }
            AppendLine("");

            AppendLine("MessageBuffer CreateMessage()");
            StartBlock();
            AppendLine("using var writer = this.Connection.GetMessageWriter();");
            AppendLine("");
            AppendLine("writer.WriteMethodCallHeader(");
            AppendLine($"    destination: Service.Destination,");
            AppendLine($"    path: Path,");
            AppendLine($"    @interface: __Interface,");
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
                string writeMethod = GetArgumentWriteMethodName(inArg);
                AppendLine($"writer.{writeMethod}({inArg.NameLower});");
            }
            AppendLine("");
            AppendLine("return writer.CreateMessage();");
            EndBlock();

            EndBlock();
        }

        private static string GetArgumentWriteMethodName(Argument inArg)
        {
            return inArg.DBusType switch
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
        }

        private void AppendReadMessageMethod(string name, bool variant, Argument[] args)
        {
            string dotnetReturnType = args.Length == 0 ? null : args.Length == 1 ? args[0].DotnetType : TupleOf(args.Select(arg => arg.DotnetType));
            AppendLine($"protected static {dotnetReturnType} {name}(Message message, {_objectName} _)");
            StartBlock();
            string signature = string.Join("", args.Select(a => a.Type));
            AppendLine("var reader = message.GetBodyReader();");
            if (variant)
            {
                AppendLine($"reader.ReadSignature(\"{signature}\");");
            }
            if (args.Length == 1)
            {
                AppendLine($"return reader.{GetArgumentReadMethodName(args[0])}();");
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    Argument arg = args[i];
                    string readMethod = GetArgumentReadMethodName(arg);
                    AppendLine($"var arg{i} = reader.{GetArgumentReadMethodName(args[i])}();");
                }
                AppendLine($"return {TupleOf(args.Select((a, i) => $"arg{i}"))};");
            }
            EndBlock();
        }

        private static string GetArgumentReadMethodName(Argument arg)
        {
            return arg.DBusType switch
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
                DBusType.Array => $"ReadArray<{arg.DotnetInnerTypes[0]}>",
                DBusType.Struct => $"ReadStruct<{string.Join(", ", arg.DotnetInnerTypes)}>",
                DBusType.Variant => "ReadVariant",
                DBusType.DictEntry => $"ReadDictionary<{arg.DotnetInnerTypes[0]}, {arg.DotnetInnerTypes[1]}>",
                DBusType.UnixFd => "ReadHandle<SafeHandle>",
                _ => throw new IndexOutOfRangeException("Unknown type")
            };
        }

        private Argument ToArgument(XElement argXml, int i)
        {
            return new Argument(i, argXml);
        }

        class Argument
        {
            public Argument(int i, XElement argXml)
            {
                Name = (string)argXml.Attribute("name") ?? $"a{i}";
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

                Func<DBusType, (string, string[], DBusType)[], (string, string[], DBusType)> map = (dbusType, inner) =>
                {
                    string[] innerTypes = inner.Select(s => s.Item1).ToArray();
                    switch (dbusType)
                    {
                        case DBusType.Byte: return ("byte", innerTypes, dbusType);
                        case DBusType.Bool: return ("bool", innerTypes, dbusType);
                        case DBusType.Int16: return ("short", innerTypes, dbusType);
                        case DBusType.UInt16: return ("ushort", innerTypes, dbusType);
                        case DBusType.Int32: return ("int", innerTypes, dbusType);
                        case DBusType.UInt32: return ("uint", innerTypes, dbusType);
                        case DBusType.Int64: return ("long", innerTypes, dbusType);
                        case DBusType.UInt64: return ("ulong", innerTypes, dbusType);
                        case DBusType.Double: return ("double", innerTypes, dbusType);
                        case DBusType.String: return ("string", innerTypes, dbusType);
                        case DBusType.ObjectPath: return ("ObjectPath", innerTypes, dbusType);
                        case DBusType.Signature: return ("Signature", innerTypes, dbusType);
                        case DBusType.Variant: return ("object", innerTypes, dbusType);
                        case DBusType.UnixFd: return ("SafeHandle", innerTypes, dbusType);
                        case DBusType.Array: return ($"{innerTypes[0]}[]", innerTypes, dbusType);
                        case DBusType.DictEntry: return ($"Dictionary<{innerTypes[0]}, {innerTypes[1]}>", innerTypes, dbusType);
                        case DBusType.Struct: return ($"({string.Join(", ", innerTypes)})", innerTypes, dbusType);
                    }
                    throw new IndexOutOfRangeException($"Invalid type {dbusType}");
                };
                (string dotnetType, string[] dotnetInnerTypes, DBusType dbusType2) = Tmds.DBus.Protocol.SignatureReader.Transform(Encoding.ASCII.GetBytes(signature), map);

                return (dotnetType, dotnetInnerTypes, dbusType2);
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
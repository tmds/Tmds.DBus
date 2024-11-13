using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Tmds.DBus.Protocol;
using Microsoft.Win32.SafeHandles;

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
        private readonly StringBuilder _sb = new();
        private readonly Dictionary<string, (bool, Argument[])> _messageReadMethods = new();
        private readonly Dictionary<string, string> _typeReadMethods = new();
        private readonly Dictionary<string, string> _typeWriteMethods = new();
        private readonly string _objectName;
        private readonly string _serviceClassName;
        private int _indentation = 0;

        public ProtocolGenerator(ProtocolGeneratorSettings settings)
        {
            _settings = settings;
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

        public bool TryGenerate(IEnumerable<InterfaceDescription> interfaceDescriptions, out string sourceCode)
        {
            _sb.Clear();
            AppendLine($"namespace {_settings.Namespace}");
            StartBlock();

            AppendLine($"using System;");
            AppendLine($"using Tmds.DBus.Protocol;");
            AppendLine($"using System.Collections.Generic;");
            AppendLine("using System.Threading.Tasks;");

            foreach (var interf in interfaceDescriptions)
            {
                try
                {
                    AppendLine("");
                    AppendInterface(interf.Name, interf.InterfaceXml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"There was an unexpected exception while generating code for the '{interf.Name}' interface:");
                    Console.WriteLine(interf.InterfaceXml);
                    Console.WriteLine();
                    Console.WriteLine("Exception:");
                    Console.WriteLine(ex);
                    sourceCode = default;
                    return false;
                }
            }

            AppendServiceClass(interfaceDescriptions);
            AppendObjectClass();

            AppendPropertiesChangedClass();

            EndBlock();

            sourceCode = _sb.ToString();
            return true;
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
                AppendLine($"public {interfaceName} Create{interfaceName}(ObjectPath path) => new {interfaceName}(this, path);");
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
            AppendLine("var writer = this.Connection.GetMessageWriter();");
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
            AppendLine("var writer = this.Connection.GetMessageWriter();");
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
            AppendLine("protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext, ObserverFlags flags)");
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
            AppendLine("                                        this, handler, emitOnCapturedContext, flags);");
            EndBlock();

            AppendLine("");
            AppendLine("public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext, ObserverFlags flags)");
            StartBlock();
            AppendLine("var rule = new MatchRule");
            AppendLine("{");
            AppendLine("    Type = MessageType.Signal,");
            AppendLine("    Sender = sender,");
            AppendLine("    Path = path,");
            AppendLine("    Member = signal,");
            AppendLine("    Interface = @interface");
            AppendLine("};");
            AppendLine("return this.Connection.AddMatchAsync(rule, reader,");
            AppendLine("                                        (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),");
            AppendLine("                                        this, handler, emitOnCapturedContext, flags);");
            EndBlock();

            AppendLine("");
            AppendLine("public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext, ObserverFlags flags)");
            StartBlock();
            AppendLine("var rule = new MatchRule");
            AppendLine("{");
            AppendLine("    Type = MessageType.Signal,");
            AppendLine("    Sender = sender,");
            AppendLine("    Path = path,");
            AppendLine("    Member = signal,");
            AppendLine("    Interface = @interface");
            AppendLine("};");
            AppendLine("return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,");
            AppendLine("                                                (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext, flags);");
            EndBlock();

            foreach (var readMethod in _messageReadMethods)
            {
                AppendReadMessageMethod(readMethod.Key, readMethod.Value.Item1, readMethod.Value.Item2);
            }

            foreach (var readMethod in _typeReadMethods)
            {
                AppendReadTypeMethod(readMethod.Key, readMethod.Value);
            }

            foreach (var writeMethod in _typeWriteMethods)
            {
                AppendWriteTypeMethod(writeMethod.Key, writeMethod.Value);
            }

            EndBlock();
        }

        private void AppendReadTypeMethod(string method, string signature)
        {
            string dotnetReturnType = GetDotnetReadType(signature);
            AppendLine($"protected static {dotnetReturnType} {method}(ref Reader reader)");
            StartBlock();
            SignatureReader reader = new SignatureReader(Encoding.UTF8.GetBytes(signature));
            if (reader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
            {
                reader = new SignatureReader(innerSignature);
                if (type == DBusType.Array)
                {
                    if (!reader.TryRead(out DBusType itemType, out ReadOnlySpan<byte> itemInnerSignature))
                    {
                        ThrowInvalidSignature(signature);
                    }
                    if (itemType == DBusType.DictEntry)
                    {
                        reader = new SignatureReader(itemInnerSignature);
                        if (!reader.TryRead(out DBusType keyType, out ReadOnlySpan<byte> keyInnerSignature))
                        {
                            ThrowInvalidSignature(signature);
                        }
                        if (!reader.TryRead(out DBusType valueType, out ReadOnlySpan<byte> valueInnerSignature))
                        {
                            ThrowInvalidSignature(signature);
                        }

                        string dotnetKeyType = GetDotnetReadType(keyType, keyInnerSignature);
                        string dotnetValueType = GetDotnetReadType(valueType, valueInnerSignature);
                        string keyTypeSignature = GetSignature(keyType, keyInnerSignature);
                        string valueTypeSignature = GetSignature(valueType, valueInnerSignature);

                        AppendLine($"Dictionary<{dotnetKeyType}, {dotnetValueType}> dictionary = new();");
                        AppendLine($"ArrayEnd dictEnd = reader.ReadDictionaryStart();");

                        AppendLine($"while (reader.HasNext(dictEnd))");
                        StartBlock();
                        AppendLine($"var key = {CallReadArgumentType(keyTypeSignature)};");
                        AppendLine($"var value = {CallReadArgumentType(valueTypeSignature)};");
                        AppendLine($"dictionary[key] = value;");
                        EndBlock();

                        AppendLine($"return dictionary;");
                    }
                    else
                    {
                        string dotnetItemType = GetDotnetReadType(itemType, itemInnerSignature);

                        AppendLine($"List<{dotnetItemType}> list = new();");
                        AppendLine($"ArrayEnd arrayEnd = reader.ReadArrayStart({GetDBusTypeEnumValue(itemType)});");

                        AppendLine($"while (reader.HasNext(arrayEnd))");
                        StartBlock();
                        AppendLine($"list.Add({CallReadArgumentType(Encoding.UTF8.GetString(innerSignature))});");
                        EndBlock();

                        AppendLine($"return list.ToArray();");
                    }
                }
                else if (type == DBusType.Struct)
                {
                    StringBuilder sb = new();
                    sb.Append("return (");
                    bool first = true;
                    while (reader.TryRead(out DBusType fieldType, out ReadOnlySpan<byte> fieldInnerSignature))
                    {
                        if (!first)
                        {
                            sb.Append(", ");
                        }
                        first = false;
                        sb.Append(CallReadArgumentType(GetSignature(fieldType, fieldInnerSignature)));
                    }
                    sb.Append(");");
                    AppendLine(sb.ToString());
                }
                else
                {
                    ThrowInvalidSignature(signature);
                }
            }
            EndBlock();
        }

        private void AppendWriteTypeMethod(string method, string signature)
        {
            string dotnetArgType = GetDotnetWriteType(signature);
            AppendLine($"protected static void {method}(ref MessageWriter writer, {dotnetArgType} value)");
            StartBlock();
            SignatureReader reader = new SignatureReader(Encoding.UTF8.GetBytes(signature));
            if (reader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
            {
                reader = new SignatureReader(innerSignature);
                if (type == DBusType.Array)
                {
                    if (!reader.TryRead(out DBusType itemType, out ReadOnlySpan<byte> itemInnerSignature))
                    {
                        ThrowInvalidSignature(signature);
                    }
                    if (itemType == DBusType.DictEntry)
                    {
                        reader = new SignatureReader(itemInnerSignature);
                        if (!reader.TryRead(out DBusType keyType, out ReadOnlySpan<byte> keyInnerSignature))
                        {
                            ThrowInvalidSignature(signature);
                        }
                        if (!reader.TryRead(out DBusType valueType, out ReadOnlySpan<byte> valueInnerSignature))
                        {
                            ThrowInvalidSignature(signature);
                        }

                        string keyTypeSignature = GetSignature(keyType, keyInnerSignature);
                        string valueTypeSignature = GetSignature(valueType, valueInnerSignature);

                        AppendLine($"ArrayStart arrayStart = writer.WriteDictionaryStart();");
                        AppendLine($"foreach (var item in value)");
                        StartBlock();
                        AppendLine($"writer.WriteDictionaryEntryStart();");
                        AppendLine($"{CallWriteArgumentType(keyTypeSignature, "item.Key")};");
                        AppendLine($"{CallWriteArgumentType(valueTypeSignature, "item.Value")};");
                        EndBlock();
                        AppendLine($"writer.WriteDictionaryEnd(arrayStart);");
                    }
                    else
                    {
                        string dotnetItemSignature = GetSignature(itemType, itemInnerSignature);

                        AppendLine($"ArrayStart arrayStart = writer.WriteArrayStart({GetDBusTypeEnumValue(itemType)});");
                        AppendLine($"foreach (var item in value)");
                        StartBlock();
                        AppendLine($"{CallWriteArgumentType(dotnetItemSignature, "item")};");
                        EndBlock();
                        AppendLine($"writer.WriteArrayEnd(arrayStart);");
                    }
                }
                else if (type == DBusType.Struct)
                {
                    AppendLine($"writer.WriteStructureStart();");
                    int i = 1;
                    while (reader.TryRead(out DBusType fieldType, out ReadOnlySpan<byte> fieldInnerSignature))
                    {
                        string fieldSignature = GetSignature(fieldType, fieldInnerSignature);
                        string parameterName = i < 8 ? $"value.Item{i}" : $"value.Rest.Item{1 + (i % 8)}";
                        AppendLine($"{CallWriteArgumentType(fieldSignature, parameterName)};");
                        i++;
                    }
                }
                else
                {
                    ThrowInvalidSignature(signature);
                }
            }
            EndBlock();
        }

        private void AppendInterface(string name, XElement interfaceXml)
        {
            var readableProperties = ReadableProperties(interfaceXml).Select(ToArgument);
            var writableProperties = WritableProperties(interfaceXml).Select(ToArgument);

            string propertiesClassName = $"{name}Properties";

            if (readableProperties.Any())
            {
                AppendLine("");
                AppendLine($"record {propertiesClassName}");
                StartBlock();
                foreach (var property in readableProperties)
                {
                    AppendLine($"public {property.DotnetReadType} {property.NameUpper} {{ get; set; }} = default!;");
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
                    AppendLine($"public Task<{property.DotnetReadType}> Get{property.NameUpper}Async()");
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
                AppendLine($"public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<{propertiesClassName}>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)");
                StartBlock();
                AppendLine($"return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, ({_objectName})s!), handler, emitOnCapturedContext, flags);");
                AppendLine("");
                AppendLine($"static PropertyChanges<{propertiesClassName}> ReadMessage(Message message, {_objectName} _)");
                StartBlock();
                AppendLine("var reader = message.GetBodyReader();");
                AppendLine("reader.ReadString(); // interface");
                AppendLine("List<string> changed = new(), invalidated = new();");
                AppendLine($"return new PropertyChanges<{propertiesClassName}>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());");
                EndBlock();
                AppendLine($"static string[] ReadInvalidated(ref Reader reader)");
                StartBlock();
                AppendLine("List<string>? invalidated = null;");
                AppendLine("ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);");
                AppendLine("while (reader.HasNext(arrayEnd))");
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
                AppendLine("ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);");
                AppendLine("while (reader.HasNext(arrayEnd))");
                StartBlock();

                AppendLine("var property = reader.ReadString();");
                AppendLine("switch (property)");
                StartBlock();

                foreach (var property in readableProperties)
                {
                    AppendLine($"case \"{property.Name}\":");
                    _indentation++;
                    AppendLine($"reader.ReadSignature(\"{property.Signature}\"u8);");
                    AppendLine($"props.{property.NameUpper} = {CallReadArgumentType(property.Signature)};");
                    AppendLine($"changedList?.Add(\"{property.NameUpper}\");");
                    AppendLine("break;");
                    _indentation--;
                }
                AppendLine("default:");
                _indentation++;
                AppendLine($"reader.ReadVariantValue();");
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
            AppendLine($"public Task {methodName}({property.DotnetWriteType} value)");
            StartBlock();
            AppendLine($"return this.Connection.CallMethodAsync(CreateMessage());");
            AppendLine("");
            AppendLine("MessageBuffer CreateMessage()");
            StartBlock();
            AppendLine("var writer = this.Connection.GetMessageWriter();");
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
            AppendLine($"writer.WriteSignature(\"{property.Signature}\");");
            AppendLine($"{CallWriteArgumentType(property.Signature, "value")};");
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
            string watchType = args.Length == 0 ? null : args.Length == 1 ? args[0].DotnetReadType : TupleOf(args.Select(arg => $"{arg.DotnetReadType} {arg.NameUpper}"));
            string methodArg = watchType == null ? $"Action<Exception?>" : $"Action<Exception?, {watchType}>";
            string dotnetMethodName = "Watch" + Prettify(dbusSignalName) + "Async";
            AppendLine($"public ValueTask<IDisposable> {dotnetMethodName}({methodArg} handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)");
            if (watchType == null)
            {
                AppendLine($"    => base.WatchSignalAsync(Service.Destination, __Interface, Path, \"{dbusSignalName}\", handler, emitOnCapturedContext, flags);");
            }
            else
            {
                string readMessageName = GetReadMessageMethodName(args, variant: false);
                AppendLine($"    => base.WatchSignalAsync(Service.Destination, __Interface, Path, \"{dbusSignalName}\", (Message m, object? s) => {readMessageName}(m, ({_objectName})s!), handler, emitOnCapturedContext, flags);");
            }
        }

        private string GetReadMessageMethodName(Argument[] args, bool variant)
        {
            string mangle = MangleSignatureForMethodName(string.Join("", args.Select(arg => arg.Signature)));
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

        private string GetReadTypeMethodName(string signature)
        {
            string mangle = MangleSignatureForMethodName(signature);
            string methodName = "ReadType_" + mangle;
            if (!_typeReadMethods.ContainsKey(methodName))
            {
                _typeReadMethods.Add(methodName, signature);

                // Ensure inner types are readable.
                CallForInnerSignatures(signature, sig => CallReadArgumentType(sig));
            }
            return methodName;
        }

        private void CallForInnerSignatures(string signature, Action<string> action)
        {
            SignatureReader reader = new SignatureReader(Encoding.UTF8.GetBytes(signature));
            if (reader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature) && innerSignature.Length > 0)
            {
                reader = new SignatureReader(innerSignature);
                if (type == DBusType.Array)
                {
                    if (!reader.TryRead(out DBusType itemType, out ReadOnlySpan<byte> itemInnerSignature))
                    {
                        ThrowInvalidSignature(signature);
                    }
                    if (itemType == DBusType.DictEntry)
                    {
                        reader = new SignatureReader(itemInnerSignature);
                        if (!reader.TryRead(out DBusType keyType, out ReadOnlySpan<byte> keyInnerSignature))
                        {
                            ThrowInvalidSignature(signature);
                        }
                        if (!reader.TryRead(out DBusType valueType, out ReadOnlySpan<byte> valueInnerSignature))
                        {
                            ThrowInvalidSignature(signature);
                        }
                        action(GetSignature(keyType, keyInnerSignature));
                        action(GetSignature(valueType, valueInnerSignature));
                    }
                    else
                    {
                        action(Encoding.UTF8.GetString(innerSignature));
                    }
                }
                else if (type == DBusType.Struct)
                {
                    while (reader.TryRead(out DBusType fieldType, out ReadOnlySpan<byte> fieldInnerSignature))
                    {
                        action(GetSignature(fieldType, fieldInnerSignature));
                    }
                }
                else
                {
                    ThrowInvalidSignature(signature);
                }
            }
        }

        private string GetWriteTypeMethodName(string signature)
        {
            string mangle = MangleSignatureForMethodName(signature);
            string methodName = "WriteType_" + mangle;
            if (!_typeWriteMethods.ContainsKey(methodName))
            {
                _typeWriteMethods.Add(methodName, signature);

                // Ensure inner types are writable.
                CallForInnerSignatures(signature, sig => CallWriteArgumentType(sig, "dummy"));
            }
            return methodName;
        }

        private static string MangleSignatureForMethodName(string signature)
        {
            return signature.Replace('{', 'e').Replace('(', 'r').Replace("}", "").Replace(")", "z");
        }

        private static string TupleOf(IEnumerable<string> elements)
            => $"({string.Join(", ", elements)})";

        private void AppendMethod(XElement methodXml)
        {
            // System.Console.WriteLine(methodXml);
            string dbusMethodName = (string)methodXml.Attribute("name");
            var inArgs = methodXml.Elements("arg").Where(arg => (arg.Attribute("direction")?.Value ?? "in") == "in").Select(ToArgument).ToArray();
            var outArgs = methodXml.Elements("arg").Where(arg => arg.Attribute("direction")?.Value == "out").Select(ToArgument).ToArray();
            string dotnetReturnType = outArgs.Length == 0 ? null : outArgs.Length == 1 ? outArgs[0].DotnetReadType : TupleOf(outArgs.Select(arg => $"{arg.DotnetReadType} {arg.NameUpper}"));
            string retType = dotnetReturnType == null ? "Task" : $"Task<{dotnetReturnType}>";

            string args = TupleOf(inArgs.Select(arg => $"{arg.DotnetWriteType} {arg.NameLower}"));

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
            AppendLine("var writer = this.Connection.GetMessageWriter();");
            AppendLine("");
            AppendLine("writer.WriteMethodCallHeader(");
            AppendLine($"    destination: Service.Destination,");
            AppendLine($"    path: Path,");
            AppendLine($"    @interface: __Interface,");
            if (inArgs.Length > 0)
            {
                string signature = string.Join("", inArgs.Select(a => a.Signature));
                AppendLine($"    signature: \"{signature}\",");
            }
            AppendLine($"    member: \"{dbusMethodName}\");");
            if (inArgs.Length > 0)
            {
                AppendLine("");
            }
            foreach (var inArg in inArgs)
            {
                AppendLine($"{CallWriteArgumentType(inArg.Signature, inArg.NameLower)};");
            }
            AppendLine("");
            AppendLine("return writer.CreateMessage();");
            EndBlock();

            EndBlock();
        }

        private void AppendReadMessageMethod(string name, bool variant, Argument[] args)
        {
            string dotnetReturnType = args.Length == 0 ? null : args.Length == 1 ? args[0].DotnetReadType : TupleOf(args.Select(arg => arg.DotnetReadType));
            AppendLine($"protected static {dotnetReturnType} {name}(Message message, {_objectName} _)");
            StartBlock();
            string signature = string.Join("", args.Select(a => a.Signature));
            AppendLine("var reader = message.GetBodyReader();");
            if (variant)
            {
                AppendLine($"reader.ReadSignature(\"{signature}\"u8);");
            }
            if (args.Length == 1)
            {
                AppendLine($"return {CallReadArgumentType(args[0].Signature)};");
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    AppendLine($"var arg{i} = {CallReadArgumentType(args[i].Signature)};");
                }
                AppendLine($"return {TupleOf(args.Select((a, i) => $"arg{i}"))};");
            }
            EndBlock();
        }

        private static string GetDBusTypeEnumValue(DBusType type)
        {
            return type switch
            {
                DBusType.Byte => $"{nameof(DBusType)}.{nameof(DBusType.Byte)}",
                DBusType.Bool => $"{nameof(DBusType)}.{nameof(DBusType.Bool)}",
                DBusType.Int16 => $"{nameof(DBusType)}.{nameof(DBusType.Int16)}",
                DBusType.UInt16 => $"{nameof(DBusType)}.{nameof(DBusType.UInt16)}",
                DBusType.Int32 => $"{nameof(DBusType)}.{nameof(DBusType.Int32)}",
                DBusType.UInt32 => $"{nameof(DBusType)}.{nameof(DBusType.UInt32)}",
                DBusType.Int64 => $"{nameof(DBusType)}.{nameof(DBusType.Int64)}",
                DBusType.UInt64 => $"{nameof(DBusType)}.{nameof(DBusType.UInt64)}",
                DBusType.Double => $"{nameof(DBusType)}.{nameof(DBusType.Double)}",
                DBusType.String => $"{nameof(DBusType)}.{nameof(DBusType.String)}",
                DBusType.ObjectPath => $"{nameof(DBusType)}.{nameof(DBusType.ObjectPath)}",
                DBusType.Signature => $"{nameof(DBusType)}.{nameof(DBusType.Signature)}",
                DBusType.Array => $"{nameof(DBusType)}.{nameof(DBusType.Array)}",
                DBusType.Struct => $"{nameof(DBusType)}.{nameof(DBusType.Struct)}",
                DBusType.Variant => $"{nameof(DBusType)}.{nameof(DBusType.Variant)}",
                DBusType.DictEntry => $"{nameof(DBusType)}.{nameof(DBusType.DictEntry)}",
                DBusType.UnixFd => $"{nameof(DBusType)}.{nameof(DBusType.UnixFd)}",
                _ => throw new ArgumentOutOfRangeException(type.ToString())
            };
        }

        private string CallWriteArgumentType(string signature, string parameterName)
        {
            switch (signature)
            {
                case "y":
                    return $"writer.{nameof(MessageWriter.WriteByte)}({parameterName})";
                case "b":
                    return $"writer.{nameof(MessageWriter.WriteBool)}({parameterName})";
                case "n":
                    return $"writer.{nameof(MessageWriter.WriteInt16)}({parameterName})";
                case "q":
                    return $"writer.{nameof(MessageWriter.WriteUInt16)}({parameterName})";
                case "i":
                    return $"writer.{nameof(MessageWriter.WriteInt32)}({parameterName})";
                case "u":
                    return $"writer.{nameof(MessageWriter.WriteUInt32)}({parameterName})";
                case "x":
                    return $"writer.{nameof(MessageWriter.WriteInt64)}({parameterName})";
                case "t":
                    return $"writer.{nameof(MessageWriter.WriteUInt64)}({parameterName})";
                case "d":
                    return $"writer.{nameof(MessageWriter.WriteDouble)}({parameterName})";
                case "s":
                    return $"writer.{nameof(MessageWriter.WriteString)}({parameterName})";
                case "o":
                    return $"writer.{nameof(MessageWriter.WriteObjectPath)}({parameterName})";
                case "g":
                    return $"writer.{nameof(MessageWriter.WriteSignature)}({parameterName})";
                case "v":
                    return $"writer.{nameof(MessageWriter.WriteVariant)}({parameterName})";
                case "h":
                    return $"writer.{nameof(MessageWriter.WriteHandle)}({parameterName})";

                case "ay":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ab":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "an":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "aq":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ai":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "au":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ax":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "at":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ad":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "as":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ao":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ag":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "av":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";
                case "ah":
                    return $"writer.{nameof(MessageWriter.WriteArray)}({parameterName})";

                case "a{sv}":
                    return $"writer.{nameof(MessageWriter.WriteDictionary)}({parameterName})";
            }

            return $"{GetWriteTypeMethodName(signature)}(ref writer, {parameterName})";
        }

        private string CallReadArgumentType(string signature)
        {
            switch (signature)
            {
                case "y":
                    return $"reader.{nameof(Reader.ReadByte)}()";
                case "b":
                    return $"reader.{nameof(Reader.ReadBool)}()";
                case "n":
                    return $"reader.{nameof(Reader.ReadInt16)}()";
                case "q":
                    return $"reader.{nameof(Reader.ReadUInt16)}()";
                case "i":
                    return $"reader.{nameof(Reader.ReadInt32)}()";
                case "u":
                    return $"reader.{nameof(Reader.ReadUInt32)}()";
                case "x":
                    return $"reader.{nameof(Reader.ReadInt64)}()";
                case "t":
                    return $"reader.{nameof(Reader.ReadUInt64)}()";
                case "d":
                    return $"reader.{nameof(Reader.ReadDouble)}()";
                case "s":
                    return $"reader.{nameof(Reader.ReadString)}()";
                case "o":
                    return $"reader.{nameof(Reader.ReadObjectPath)}()";
                case "g":
                    return $"reader.{nameof(Reader.ReadSignature)}()";
                case "v":
                    return $"reader.{nameof(Reader.ReadVariantValue)}()";
                case "h":
                    return $"reader.{nameof(Reader.ReadHandle)}<{typeof(SafeFileHandle).FullName}>()";

                case "ay":
                    return $"reader.{nameof(Reader.ReadArrayOfByte)}()";
                case "ab":
                    return $"reader.{nameof(Reader.ReadArrayOfBool)}()";
                case "an":
                    return $"reader.{nameof(Reader.ReadArrayOfInt16)}()";
                case "aq":
                    return $"reader.{nameof(Reader.ReadArrayOfUInt16)}()";
                case "ai":
                    return $"reader.{nameof(Reader.ReadArrayOfInt32)}()";
                case "au":
                    return $"reader.{nameof(Reader.ReadArrayOfUInt32)}()";
                case "ax":
                    return $"reader.{nameof(Reader.ReadArrayOfInt64)}()";
                case "at":
                    return $"reader.{nameof(Reader.ReadArrayOfUInt64)}()";
                case "ad":
                    return $"reader.{nameof(Reader.ReadArrayOfDouble)}()";
                case "as":
                    return $"reader.{nameof(Reader.ReadArrayOfString)}()";
                case "ao":
                    return $"reader.{nameof(Reader.ReadArrayOfObjectPath)}()";
                case "ag":
                    return $"reader.{nameof(Reader.ReadArrayOfSignature)}()";
                case "av":
                    return $"reader.{nameof(Reader.ReadArrayOfVariantValue)}()";
                case "ah":
                    return $"reader.{nameof(Reader.ReadArrayOfHandle)}<{typeof(SafeFileHandle).FullName}>()";

                case "a{sv}":
                    return $"reader.{nameof(Reader.ReadDictionaryOfStringToVariantValue)}()";

            }

            return $"{GetReadTypeMethodName(signature)}(ref reader)";
        }

        private static string GetDotnetReadType(string signature)
            => GetDotnetType(signature, true);

        private static string GetDotnetWriteType(string signature)
            => GetDotnetType(signature, false);

        private static string GetDotnetType(string signature, bool readNotWrite)
        {
            SignatureReader reader = new SignatureReader(Encoding.UTF8.GetBytes(signature));
            if (!reader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
            {
                ThrowInvalidSignature(signature);
            }
            return GetDotnetType(type, innerSignature, readNotWrite);
        }

        private static string GetDotnetReadType(DBusType type, ReadOnlySpan<byte> innerSignature)
            => GetDotnetType(type, innerSignature, true);

        private static string GetSignature(DBusType type, ReadOnlySpan<byte> innerSignature)
        {
            if (innerSignature.Length == 0)
            {
                return $"{(char)type}";
            }
            else if (type == DBusType.Array)
            {
                return $"a{Encoding.UTF8.GetString(innerSignature)}";
            }
            else if (type == DBusType.Struct)
            {
                return $"({Encoding.UTF8.GetString(innerSignature)})";
            }
            else if (type == DBusType.DictEntry)
            {
                return "{" + Encoding.UTF8.GetString(innerSignature) + "}";
            }
            else
            {
                throw new InvalidOperationException($"Cannot create signature for {type} and {Encoding.UTF8.GetString(innerSignature)}.");
            }
        }

        private static string GetDotnetType(DBusType type, ReadOnlySpan<byte> innerSignature, bool readNotWrite)
        {
            switch (type)
            {
               case DBusType.Byte:
                    return "byte";
               case DBusType.Bool:
                    return "bool";
               case DBusType.Int16:
                  return "short";
               case DBusType.UInt16:
                   return "ushort";
               case DBusType.Int32:
                   return "int";
               case DBusType.UInt32:
                   return "uint";
               case DBusType.Int64:
                   return "long";
               case DBusType.UInt64:
                   return "ulong";
               case DBusType.Double:
                   return "double";
               case DBusType.String:
                    return "string";
               case DBusType.ObjectPath:
                   return "ObjectPath";
               case DBusType.Signature:
                   return "Signature";
               case DBusType.Variant:
                   return "VariantValue";
               case DBusType.UnixFd:
                   return typeof(SafeHandle).FullName;

               case DBusType.Array:
                    {
                        SignatureReader reader = new SignatureReader(innerSignature);
                        if (!reader.TryRead(out DBusType itemtype, out ReadOnlySpan<byte> itemInnerSignature))
                        {
                            ThrowInvalidSignature(GetSignature(type, innerSignature));
                        }
                        bool isDictionary = itemtype == DBusType.DictEntry;
                        if (isDictionary)
                        {
                            reader = new SignatureReader(itemInnerSignature);
                            if (!reader.TryRead(out DBusType keyType, out ReadOnlySpan<byte> keyInnerSignature))
                            {
                                ThrowInvalidSignature(GetSignature(type, innerSignature));
                            }
                            if (!reader.TryRead(out DBusType valueType, out ReadOnlySpan<byte> valueInnerSignature))
                            {
                                ThrowInvalidSignature(GetSignature(type, innerSignature));
                            }
                            string dotnetKeyType = GetDotnetType(keyType, keyInnerSignature, readNotWrite);
                            string dotnetValueType = GetDotnetType(valueType, valueInnerSignature, readNotWrite);
                            return $"Dictionary<{dotnetKeyType}, {dotnetValueType}>";
                        }
                        else
                        {
                            string itemType = GetDotnetType(itemtype, itemInnerSignature, readNotWrite);
                            return $"{itemType}[]";
                        }
                    }
               case DBusType.Struct:
                    {
                        SignatureReader reader = new SignatureReader(innerSignature);
                        StringBuilder sb = new();
                        sb.Append("(");
                        bool first = true;
                        while (reader.TryRead(out DBusType fieldType, out ReadOnlySpan<byte> fieldInnerSignature))
                        {
                            if (!first)
                            {
                                sb.Append(", ");
                            }
                            first = false;
                            sb.Append(GetDotnetType(fieldType, fieldInnerSignature, readNotWrite));
                        }
                        sb.Append(")");
                        return sb.ToString();
                    }
            }

            throw new InvalidOperationException($"Cannot determine .NET type for {type} and {Encoding.UTF8.GetString(innerSignature)}.");
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
                Signature = (string)argXml.Attribute("type");
            }

            public string Name { get; }
            public string NameUpper => Prettify(Name, startWithUpper: true);
            public string NameLower => Prettify(Name, startWithUpper: false);
            public string Signature { get; }
            public string DotnetReadType => GetDotnetReadType(Signature);
            public string DotnetWriteType => GetDotnetWriteType(Signature);
        }

        private static void ThrowInvalidSignature(string signature)
        {
            throw new InvalidOperationException($"Invalid signature: {signature}");
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
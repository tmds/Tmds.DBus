using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    class CodeGenCommand : Command
    {
        CommandOption _serviceOption;
        CommandOption _busOption;
        CommandOption _pathOption;
        CommandOption _norecurseOption;
        CommandOption _namespaceOption;
        CommandOption _outputOption;
        CommandOption _catOption;
        CommandOption _skipOptions;
        CommandOption _interfaceOptions;
        CommandArgument _files;

        public CodeGenCommand(CommandLineApplication parent) :
            base("codegen", parent)
        {}

        public override void Configure()
        {
            _serviceOption = AddServiceOption();
            _busOption = AddBusOption();
            _pathOption = AddPathOption();
            _norecurseOption = AddNoRecurseOption();
            _namespaceOption = Configuration.Option("--namespace", "C# namespace (default: <service>)", CommandOptionType.SingleValue);
            _outputOption = Configuration.Option("--output", "File to write (default: <namespace>.cs)", CommandOptionType.SingleValue);
            _catOption = Configuration.Option("--cat", "Write to standard out instead of file", CommandOptionType.NoValue);
            _skipOptions = Configuration.Option("--skip", "DBus interfaces to skip", CommandOptionType.MultipleValue);
            _interfaceOptions = Configuration.Option("--interface", "DBus interfaces to include, optionally specify a name (e.g. 'org.freedesktop.NetworkManager.Device.Wired:WiredDevice')", CommandOptionType.MultipleValue);
            _files = AddFilesArgument();
        }

        public override void Execute()
        {
            if (!_serviceOption.HasValue() && _files.Values == null)
            {
                throw new ArgumentException("Service option or files argument must be specified.", "service");
            }
            IEnumerable<string> skipInterfaces = new [] { "org.freedesktop.DBus.Introspectable", "org.freedesktop.DBus.Peer", "org.freedesktop.DBus.Properties" };
            if (_skipOptions.HasValue())
            {
                skipInterfaces = skipInterfaces.Concat(_skipOptions.Values);
            }
            Dictionary<string, string> interfaces = null;
            if (_interfaceOptions.HasValue())
            {
                interfaces = new Dictionary<string, string>();
                foreach (var interf in _interfaceOptions.Values)
                {
                    if (interf.Contains(':'))
                    {
                        var split = interf.Split(new[] { ':' });
                        interfaces.Add(split[0], split[1]);
                    }
                    else
                    {
                        interfaces.Add(interf, null);
                    }
                }
            }
            var address = ParseBusAddress(_busOption);
            var service = _serviceOption.Value();
            string ns = "DBus";
            if (service != null)
            {
                var serviceSplit = service.Split(new [] { '.' });
                ns = _namespaceOption.Value() ?? $"{serviceSplit[serviceSplit.Length - 1]}.DBus";
            }
            var codeGenArguments = new CodeGenArguments
            {
                Namespace = ns,
                Service = service,
                Path = _pathOption.HasValue() ? _pathOption.Value() : "/",
                Address = address,
                Recurse = !_norecurseOption.HasValue(),
                OutputFileName = _catOption.HasValue() ? null : _outputOption.Value() ?? $"{ns}.cs",
                SkipInterfaces = skipInterfaces,
                Interfaces = interfaces,
                Files = _files.Values
            };
            GenerateCodeAsync(codeGenArguments).Wait();
        }

        class Visitor
        {
            private CodeGenArguments _arguments;
            private Dictionary<string, InterfaceDescription> _introspections;
            private HashSet<string> _names;

            public Visitor(CodeGenArguments codeGenArguments)
            {
                _introspections = new Dictionary<string, InterfaceDescription>();
                _names = new HashSet<string>();
                _arguments = codeGenArguments;
            }

            public bool VisitNode(string path, XElement nodeXml)
            {
                foreach (var interfaceXml in nodeXml.Elements("interface"))
                {
                    string fullName = interfaceXml.Attribute("name").Value;
                    if (_introspections.ContainsKey(fullName) || _arguments.SkipInterfaces.Contains(fullName))
                    {
                        continue;
                    }
                    string proposedName = null;
                    if (_arguments.Interfaces != null)
                    {
                        if (!_arguments.Interfaces.TryGetValue(fullName, out proposedName))
                        {
                            continue;
                        }
                    }
                    if (proposedName == null)
                    {
                        var split = fullName.Split(new[] { '.' });
                        var name = Generator.Prettify(split[split.Length - 1]);
                        proposedName = name;
                        int index = 0;
                        while (_names.Contains(proposedName))
                        {
                            proposedName = $"{name}{index}";
                            index++;
                        }
                    }
                    _names.Add(proposedName);
                    _introspections.Add(fullName, new InterfaceDescription { InterfaceXml = interfaceXml, Name = proposedName });
                    if (_arguments.Interfaces != null)
                    {
                        _arguments.Interfaces.Remove(fullName);
                        return _arguments.Interfaces.Count != 0;
                    }
                }
                return true;
            }

            public List<InterfaceDescription> Descriptions => _introspections.Values.ToList();
        }

        private async Task GenerateCodeAsync(CodeGenArguments codeGenArguments)
        {
            var visitor = new Visitor(codeGenArguments);
            if (codeGenArguments.Service != null)
            {
                using (var connection = new Connection(codeGenArguments.Address))
                {
                    await connection.ConnectAsync();
                    await NodeVisitor.VisitAsync(connection, codeGenArguments.Service, codeGenArguments.Path, codeGenArguments.Recurse, visitor.VisitNode);
                }
            }
            if (codeGenArguments.Files != null)
            {
                foreach (var file in codeGenArguments.Files)
                {
                    await NodeVisitor.VisitAsync(file, visitor.VisitNode);
                }
            }
            var descriptions = visitor.Descriptions;

            var generator = new Generator(
                new GeneratorSettings {
                    Namespace = codeGenArguments.Namespace
                });
            var code = generator.Generate(descriptions);

            if (codeGenArguments.OutputFileName != null)
            {
                File.WriteAllText(codeGenArguments.OutputFileName, code);
                Console.WriteLine($"Generated: {Path.GetFullPath(codeGenArguments.OutputFileName)}");
            }
            else
            {
                System.Console.WriteLine(code);
            }
        }

        class CodeGenArguments
        {
            public string Namespace { get; set; }
            public string Service { get; set; }
            public string Path { get; set; }
            public string Address { get; set; }
            public bool Recurse { get; set; }
            public string OutputFileName { get; set; }
            public IEnumerable<string> SkipInterfaces { get; set; }
            public Dictionary<string, string> Interfaces { get; set; }
            public List<string> Files { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        }

        public override void Execute()
        {
            if (!_serviceOption.HasValue())
            {
                throw new ArgumentNullException("Service argument is required.", "service");
            }
            IEnumerable<string> skipInterfaces = new [] { "org.freedesktop.DBus.Introspectable", "org.freedesktop.DBus.Peer", "org.freedesktop.DBus.ObjectManager", "org.freedesktop.DBus.Properties" };
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
            var serviceSplit = service.Split(new [] { '.' });
            var ns = _namespaceOption.Value() ?? $"{serviceSplit[serviceSplit.Length - 1]}.DBus";
            var codeGenArguments = new CodeGenArguments
            {
                Namespace = ns,
                Service = service,
                Path = _pathOption.HasValue() ? _pathOption.Value() : "/",
                Address = address,
                Recurse = !_norecurseOption.HasValue(),
                OutputFileName = _catOption.HasValue() ? null : _outputOption.Value() ?? $"{ns}.cs",
                SkipInterfaces = skipInterfaces,
                Interfaces = interfaces
            };
            GenerateCodeAsync(codeGenArguments).Wait();
        }

        private async static Task GenerateCodeAsync(CodeGenArguments codeGenArguments)
        {
            var fetcher = new IntrospectionsFetcher(
                new IntrospectionsFetcherSettings {
                    Service = codeGenArguments.Service,
                    Path = codeGenArguments.Path,
                    Address = codeGenArguments.Address,
                    Recurse = codeGenArguments.Recurse,
                    SkipInterfaces = codeGenArguments.SkipInterfaces,
                    Interfaces = codeGenArguments.Interfaces
                });
            var introspections = await fetcher.GetIntrospectionsAsync();

            var generator = new Generator(
                new GeneratorSettings {
                    Namespace = codeGenArguments.Namespace
                });
            var code = generator.Generate(introspections);

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
        }
    }
}
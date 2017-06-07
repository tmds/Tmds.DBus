using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.CodeGen
{
    class CodeGenArguments
    {
        public string Namespace { get; set; } = "DBus";
        public string Service { get; set; }
        public string Path { get; set; }
        public string Address { get; set; }
        public bool Recurse { get; set; }
        public string OutputFileName { get; set; }
        public IEnumerable<string> SkipInterfaces { get; set; }
        public Dictionary<string, string> Interfaces { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var commandLineApp = new CommandLineApplication();
            commandLineApp.Name = "dbus-codegen";
            commandLineApp.HelpOption("-?|-h|--help");
            var serviceOption = commandLineApp.Option("--service", "DBus service", CommandOptionType.SingleValue);
            var addressOption = commandLineApp.Option("--daemon", "Address of DBus daemon. 'session'/'system'/<address> (default: session)", CommandOptionType.SingleValue);
            var pathOption = commandLineApp.Option("--path", "DBus object path (default: /)", CommandOptionType.SingleValue);
            var norecurseOption = commandLineApp.Option("--no-recurse", "Don't visit child nodes of path", CommandOptionType.NoValue);
            var namespaceOption = commandLineApp.Option("--namespace", "C# namespace (default: <service>)", CommandOptionType.SingleValue);
            var outputOption = commandLineApp.Option("--output", "File to write (default: <namespace>.cs)", CommandOptionType.SingleValue);
            var skipOptions = commandLineApp.Option("--skip", "DBus interfaces to skip", CommandOptionType.MultipleValue);
            var interfaceOptions = commandLineApp.Option("--interface", "DBus interfaces to include, optionally specify a name (e.g. 'org.freedesktop.NetworkManager.Device.Wired:WiredDevice')", CommandOptionType.MultipleValue);
            commandLineApp.Execute(args);
            if (commandLineApp.IsShowingInformation)
            {
                return;
            }
            if (!serviceOption.HasValue())
            {
                throw new ArgumentException("Service argument is required.", "service");
            }
            IEnumerable<string> skipInterfaces = new [] { "org.freedesktop.DBus.Introspectable", "org.freedesktop.DBus.Peer", "org.freedesktop.DBus.ObjectManager", "org.freedesktop.DBus.Properties" };
            if (skipOptions.HasValue())
            {
                skipInterfaces = skipInterfaces.Concat(skipOptions.Values);
            }
            Dictionary<string, string> interfaces = null;
            if (interfaceOptions.HasValue())
            {
                interfaces = new Dictionary<string, string>();
                foreach (var interf in interfaceOptions.Values)
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
            var address = Address.Session;
            if (addressOption.HasValue())
            {
                if (addressOption.Value() == "system")
                {
                    address = Address.System;
                }
                else if (addressOption.Value() == "session")
                {
                    address = Address.Session;
                }
                else
                {
                    address = addressOption.Value();
                }
            }
            var service = serviceOption.Value();
            var serviceSplit = service.Split(new [] { '.' });
            var ns = namespaceOption.Value() ?? $"{serviceSplit[serviceSplit.Length - 1]}.DBus";
            var codeGenArguments = new CodeGenArguments
            {
                Namespace = ns,
                Service = service,
                Path = pathOption.HasValue() ? pathOption.Value() : "/",
                Address = address,
                Recurse = !norecurseOption.HasValue(),
                OutputFileName = outputOption.Value() ?? $"{ns}.cs",
                SkipInterfaces = skipInterfaces,
                Interfaces = interfaces
            };
            GenerateCodeAsync(codeGenArguments).Wait();
            System.Console.WriteLine($"Generated: {Path.GetFullPath(codeGenArguments.OutputFileName)}");
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

            File.WriteAllText(codeGenArguments.OutputFileName, code);
        }
    }
}
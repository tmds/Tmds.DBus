using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Tool
{
    class ListCommand : Command
    {
        public ListCommand() : base("list")
        {
            Description = "List DBus objects, interfaces, or services";

            Option<string?> serviceOption = CommandHelpers.CreateServiceOption();
            Option<string> busOption = CommandHelpers.CreateBusOption();
            Option<string> pathOption = CommandHelpers.CreatePathOption();
            Option<bool> norecurseOption = CommandHelpers.CreateNoRecurseOption();
            Argument<string> typeArgument = new Argument<string>("type");
            typeArgument.Description = "Type to list. 'objects'/'interfaces'/services'/'activatable-services'";
            Argument<string[]?> filesArgument = CommandHelpers.CreateFilesArgument();

            Add(serviceOption);
            Add(busOption);
            Add(pathOption);
            Add(norecurseOption);
            Add(typeArgument);
            Add(filesArgument);

            this.SetAction((parseResult) =>
            {
                string? service = parseResult.GetValue(serviceOption);
                string bus = parseResult.GetValue(busOption)!;
                string path = parseResult.GetValue(pathOption)!;
                bool noRecurse = parseResult.GetValue(norecurseOption);
                string? type = parseResult.GetValue(typeArgument);
                string[]? files = parseResult.GetValue(filesArgument);

                string address = CommandHelpers.ParseBusAddress(bus);
                if (type == null)
                {
                    Console.Error.WriteLine("Type argument is required.");
                    return 1;
                }
                if (type == "services")
                {
                    ListServicesAsync(address).Wait();
                }
                else if (type == "activatable-services")
                {
                    ListActivatableServicesAsync(address).Wait();
                }
                else if (type == "objects")
                {
                    if (string.IsNullOrEmpty(service))
                    {
                        Console.Error.WriteLine("Service option must be specified for listing objects.");
                        return 1;
                    }
                    bool recurse = !noRecurse;
                    ListObjectsAsync(address, service, path, recurse).Wait();
                }
                else if (type == "interfaces")
                {
                    if (string.IsNullOrEmpty(service) && (files == null || files.Length == 0))
                    {
                        Console.Error.WriteLine("Service option or files argument must be specified for listing interfaces.");
                        return 1;
                    }
                    bool recurse = !noRecurse;
                    ListInterfacesAsync(address, service, path, recurse, files).Wait();
                }
                else
                {
                    Console.Error.WriteLine($"Unknown type: {type}");
                    return 1;
                }
                return 0;
            });
        }

        class DBusObject
        {
            public required string Path { get; init; }
            public required List<string> Interfaces { get; init; }
        }

        class ObjectsVisitor
        {
            private static readonly IEnumerable<string> s_skipInterfaces = new[] { "org.freedesktop.DBus.Introspectable", "org.freedesktop.DBus.Peer", "org.freedesktop.DBus.Properties" };
            public List<DBusObject> Objects { private set; get; }

            public ObjectsVisitor()
            {
                Objects = new List<DBusObject>();
            }

            public bool Visit(string path, XElement nodeXml)
            {
                IEnumerable<string> interfaces = nodeXml.Elements("interface")
                    .Select(i => i.Attribute("name").Value)
                    .Where(i => !s_skipInterfaces.Contains(i));
                if (interfaces.Any())
                {
                    DBusObject o = new DBusObject()
                    {
                        Path = path,
                        Interfaces = interfaces.OrderBy(s => s).ToList()
                    };
                    Objects.Add(o);
                }
                return true;
            }
        }

        private static async Task ListObjectsAsync(string address, string service, string path, bool recurse)
        {
            ObjectsVisitor visitor = new ObjectsVisitor();
            using (DBusConnection connection = new DBusConnection(address))
            {
                await connection.ConnectAsync();
                await NodeVisitor.VisitAsync(connection, service, path, recurse, visitor.Visit);
            }
            foreach (DBusObject o in visitor.Objects.OrderBy(o => o.Path))
            {
                Console.WriteLine($"{o.Path} : {string.Join(" ", o.Interfaces)}");
            }
        }

        class InterfacesVisitor
        {
            private static readonly IEnumerable<string> s_skipInterfaces = new[] { "org.freedesktop.DBus.Introspectable", "org.freedesktop.DBus.Peer", "org.freedesktop.DBus.Properties" };
            public HashSet<string> Interfaces { private set; get; }

            public InterfacesVisitor()
            {
                Interfaces = new HashSet<string>();
            }

            public bool Visit(string path, XElement nodeXml)
            {
                IEnumerable<string> interfaces = nodeXml.Elements("interface")
                    .Select(i => i.Attribute("name").Value)
                    .Where(i => !s_skipInterfaces.Contains(i));
                foreach (string interf in interfaces)
                {
                    Interfaces.Add(interf);
                }
                return true;
            }
        }

        private static async Task ListInterfacesAsync(string address, string service, string path, bool recurse, string[]? files)
        {
            InterfacesVisitor visitor = new InterfacesVisitor();
            if (service != null)
            {
                using (DBusConnection connection = new DBusConnection(address))
                {
                    await connection.ConnectAsync();
                    await NodeVisitor.VisitAsync(connection, service, path, recurse, visitor.Visit);
                }
            }
            if (files != null)
            {
                foreach (string file in files)
                {
                    await NodeVisitor.VisitAsync(file, visitor.Visit);
                }
            }
            foreach (string interf in visitor.Interfaces.OrderBy(i => i))
            {
                Console.WriteLine(interf);
            }
        }

        public static async Task ListServicesAsync(string address)
        {
            using (DBusConnection connection = new DBusConnection(address))
            {
                await connection.ConnectAsync();
                string[] services = await connection.ListServicesAsync();
                Array.Sort(services);
                foreach (string service in services)
                {
                    if (!service.StartsWith(":", StringComparison.Ordinal))
                    {
                        Console.WriteLine(service);
                    }
                }
            }
        }

        public static async Task ListActivatableServicesAsync(string address)
        {
            using (DBusConnection connection = new DBusConnection(address))
            {
                await connection.ConnectAsync();
                string[] services = await connection.ListActivatableServicesAsync();
                Array.Sort(services);
                foreach (string service in services)
                {
                    Console.WriteLine(service);
                }
            }
        }
    }
}

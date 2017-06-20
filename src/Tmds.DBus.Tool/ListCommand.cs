using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    class ListCommand : Command
    {
        CommandOption _serviceOption;
        CommandOption _busOption;
        CommandOption _pathOption;
        CommandOption _norecurseOption;
        CommandArgument _typeArgument;

        public ListCommand(CommandLineApplication parent) :
            base("list", parent)
        {}

        public override void Configure()
        {
            _serviceOption = AddServiceOption();
            _busOption = AddBusOption();
            _pathOption = AddPathOption();
            _norecurseOption = AddNoRecurseOption();
            _typeArgument = Configuration.Argument("type", "Type to list. 'objects'/services'/'activatable-services'");
        }

        public override void Execute()
        {
            var address = ParseBusAddress(_busOption);
            if (_typeArgument.Value == null)
            {
                throw new ArgumentNullException("Type argument is required.", "type");
            }
            if (_typeArgument.Value == "services")
            {
                ListServicesAsync(address).Wait();
            }
            else if (_typeArgument.Value == "activatable-services")
            {
                ListActivatableServicesAsync(address).Wait();
            }
            else if (_typeArgument.Value == "objects")
            {
                if (!_serviceOption.HasValue())
                {
                    throw new ArgumentException("Service option must be specified for listing objects.", "service");
                }
                string service = _serviceOption.Value();
                bool recurse = !_norecurseOption.HasValue();
                string path = _pathOption.HasValue() ? _pathOption.Value() : "/";
                ListObjectsAsync(address, service, path, recurse).Wait();
            }
            else
            {
                throw new ArgumentException("Unknown type", "type");
            }
        }

        class DBusObject
        {
            public string Path { get; set; }
            public List<string> Interfaces { get; set; }
        }

        class Visitor
        {
            private static readonly IEnumerable<string> s_skipInterfaces = new[] { "org.freedesktop.DBus.Introspectable", "org.freedesktop.DBus.Peer", "org.freedesktop.DBus.Properties" };
            public List<DBusObject> Objects { private set; get; }

            public Visitor()
            {
                Objects = new List<DBusObject>();
            }

            public bool Visit(string path, XElement nodeXml)
            {
                var interfaces = nodeXml.Elements("interface").Where(i => !s_skipInterfaces.Contains(i.Attribute("name").Value));
                if (interfaces.Any())
                {
                    var o = new DBusObject()
                    {
                        Path = path,
                        Interfaces = interfaces.Select(interfaceXml => interfaceXml.Attribute("name").Value).OrderBy(s => s).ToList()
                    };
                    Objects.Add(o);
                }
                return true;
            }
        }

        private async Task ListObjectsAsync(string address, string service, string path, bool recurse)
        {
            var visitor = new Visitor();
            using (var connection = new Connection(address))
            {
                await connection.ConnectAsync();
                await NodeVisitor.VisitAsync(connection, service, path, recurse, visitor.Visit);
            }
            foreach (var o in visitor.Objects.OrderBy(o => o.Path))
            {
                Console.WriteLine($"{o.Path} : {string.Join(" ", o.Interfaces)}");
            }
        }

        public async Task ListServicesAsync(string address)
        {
            using (var connection = new Connection(address))
            {
                await connection.ConnectAsync();
                var services = await connection.ListServicesAsync();
                Array.Sort(services);
                foreach (var service in services)
                {
                    if (!service.StartsWith(":"))
                    {
                        Console.WriteLine(service);
                    }
                }
            }
        }

        public async Task ListActivatableServicesAsync(string address)
        {
            using (var connection = new Connection(address))
            {
                await connection.ConnectAsync();
                var services = await connection.ListActivatableServicesAsync();
                Array.Sort(services);
                foreach (var service in services)
                {
                    Console.WriteLine(service);
                }
            }
        }
    }
}
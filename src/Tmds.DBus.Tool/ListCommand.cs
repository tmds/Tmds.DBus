using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    class ListCommand : Command
    {
        CommandOption _serviceOption;
        CommandOption _daemonOption;
        CommandOption _pathOption;
        CommandOption _norecurseOption;
        CommandArgument _typeArgument;

        public ListCommand(CommandLineApplication parent) :
            base("list", parent)
        {}

        public override void Configure()
        {
            //_serviceOption = AddServiceOption();
            _daemonOption = AddDaemonOption();
            //_pathOption = AddPathOption();
            //_norecurseOption = AddNoRecurseOption();
            _typeArgument = Configuration.Argument("type", "Type to list. 'services'/'activatable-services'");
        }

        public override void Execute()
        {
            var address = ParseDaemonAddress(_daemonOption);
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
            else
            {
                throw new ArgumentException("Unknown type", "type");
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
using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    abstract class Command
    {
        protected CommandLineApplication Configuration { get; private set; }
        protected Command(string name, CommandLineApplication parent)
        {
            parent.Command(name, configuration =>
            {
                Configuration = configuration;
                configuration.HelpOption(Command.HelpTemplate);
                Configure();
                configuration.OnExecute(() =>
                {
                    Execute();
                    return 0;
                });
            });
        }

        public const string HelpTemplate = "-h|--help";
        public string Name { get; protected set; }
        public abstract void Configure();
        public abstract void Execute();

        internal CommandOption AddServiceOption()
            => Configuration.Option("--service", "DBus service", CommandOptionType.SingleValue);

        internal CommandOption AddBusOption()
            => Configuration.Option("--bus", "Address of bus. 'session'/'system'/<address> (default: session)", CommandOptionType.SingleValue);

        internal CommandOption AddPathOption()
            => Configuration.Option("--path", "DBus object path (default: /)", CommandOptionType.SingleValue);

        internal CommandOption AddNoRecurseOption()
            => Configuration.Option("--no-recurse", "Don't visit child nodes of path", CommandOptionType.NoValue);

        internal string ParseBusAddress(CommandOption addressOption)
        {
            if (addressOption.HasValue())
            {
                if (addressOption.Value() == "system")
                {
                    return Address.System;
                }
                else if (addressOption.Value() == "session")
                {
                    return Address.Session;
                }
                else
                {
                    return addressOption.Value();
                }
            }
            return Address.Session;
        }
    }
}
using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    abstract class Command
    {
        public const string HelpTemplate = "-h|--help";
        public string Name { get; protected set; }
        public abstract void Configure(CommandLineApplication commandLineApplication);
        public abstract void Execute();
    }
}
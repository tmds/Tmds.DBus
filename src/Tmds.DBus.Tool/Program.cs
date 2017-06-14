using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var commandLineApp = new CommandLineApplication();
            commandLineApp.Name = "dotnet-dbus";
            commandLineApp.HelpOption(Command.HelpTemplate);
            new CodeGenCommand(commandLineApp);
            new ListCommand(commandLineApp);
            commandLineApp.OnExecute(() => { commandLineApp.ShowHelp(); return 0; });
            commandLineApp.Execute(args);
        }
    }
}
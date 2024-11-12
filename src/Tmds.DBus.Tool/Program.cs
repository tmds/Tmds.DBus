using McMaster.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var commandLineApp = new CommandLineApplication();
            commandLineApp.Name = "dotnet-dbus";
            commandLineApp.HelpOption(Command.HelpTemplate);
            new CodeGenCommand(commandLineApp);
            new ListCommand(commandLineApp);
            new MonitorCommand(commandLineApp);
            commandLineApp.OnExecute(() => { commandLineApp.ShowHelp(); return 0; });
            return commandLineApp.Execute(args);
        }
    }
}
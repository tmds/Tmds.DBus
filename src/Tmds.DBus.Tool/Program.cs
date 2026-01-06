using System.CommandLine;

namespace Tmds.DBus.Tool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand();
            rootCommand.Description = "dotnet-dbus";

            rootCommand.Add(new CodeGenCommand());
            rootCommand.Add(new ListCommand());
            rootCommand.Add(new MonitorCommand());

            return rootCommand.Parse(args).Invoke();
        }
    }
}

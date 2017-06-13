using Microsoft.Extensions.CommandLineUtils;

namespace Tmds.DBus.Tool
{
    static class CommandLineApplicationExtensions
    {
        public static void AddCommand(this CommandLineApplication app, Command command)
        {
            app.Command(command.Name, configuration =>
            {
                configuration.HelpOption(Command.HelpTemplate);
                command.Configure();
                configuration.OnExecute(() =>
                    {
                        command.Execute();
                        return 0;
                    });
            });
        }
    }
}
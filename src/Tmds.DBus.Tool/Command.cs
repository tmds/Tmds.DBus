using System.CommandLine;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Tool
{
    static class CommandHelpers
    {
        internal static Option<string?> CreateServiceOption()
        {
            Option<string?> option = new Option<string?>("--service");
            option.Description = "DBus service";
            return option;
        }

        internal static Option<string> CreateBusOption()
        {
            Option<string> option = new Option<string>("--bus");
            option.Description = "Address of bus. 'session'/'system'/<address>";
            option.DefaultValueFactory = _ => "session";
            return option;
        }

        internal static Option<string> CreatePathOption()
        {
            Option<string> option = new Option<string>("--path");
            option.Description = "DBus object path";
            option.DefaultValueFactory = _ => "/";
            return option;
        }

        internal static Option<bool> CreateNoRecurseOption()
        {
            Option<bool> option = new Option<bool>("--no-recurse");
            option.Description = "Don't visit child nodes of path";
            option.DefaultValueFactory = _ => false;
            return option;
        }

        internal static Argument<string[]?> CreateFilesArgument()
        {
            Argument<string[]?> argument = new Argument<string[]?>("files");
            argument.Description = "Interface xml files";
            argument.Arity = ArgumentArity.ZeroOrMore;
            return argument;
        }

        internal static string ParseBusAddress(string addressOption)
        {
            if (addressOption == "system")
            {
                return Address.System;
            }
            else if (addressOption == "session")
            {
                return Address.Session;
            }
            else
            {
                return addressOption;
            }
        }
    }
}

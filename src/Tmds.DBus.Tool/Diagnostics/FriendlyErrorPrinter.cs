#nullable enable
using System;

namespace Tmds.DBus.Tool.Diagnostics;

internal static class FriendlyErrorPrinter
{
    public static void WriteToConsole(Exception e)
    {
        switch (e)
        {
            case GenerationException ge:
                WriteToConsole(ge);
                break;
            case AggregateException { InnerExceptions.Count: 1 } ae when ae.InnerExceptions[0] is GenerationException ge:
                WriteToConsole(ge);
                break;
            default:
                Console.WriteLine("Failed to generate C# code.");
                Console.WriteLine(e.ToString());
                break;
        }
    }

    private static void WriteToConsole(GenerationException ge)
    {
        Console.WriteLine("There was a problem when generating C# code from a D-Bus interface definition.");
        Console.WriteLine($"Reason: {ge.Message}");
        if (ge.InterfaceDescription is {} desc)
        {
            Console.WriteLine($"D-Bus object path: {desc.Path}");
            Console.WriteLine($"D-Bus interface name: {desc.FullName}");
            Console.WriteLine($"XML location: {ge.FaultyElement.GetAbsoluteXPath()}");
            Console.WriteLine();
            XmlErrorHighlighter.WriteToConsole(desc.FullXml, ge.FaultyElement, ConsoleColor.Red);
            Console.WriteLine();
        }
        Console.WriteLine("If you think this is caused by a bug in the codegen tool then please report issue to https://github.com/tmds/Tmds.DBus/issues.");
        Console.WriteLine("Alternatively if the problem is caused by a malformed introspection XML then the following steps could be taken:");
        Console.WriteLine("1. Report the issue to the developer/organization maintaining that service.");
        Console.WriteLine("2. Use the codegen --skip option to skip the problematic interface.");
        Console.WriteLine("3. You could also copy the faulty introspection XML to a file, corrected it and then instead specify it as a file argument to codegen.");
    }
}
using System;
using System.IO;

namespace Tmds.DBus.Tool.Tests;

public class ConsoleSuppressor : IDisposable
{
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;
    private readonly TextWriter _temporaryTextWriter;

    public ConsoleSuppressor()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;
        _temporaryTextWriter = new StringWriter();
        Console.SetOut(_temporaryTextWriter);
        Console.SetError(_temporaryTextWriter);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _temporaryTextWriter.Dispose();
    }
}
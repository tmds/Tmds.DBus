#nullable enable
using System;
using System.Xml.Linq;

namespace Tmds.DBus.Tool.Diagnostics;

public class GenerationException : Exception
{
    public XObject FaultyElement { get; }
    public InterfaceDescription? InterfaceDescription { get; set; }

    public GenerationException(string message, XObject faultyElement, InterfaceDescription? interfaceDescription = null, Exception? innerException = null) : base(message, innerException)
    {
        FaultyElement = faultyElement;
        InterfaceDescription = interfaceDescription;
    }

    public void Inform(InterfaceDescription interfaceDescription)
    {
        InterfaceDescription ??= interfaceDescription;
    }
}
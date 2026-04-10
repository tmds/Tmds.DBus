using System;
using System.Collections.Generic;

namespace Tmds.DBus.Tool
{
    interface IGenerator
    {
        public string Generate(string ns, IEnumerable<InterfaceDescription> interfaceDescriptions, bool generateProxies, bool generateHandlers);
    }

    class InterfaceGenerationException : Exception
    {
        public InterfaceDescription InterfaceDescription { get; }

        public InterfaceGenerationException(InterfaceDescription interfaceDescription, Exception innerException)
            : base($"Exception occurred while generating code for the '{interfaceDescription.Name}' interface.", innerException)
        {
            InterfaceDescription = interfaceDescription;
        }
    }
}
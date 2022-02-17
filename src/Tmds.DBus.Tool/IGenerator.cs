using System.Collections.Generic;

namespace Tmds.DBus.Tool
{
    interface IGenerator
    {
        string Generate(IEnumerable<InterfaceDescription> interfaceDescriptions);
    }
}
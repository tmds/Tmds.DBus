using System.Collections.Generic;

namespace Tmds.DBus.Tool
{
    public interface IGenerator
    {
        string Generate(IEnumerable<InterfaceDescription> interfaceDescriptions);
    }
}
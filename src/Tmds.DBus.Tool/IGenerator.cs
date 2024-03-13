using System.Collections.Generic;

namespace Tmds.DBus.Tool
{
    interface IGenerator
    {
        public bool TryGenerate(IEnumerable<InterfaceDescription> interfaceDescriptions, out string sourceCode);
    }
}
using System.Xml.Linq;

namespace Tmds.DBus.CodeGen
{
    class InterfaceDescription
    {
        public XElement InterfaceXml { get; set; }
        public string   Name { get; set; }
    }
}
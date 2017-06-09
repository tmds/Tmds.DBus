using System.Xml.Linq;

namespace Tmds.DBus.Tool
{
    class InterfaceDescription
    {
        public XElement InterfaceXml { get; set; }
        public string   Name { get; set; }
    }
}
using System.Xml.Linq;

namespace Tmds.DBus.Tool
{
    class InterfaceDescription
    {
        public string Path { get; set; }
        public string FullXml { get; set; }
        public string FullName { get; set; }
        public XElement InterfaceXml { get; set; }
        public string   Name { get; set; }
    }
}
using System.IO;
using System.Xml.Linq;

namespace Tmds.DBus.CodeGen
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var filename = args.Length > 1 ? args[0] : "example.xml";
            var interfaceXml = XDocument.Load(File.OpenRead(filename)).Root;
            var generator = new Generator();
            var code = generator.Generate(new[] { interfaceXml });
            System.Console.WriteLine(code);
        }
    }
}
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Tool
{
    class NodeVisitor
    {
        public static Task VisitAsync(string filename, Func<string, string, XElement, bool> visit)
        {
            var doc = XDocument.Load(filename, LoadOptions.SetLineInfo);
            var nodeXml = doc.Root;

            var encoding = Encoding.GetEncoding(doc.Declaration?.Encoding ?? Encoding.UTF8.WebName);
            var fullXml = File.ReadAllText(filename, encoding);

            return GetInterfacesFromIntrospection(null, null, null, fullXml, nodeXml, false, visit);
        }

        public static Task VisitAsync(Connection connection, string service, string objectPath, bool recurse, Func<string, string, XElement, bool> visit)
        {
            return VisitAsyncInternal(connection, service, objectPath, recurse, visit);
        }

        private static async Task<bool> VisitAsyncInternal(Connection connection, string service, string objectPath, bool recurse, Func<string, string, XElement, bool> visit)
        {
            var xml = await connection.CallMethodAsync(CreateMessage(objectPath), (Message m, object s) => m.GetBodyReader().ReadString(), null);
            var nodeXml = XDocument.Parse(xml, LoadOptions.SetLineInfo).Root;

            return await GetInterfacesFromIntrospection(connection, service, objectPath, xml, nodeXml, recurse, visit);

            MessageBuffer CreateMessage(string path)
            {
                using var writer = connection.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    destination: service,
                    path: path,
                    @interface: "org.freedesktop.DBus.Introspectable",
                    member: "Introspect");
                return writer.CreateMessage();
            }
        }

        private static async Task<bool> GetInterfacesFromIntrospection(Connection connection, string service, string objectPath, string fullXml, XElement nodeXml, bool recurse, Func<string, string, XElement, bool> visit)
        {
            if (!visit(objectPath, fullXml, nodeXml))
            {
                return false;
            }
            if (recurse)
            {
                foreach (var childNodeXml in nodeXml.Elements("node"))
                {
                    objectPath = objectPath.Length == 1 ? string.Empty : objectPath;
                    if (childNodeXml.HasElements)
                    {
                        if (!await GetInterfacesFromIntrospection(connection, service, $"{objectPath}/{childNodeXml.Attribute("name").Value}", fullXml, childNodeXml, recurse, visit))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!await VisitAsyncInternal(connection, service, $"{objectPath}/{childNodeXml.Attribute("name").Value}", recurse, visit))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Tmds.DBus
{

    [DBusInterface("org.freedesktop.DBus.Introspectable")]
    public interface IIntrospectable : IDBusObject
    {
        Task<string> IntrospectAsync();
    }

    class NodeVisitor
    {
        public static Task VisitAsync(string filename, Func<XElement, bool> visit)
        {
            var xml = XDocument.Load(filename).Root;
            return GetInterfacesFromIntrospection(null, null, null, xml, false, visit);
        }

        public static Task VisitAsync(Connection connection, string service, string objectPath, bool recurse, Func<XElement, bool> visit)
        {
            return VisitAsyncInternal(connection, service, objectPath, recurse, visit);
        }

        private static async Task<bool> VisitAsyncInternal(Connection connection, string service, string objectPath, bool recurse, Func<XElement, bool> visit)
        {
            var introspectable = connection.CreateProxy<IIntrospectable>(service, objectPath);
            var xml = await introspectable.IntrospectAsync();
            var nodeXml = XDocument.Parse(xml).Root;
            return await GetInterfacesFromIntrospection(connection, service, objectPath, nodeXml, recurse, visit);
        }

        private static async Task<bool> GetInterfacesFromIntrospection(Connection connection, string service, string objectPath, XElement nodeXml, bool recurse, Func<XElement, bool> visit)
        {
            if (!visit(nodeXml))
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
                        if (!await GetInterfacesFromIntrospection(connection, service, $"{objectPath}/{childNodeXml.Attribute("name").Value}", childNodeXml, recurse, visit))
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Tmds.DBus.Tool
{

    [DBusInterface("org.freedesktop.DBus.Introspectable")]
    public interface IIntrospectable : IDBusObject
    {
        Task<string> IntrospectAsync();
    }

    class IntrospectionsFetcherSettings
    {
        public string Service { get; set; }
        public string Path { get; set; }
        public string Address { get; set; }
        public bool Recurse { get; set; }
        public IEnumerable<string> SkipInterfaces { get; set; }
        public Dictionary<string, string> Interfaces { get; set; }
    }

    class IntrospectionsFetcher
    {
        private readonly IntrospectionsFetcherSettings _settings;
        private Dictionary<string, InterfaceDescription> _introspections;
        private Dictionary<string, string> _interfaceNames;
        private HashSet<string> _names;
        private Connection _connection;

        public IntrospectionsFetcher(IntrospectionsFetcherSettings settings)
        {
            _settings = settings;
        }

        public async Task<List<InterfaceDescription>> GetIntrospectionsAsync()
        {
            _introspections = new Dictionary<string, InterfaceDescription>();
            _names = new HashSet<string>();
            _interfaceNames = _settings.Interfaces?.ToDictionary(e => e.Key, e => e.Value);
            foreach (var skipInterface in _settings.SkipInterfaces)
            {
                _introspections.Add(skipInterface, null);
            }

            using (_connection = new Connection(_settings.Address))
            {
                await _connection.ConnectAsync();
                await GetInterfacesFromIntrospection(_settings.Path);
            }

            foreach (var skipInterface in _settings.SkipInterfaces)
            {
                _introspections.Remove(skipInterface);
            }

            return _introspections.Values.ToList();
        }

        private async Task GetInterfacesFromIntrospection(string objectPath)
        {
            var introspectable = _connection.CreateProxy<IIntrospectable>(_settings.Service, objectPath);
            var xml = await introspectable.IntrospectAsync();
            var nodeXml = XDocument.Parse(xml).Root;
            await GetInterfacesFromIntrospection(objectPath, nodeXml);
        }

        private async Task GetInterfacesFromIntrospection(string objectPath, XElement nodeXml)
        {
            foreach (var interfaceXml in nodeXml.Elements("interface"))
            {
                string fullName = interfaceXml.Attribute("name").Value;
                if (_introspections.ContainsKey(fullName))
                {
                    continue;
                }
                string proposedName = null;
                if (_interfaceNames != null)
                {
                    if (!_interfaceNames.TryGetValue(fullName, out proposedName))
                    {
                        continue;
                    }
                }
                if (proposedName == null)
                {
                    var split = fullName.Split(new[] { '.' });
                    var name = split[split.Length - 1];
                    proposedName = name;
                    int index = 0;
                    while (_names.Contains(proposedName))
                    {
                        proposedName = $"{name}{index}";
                        index++;
                    }
                }
                _names.Add(proposedName);
                _introspections.Add(fullName, new InterfaceDescription { InterfaceXml = interfaceXml, Name = proposedName });
                if (_interfaceNames != null)
                {
                    _interfaceNames.Remove(fullName);
                    if (_interfaceNames.Count == 0)
                    {
                        return;
                    }
                }
            }
            if (_settings.Recurse)
            {
                foreach (var childNodeXml in nodeXml.Elements("node"))
                {
                    objectPath = objectPath.Length == 1 ? string.Empty : objectPath;
                    if (childNodeXml.HasElements)
                    {
                        await GetInterfacesFromIntrospection($"{objectPath}/{childNodeXml.Attribute("name").Value}", childNodeXml);
                    }
                    else
                    {
                        await GetInterfacesFromIntrospection($"{objectPath}/{childNodeXml.Attribute("name").Value}");
                    }
                }
            }
        }
    }
}
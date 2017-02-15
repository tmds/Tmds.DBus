using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    public class PropertyObject : IPropertyObject
    {
        public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/propertyobject");
        private IDictionary<string, object> _properties;
        public PropertyObject(IDictionary<string, object> properties)
        {
            _properties = properties;
        }
        public ObjectPath ObjectPath
        {
            get
            {
                return Path;
            }
        }

        public event Action<PropertyChanges> OnPropertiesChanged;

        public Task<IDictionary<string, object>> GetAllAsync()
        {
            return Task.FromResult(_properties);
        }

        public Task<object> GetAsync(string prop)
        {
            return Task.FromResult(_properties[prop]);
        }

        public Task SetAsync(string prop, object val)
        {
            _properties[prop] = val;
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty(prop, val));
            return Task.CompletedTask;
        }

        public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return EventHandler.AddAsync(this, nameof(OnPropertiesChanged), handler);
        }
    }
}
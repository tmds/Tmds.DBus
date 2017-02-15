using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    [DBusInterface("tmds.dbus.tests.PropertyObject")]
    public interface IPropertyObject : IDBusObject
    {
        Task<IDictionary<string, object>> GetAllAsync();
        Task<object> GetAsync(string prop);
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }
}
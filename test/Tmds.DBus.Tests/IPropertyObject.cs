using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    [DBusInterface("tmds.dbus.tests.PropertyObject",
        GetPropertyMethod = "GetPropAsync",
        SetPropertyMethod = "SetPropAsync",
        GetAllPropertiesMethod = "GetAllPropsAsync",
        WatchPropertiesMethod = "WatchPropsAsync")]
    public interface IPropertyObject : IDBusObject
    {
        Task<IDictionary<string, object>> GetAllPropsAsync();
        Task<object> GetPropAsync(string prop);
        Task SetPropAsync(string prop, object val);
        Task<IDisposable> WatchPropsAsync(Action<PropertyChanges> handler);
    }

    [DBusInterface("tmds.dbus.tests.PropertyObject",
        GetPropertyMethod = "GetPropertyAsync",
        SetPropertyMethod = "SetPropertyAsync",
        GetAllPropertiesMethod = "GetAllPropertiesAsync",
        WatchPropertiesMethod = "WatchPropzAsync")]
    public interface IPropertyProxyObject : IDBusObject
    {
        Task<IDictionary<string, object>> GetAllPropertiesAsync();
        Task<object> GetPropertyAsync(string prop);
        Task SetPropertyAsync(string prop, object val);
        Task<IDisposable> WatchPropzAsync(Action<PropertyChanges> handler);
    }
}
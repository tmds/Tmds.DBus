using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    [DBusInterface("tmds.dbus.tests.PropertyObject")]
    public interface IPropertyObject : IDBusObject
    {
        Task<IDictionary<string, object>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<object> GetAsync(string prop, CancellationToken cancellationToken = default(CancellationToken));
        Task SetAsync(string prop, object val, CancellationToken cancellationToken = default(CancellationToken));
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, CancellationToken cancellationToken = default(CancellationToken));
    }
}
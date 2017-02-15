// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    public interface IConnection : IDisposable
    {
        string LocalName { get; }
        bool? RemoteIsBus { get; }
        Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);
        Task RegisterServiceAsync(string serviceName, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);
        Task<bool> UnregisterServiceAsync(string serviceName);
        Task<string[]> ListServicesAsync();
        T CreateProxy<T>(string serviceName, ObjectPath path);
        Task RegisterObjectAsync(IDBusObject o);
        Task RegisterObjectsAsync(IEnumerable<IDBusObject> objects);
        void UnregisterObject(ObjectPath path);
        void UnregisterObjects(IEnumerable<ObjectPath> paths);
        Task ConnectAsync(Action<Exception> onDisconnect = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<string[]> ListActivatableServicesAsync();
        Task<string> ResolveServiceOwnerAsync(string serviceName);
        Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler);
        Task<ServiceStartResult> ActivateServiceAsync(string serviceName);
        Task<bool> IsServiceActiveAsync(string serviceName);
    }
}

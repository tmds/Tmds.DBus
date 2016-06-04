// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    public interface IConnection : IDisposable
    {
        string LocalName { get; }
        bool? RemoteIsBus { get; }
        Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default , CancellationToken cancellationToken = default(CancellationToken));
        Task RegisterServiceAsync(string serviceName, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default, CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> UnregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
        Task<string[]> ListServicesAsync(CancellationToken cancellationToken = default(CancellationToken));
        T CreateProxy<T>(string serviceName, ObjectPath path);
        Task RegisterObjectAsync(IDBusObject o, CancellationToken cancellationToken = default(CancellationToken));
        void UnregisterObject(ObjectPath path);
        Task ConnectAsync(Action<Exception> onDisconnect = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<string[]> ListActivatableServicesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<string> ResolveServiceOwnerAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
        Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, CancellationToken cancellationToken = default(CancellationToken));
        Task<ServiceStartResult> ActivateServiceAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> IsServiceActiveAsync(string serviceName, CancellationToken cancellationToken = default(CancellationToken));
    }
}

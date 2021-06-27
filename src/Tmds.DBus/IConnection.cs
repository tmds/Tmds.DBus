// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Interface of the Connection class.
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary><see cref="Connection"/></summary>
        Task<ConnectionInfo> ConnectAsync();

        /// <summary><see cref="Connection"/></summary>
        T CreateProxy<T>(string serviceName, ObjectPath path);

        /// <summary><see cref="Connection"/></summary>
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        /// <summary><see cref="Connection"/></summary>
        Task<string[]> ListServicesAsync();

        /// <summary><see cref="Connection"/></summary>
        Task<string[]> ListActivatableServicesAsync();

        /// <summary><see cref="Connection"/></summary>
        Task<string> ResolveServiceOwnerAsync(string serviceName);

        /// <summary><see cref="Connection"/></summary>
        Task<IDisposable> ResolveServiceOwnerAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, Action<Exception> onError = null);

        /// <summary><see cref="Connection"/></summary>
        Task<ServiceStartResult> ActivateServiceAsync(string serviceName);

        /// <summary><see cref="Connection"/></summary>
        Task<bool> IsServiceActiveAsync(string serviceName);

        /// <summary><see cref="Connection"/></summary>
        Task QueueServiceRegistrationAsync(string serviceName, Action onAquired = null, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);

        /// <summary><see cref="Connection"/></summary>
        Task QueueServiceRegistrationAsync(string serviceName, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);

        /// <summary><see cref="Connection"/></summary>
        Task RegisterServiceAsync(string serviceName, Action onLost = null, ServiceRegistrationOptions options = ServiceRegistrationOptions.Default);

        /// <summary><see cref="Connection"/></summary>
        Task RegisterServiceAsync(string serviceName, ServiceRegistrationOptions options);

        /// <summary><see cref="Connection"/></summary>
        Task<bool> UnregisterServiceAsync(string serviceName);

        /// <summary><see cref="Connection"/></summary>
        Task RegisterObjectAsync(IDBusObject o);

        /// <summary><see cref="Connection"/></summary>
        Task RegisterObjectsAsync(IEnumerable<IDBusObject> objects);

        /// <summary><see cref="Connection"/></summary>
        void UnregisterObject(ObjectPath path);

        /// <summary><see cref="Connection"/></summary>
        void UnregisterObject(IDBusObject dbusObject);

        /// <summary><see cref="Connection"/></summary>
        void UnregisterObjects(IEnumerable<ObjectPath> paths);

        /// <summary><see cref="Connection"/></summary>
        void UnregisterObjects(IEnumerable<IDBusObject> objects);

        /// <summary><see cref="Connection"/></summary>
        Task<IDisposable> WatchSignalAsync<TSignalArgs>(SignalMatchRule rule, Action<(ObjectPath @object, TSignalArgs args)> handler, Action<Exception> onError = null);
    }
}

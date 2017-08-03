// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    interface IDBusConnection : IDisposable
    {
        Task<Message> CallMethodAsync(Message message);
        Task<IDisposable> WatchSignalAsync(ObjectPath path, string @interface, string signalName, SignalHandler handler);
        Task<IDisposable> WatchNameOwnerChangedAsync(string serviceName, Action<ServiceOwnerChangedEventArgs, Exception> handler);
        Task<RequestNameReply> RequestNameAsync(string name, RequestNameOptions options, Action onAquired, Action onLost, SynchronizationContext synchronzationContext);
        Task<ReleaseNameReply> ReleaseNameAsync(string name);
        void AddMethodHandlers(IEnumerable<KeyValuePair<ObjectPath, MethodHandler>> handlers);
        void RemoveMethodHandlers(IEnumerable<ObjectPath> paths);
        void EmitSignal(Message message);
        ConnectionInfo ConnectionInfo { get; }
        string[] GetChildNames(ObjectPath path);
        void Disconnect(bool dispose, Exception exception);
    }
}
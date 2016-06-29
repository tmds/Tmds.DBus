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
        Task<Message> CallMethodAsync(Message message, CancellationToken cancellationToken);
        Task<IDisposable> WatchSignalAsync(ObjectPath path, string @interface, string signalName, SignalHandler handler, CancellationToken cancellationToken);
        Task<IDisposable> WatchNameOwnerChangedAsync(string serviceName, Action<ServiceOwnerChangedEventArgs> handler, CancellationToken cancellationToken = default(CancellationToken));
        Task<RequestNameReply> RequestNameAsync(string name, RequestNameOptions options, Action onAquired, Action onLost, SynchronizationContext synchronzationContext, CancellationToken cancellationToken);
        Task<ReleaseNameReply> ReleaseNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken));
        void AddMethodHandlers(IEnumerable<KeyValuePair<ObjectPath, MethodHandler>> handlers);
        void RemoveMethodHandlers(IEnumerable<ObjectPath> paths);
        void EmitSignal(Message message);
        string LocalName { get; }
        bool? RemoteIsBus { get; }
    }
}
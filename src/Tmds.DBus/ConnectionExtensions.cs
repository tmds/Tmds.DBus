// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    public static class ConnectionExtensions
    {
        public static Task QueueServiceRegistrationAsync(this IConnection connection, string serviceName, ServiceRegistrationOptions options , CancellationToken cancellationToken = default(CancellationToken))
        {
            return connection.QueueServiceRegistrationAsync(serviceName, null, null, options, cancellationToken);
        }
        
        public static Task RegisterServiceAsync(this IConnection connection, string serviceName, ServiceRegistrationOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return connection.RegisterServiceAsync(serviceName, null, options, cancellationToken);
        }
        
        public static void UnregisterObject(this IConnection connection, IDBusObject dbusObject)
        {
            connection.UnregisterObject(dbusObject.ObjectPath);
        }
        
        public static Task ConnectAsync(this IConnection connection, CancellationToken cancellationToken)
        {
            return connection.ConnectAsync(null, cancellationToken);
        }
    }
}

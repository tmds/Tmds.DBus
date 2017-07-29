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
        public static Task QueueServiceRegistrationAsync(this IConnection connection, string serviceName, ServiceRegistrationOptions options)
        {
            return connection.QueueServiceRegistrationAsync(serviceName, null, null, options);
        }
        
        public static Task RegisterServiceAsync(this IConnection connection, string serviceName, ServiceRegistrationOptions options)
        {
            return connection.RegisterServiceAsync(serviceName, null, options);
        }
        
        public static void UnregisterObject(this IConnection connection, IDBusObject dbusObject)
        {
            connection.UnregisterObject(dbusObject.ObjectPath);
        }
    }
}

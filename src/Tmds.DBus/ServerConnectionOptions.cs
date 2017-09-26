// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Options that configure the behavior of a Connection for a D-Bus local server.
    /// </summary>
    public class ServerConnectionOptions : ConnectionOptions
    {
        private Connection _connection;

        /// <summary>
        /// Starts the server at the specified address.
        /// </summary>
        /// <param name="address">Address of the D-Bus peer.</param>
        /// <returns>
        /// Bound address.
        /// </returns>
        public Task<string> StartAsync(string address)
            => StartAsync(new ServerStartOptions { Address = address });

        /// <summary>
        /// Starts the server with the specified options.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>
        /// Bound address.
        /// </returns>
        public Task<string> StartAsync(ServerStartOptions options)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("Not attached to connection.");
            }

            return _connection.StartServerAsync(options.Address);
        }

        internal Connection Connection
        {
            get => _connection;
            set
            {
                if (_connection != null)
                {
                    throw new InvalidOperationException("Already attached to another connection.");
                }
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _connection = value;
            }
        }
    }
}
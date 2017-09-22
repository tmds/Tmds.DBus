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
        /// Starts the server.
        /// </summary>
        /// <param name="address">Address of the D-Bus peer.</param>
        public string Start(string address) // TODO: make this a type
        {
            if (_connection == null)
            {
                throw new InvalidOperationException();
            }

            return _connection.StartServer(address);
        }

        internal Connection Connection
        {
            get => _connection;
            set
            {
                if (_connection != null)
                {
                    throw new InvalidOperationException();
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
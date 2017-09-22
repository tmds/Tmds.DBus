// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Default ConnectionOptions
    /// </summary>
    public class DefaultConnectionOptions : ClientConnectionOptions
    {
        /// <summary>
        /// Creates a new Connection with a specific address.
        /// </summary>
        /// <param name="address">Address of the D-Bus peer.</param>
        public DefaultConnectionOptions(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            Address = address;
        }

        /// <summary>
        /// Address of D-Bus peer.
        /// </summary>
        public string Address { get; private set; }

        /// <summary><see cref="ConnectionOptions"/></summary>
        protected override internal Task<ClientSetupResult> SetupAsync()
        {
            return Task.FromResult(
                new ClientSetupResult
                {
                    ConnectionAddress = Address,
                    SupportsFdPassing = true,
                    UserId = Environment.UserId
                });
        }
    }
}
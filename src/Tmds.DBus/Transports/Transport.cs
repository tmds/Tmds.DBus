// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Transports
{
    internal abstract class Transport
    {
        private AddressEntry _addressEntry;

        protected Transport(AddressEntry entry)
        {
            _addressEntry = entry;
        }

        protected abstract Task<Stream> OpenAsync(AddressEntry entry, CancellationToken cancellationToken);

        public static Transport CreateTransport(AddressEntry entry)
        {
            switch (entry.Method)
            {
                case "tcp":
                    {
                        Transport transport = new TcpTransport(entry);
                        return transport;
                    }
                case "unix":
                    {
                        Transport transport = new UnixTransport(entry);
                        return transport;
                    }
            }
            throw new NotSupportedException("Transport method \"" + entry.Method + "\" not supported");
        }

        public Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            return OpenAsync(_addressEntry, cancellationToken);
        }

        protected async Task DoSaslAuthenticationAsync(Stream stream, CancellationToken cancellationToken)
        {
            var authentication = new SaslAuthentication(stream);
            var authenticationId = await authentication.AuthenticateAsync(cancellationToken);
            if (_addressEntry.Guid != Guid.Empty)
            {
                if (_addressEntry.Guid != authenticationId)
                {
                    throw new ConnectionException("Authentication failure: Unexpected GUID");
                }
            }
        }
    }
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Transports
{
    internal class TcpTransport : Transport
    {
        private static readonly byte[] _oneByteArray = new[] { (byte)0 };

        public TcpTransport(AddressEntry entry) :
            base(entry)
        {}

        protected override Task<Stream> OpenAsync (AddressEntry entry, CancellationToken cancellationToken)
        {
            string host, portStr, family;
            int port;

            if (!entry.Properties.TryGetValue ("host", out host))
                host = "localhost";

            if (!entry.Properties.TryGetValue ("port", out portStr))
                throw new FormatException ("No port specified");

            if (!Int32.TryParse (portStr, out port))
                throw new FormatException("Invalid port: \"" + port + "\"");

            if (!entry.Properties.TryGetValue ("family", out family))
                family = null;

            return OpenAsync (host, port, family, cancellationToken);
        }

        private async Task<Stream> OpenAsync (string host, int port, string family, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("host");
            }

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host);
            }
            catch (System.Exception e)
            {
                throw new ConnectionException($"No addresses for host '{host}'", e);
            }

            for (int i = 0; i < addresses.Length; i++)
            {
                var address = addresses[i];
                bool lastAddress = i == (addresses.Length - 1);
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                var registration = cancellationToken.Register(() => ((IDisposable)socket).Dispose());
                try
                {
                    await SocketUtils.ConnectAsync(socket, new IPEndPoint(address, port));
                    var stream = new NetworkStream(socket, true);
                    try
                    {
                        registration.Dispose();
                        await stream.WriteAsync(_oneByteArray, 0, 1, cancellationToken);
                        await DoSaslAuthenticationAsync(stream, cancellationToken);
                        return stream;
                    }
                    catch (Exception e)
                    {
                        stream.Dispose();
                        if (lastAddress)
                        {
                            throw new ConnectionException($"Unable to authenticate: {e.Message}", e);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    socket.Dispose();
                    if (lastAddress)
                    {
                        throw new ConnectionException($"Socket error: {e.Message}", e);
                    }
                }
                finally
                {
                    registration.Dispose();
                }
            }
            throw new ConnectionException($"No addresses for host '{host}'");
        }
    }
}

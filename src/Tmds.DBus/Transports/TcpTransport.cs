// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Transports
{
    internal class TcpTransport : Transport
    {
        private static readonly byte[] _oneByteArray = new[] { (byte)0 };
        private Stream _stream;

        private TcpTransport(ConnectionContext context) :
            base(context)
        {}

        public static new async Task<IMessageStream> OpenAsync(AddressEntry entry, ConnectionContext context, CancellationToken cancellationToken)
        {
            var messageStream = new TcpTransport(context);
            await messageStream.DoOpenAsync(entry, cancellationToken);
            return messageStream;
        }

        protected async Task DoOpenAsync (AddressEntry entry, CancellationToken cancellationToken)
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

            await OpenAsync (entry.Guid, host, port, family, cancellationToken);
        }

        private async Task OpenAsync (Guid guid, string host, int port, string family, CancellationToken cancellationToken)
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
                throw new ConnectException($"No addresses for host '{host}'", e);
            }

            for (int i = 0; i < addresses.Length; i++)
            {
                var address = addresses[i];
                bool lastAddress = i == (addresses.Length - 1);
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                cancellationToken.Register(() => socket.Dispose());
                try
                {
                    await socket.ConnectAsync(address, port);
                    _stream = new NetworkStream(socket, true);
                    await _stream.WriteAsync(_oneByteArray, 0, 1);
                    await DoSaslAuthenticationAsync(guid, transportSupportsUnixFdPassing: false);
                    return;
                }
                catch
                {
                    _stream?.Dispose();
                    socket.Dispose();
                    if (lastAddress)
                    {
                        throw;
                    }
                }
            }
            throw new ConnectException($"No addresses for host '{host}'");
        }

        protected override Task<int> ReadAsync(byte[] buffer, int offset, int count, List<UnixFd> fileDescriptors)
        {
            return _stream.ReadAsync(buffer, offset, count);
        }

        public override void Dispose()
        {
            _stream?.Dispose();
        }

        protected async override Task SendAsync(byte[] buffer, int offset, int count)
        {
            await _stream.WriteAsync(buffer, offset, count);
            await _stream.FlushAsync();
        }

        protected async override Task SendAsync(Message message)
        {
            var headerBytes = message.Header.ToArray();
            await _stream.WriteAsync(headerBytes, 0, headerBytes.Length, CancellationToken.None);

            if (message.Body != null && message.Body.Length != 0)
            {
                await _stream.WriteAsync(message.Body, 0, message.Body.Length, CancellationToken.None);
            }

            await _stream.FlushAsync(CancellationToken.None);
        }
    }
}

// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Transports
{
    class LocalServer : IDisposable
    {
        private readonly object _gate = new object();
        private readonly DBusConnection _connection;
        private IMessageStream[] _clientStreams;
        private Socket _serverSocket;

        public LocalServer(DBusConnection connection)
        {
            _connection = connection;
            _clientStreams = Array.Empty<IMessageStream>();
        }

        public async Task<string> StartAsync(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            var entries = AddressEntry.ParseEntries(address);
            if (entries.Length != 1)
            {
                throw new ArgumentException("Address must contain a single entry.", nameof(address));
            }
            var entry = entries[0];
            var endpoints = await entry.ResolveAsync(listen: true);
            if (endpoints.Length == 0)
            {
                throw new ArgumentException("Address does not resolve to an endpoint.", nameof(address));
            }
            var endpoint = endpoints[0];
            if (endpoint is IPEndPoint ipEndPoint)
            {
                _serverSocket = new Socket(ipEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            else if (endpoint is UnixDomainSocketEndPoint unixEndPoint)
            {
                _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                if (unixEndPoint.Path[0] == '\0')
                {
                    address = $"unix:abstract={unixEndPoint.Path.Substring(1)}";
                }
                else
                {
                    address = $"unix:path={unixEndPoint.Path}";
                }
            }
            _serverSocket.Bind(endpoint);
            _serverSocket.Listen(10);
            AcceptConnections();

            if (endpoint is IPEndPoint)
            {
                var boundEndPoint = _serverSocket.LocalEndPoint as IPEndPoint;
                address = $"tcp:host={boundEndPoint.Address},port={boundEndPoint.Port}";
            }
            return address;
        }

        public async void AcceptConnections()
        {
            while (true)
            {
                Socket clientSocket = null;
                try
                {
                    try
                    {
                        clientSocket = await _serverSocket.AcceptAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    var clientStream = await Transport.AcceptAsync(clientSocket,
                        supportsFdPassing: _serverSocket.AddressFamily == AddressFamily.Unix);
                    if (clientStream == null) // TODO
                    {
                        continue;
                    }
                    lock (_gate)
                    {
                        var streams = new IMessageStream[_clientStreams.Length + 1];
                        Array.Copy(_clientStreams, streams, _clientStreams.Length);
                        streams[streams.Length - 1] = clientStream;
                        _clientStreams = streams; // TODO Volatile Write?
                    }
                    _connection.ReceiveMessages(clientStream, RemoveStream);
                }
                catch
                {
                    clientSocket?.Dispose();
                }
            }
        }

        private void RemoveStream(IMessageStream stream, Exception e)
        {
            // TODO
        }

        public void TrySendMessage(Message message)
        {
            var streams = Volatile.Read(ref _clientStreams);
            foreach (var stream in streams)
            {
                stream.TrySendMessage(message);
            }
        }

        public Task SendMethodCallAsync(Message message)
        {
            return Task.FromException(new NotSupportedException("Cannot determine destination peer."));
        }

        public void Dispose()
        {
            _serverSocket?.Dispose();
        }
    }
}
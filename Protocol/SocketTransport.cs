// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace DBus.Transports
{
	class SocketTransport : Transport
	{
		internal Socket socket;

		public override void Open (AddressEntry entry)
		{
			string host, portStr, family;
			int port;

			if (!entry.Properties.TryGetValue ("host", out host))
				host = "localhost";

			if (!entry.Properties.TryGetValue ("port", out portStr))
				throw new Exception ("No port specified");

			if (!Int32.TryParse (portStr, out port))
				throw new Exception ("Invalid port: \"" + port + "\"");

			if (!entry.Properties.TryGetValue ("family", out family))
				family = null;

			Open (host, port, family);
		}

		public void Open (string host, int port, string family)
		{
			//TODO: use Socket directly
			TcpClient client = new TcpClient (host, port);
			/*
			client.NoDelay = true;
			client.ReceiveBufferSize = (int)Protocol.MaxMessageLength;
			client.SendBufferSize = (int)Protocol.MaxMessageLength;
			*/
			this.socket = client.Client;
			SocketHandle = (long)client.Client.Handle;
			Stream = client.GetStream ();
		}

		public void Open (Socket socket)
		{
			this.socket = socket;

			socket.Blocking = true;
			SocketHandle = (long)socket.Handle;
			//Stream = new UnixStream ((int)socket.Handle);
			Stream = new NetworkStream (socket);
		}

		public override void WriteCred ()
		{
			Stream.WriteByte (0);
		}

		public override string AuthString ()
		{
			return OSHelpers.PlatformIsUnixoid ?
				Mono.Unix.Native.Syscall.geteuid ().ToString ()                       // Unix User ID
				: System.Security.Principal.WindowsIdentity.GetCurrent ().User.Value; // Windows User ID
		}
	}
}

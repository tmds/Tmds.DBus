// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using Mono.Unix;
using Mono.Unix.Native;

namespace NDesk.DBus.Transports
{
	public class UnixMonoTransport : Transport, IAuthenticator
	{
		protected Socket socket;

		public UnixMonoTransport (string path, bool @abstract)
		{
			if (@abstract)
				socket = OpenAbstractUnix (path);
			else
				socket = OpenUnix (path);

			socket.Blocking = true;
			SocketHandle = (long)socket.Handle;
			//Stream = new UnixStream ((int)socket.Handle);
			Stream = new NetworkStream (socket);
		}

		public override string AuthString ()
		{
			long uid = UnixUserInfo.GetRealUserId ();

			return uid.ToString ();
		}

		protected Socket OpenAbstractUnix (string path)
		{
			AbstractUnixEndPoint ep = new AbstractUnixEndPoint (path);

			Socket client = new Socket (AddressFamily.Unix, SocketType.Stream, 0);
			client.Connect (ep);

			return client;
		}

		public Socket OpenUnix (string path)
		{
			UnixEndPoint remoteEndPoint = new UnixEndPoint (path);

			Socket client = new Socket (AddressFamily.Unix, SocketType.Stream, 0);
			client.Connect (remoteEndPoint);

			return client;
		}
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using Mono.Unix;
using Mono.Unix.Native;

namespace NDesk.DBus
{
	public class UnixTransport : Transport, IAuthenticator
	{
		/*
		public UnixTransport (int fd)
		{
		}
		*/

		protected Socket socket;

		public UnixTransport (string path, bool @abstract)
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
			/*
			byte[] p = System.Text.Encoding.Default.GetBytes (path);

			SocketAddress sa = new SocketAddress (AddressFamily.Unix, 2 + 1 + p.Length);
			sa[2] = 0; //null prefix for abstract sockets, see unix(7)
			for (int i = 0 ; i != p.Length ; i++)
				sa[i+3] = p[i];

			//TODO: this uglyness is a limitation of Mono.Unix
			UnixEndPoint remoteEndPoint = new UnixEndPoint ("foo");
			EndPoint ep = remoteEndPoint.Create (sa);
			*/

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

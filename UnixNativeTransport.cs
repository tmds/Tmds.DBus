// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Runtime.InteropServices;

using Mono.Unix;
using Mono.Unix.Native;

namespace NDesk.DBus
{
	public class UnixSocket
	{
		//TODO: verify these
		[DllImport ("libc")]
			protected static extern int socket (int domain, int type, int protocol);

		[DllImport ("libc")]
			protected static extern int connect (int sockfd, byte[] serv_addr, uint addrlen);

		public int Handle;

		public UnixSocket ()
		{
			//AddressFamily family, SocketType type, ProtocolType proto
			//Handle = socket ((int)AddressFamily.Unix, (int)SocketType.Stream, 0);
			Handle = socket (1, (int)SocketType.Stream, 0);

		}

		//TODO: consider memory management
		public void Connect (byte[] remote_end)
		{
			int ret = connect (Handle, remote_end, (uint)remote_end.Length);
			//Console.Error.WriteLine ("connect ret: " + ret);
			//FIXME: we need to get the errno or it will screw things up later?
		}
	}

	public class UnixNativeTransport : Transport, IAuthenticator
	{
		protected UnixSocket socket;

		public UnixNativeTransport (string path, bool @abstract)
		{
			if (@abstract)
				socket = OpenAbstractUnix (path);
			else
				socket = OpenUnix (path);

			//socket.Blocking = true;
			SocketHandle = (long)socket.Handle;
			Stream = new UnixStream ((int)socket.Handle);
		}

		public override string AuthString ()
		{
			long uid = UnixUserInfo.GetRealUserId ();

			return uid.ToString ();
		}

		protected UnixSocket OpenAbstractUnix (string path)
		{
			byte[] p = System.Text.Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + 1 + p.Length];

			//sa[0] = (byte)AddressFamily.Unix;
			sa[0] = 1;
			sa[1] = 0;

			sa[2] = 0; //null prefix for abstract sockets, see unix(7)
			for (int i = 0 ; i != p.Length ; i++)
				sa[i+3] = p[i];

			UnixSocket client = new UnixSocket ();
			client.Connect (sa);
			//Console.Error.WriteLine ("client Handle: " + client.Handle);

			return client;
		}

		public UnixSocket OpenUnix (string path)
		{
			byte[] p = System.Text.Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + p.Length + 1];

			//sa[0] = (byte)AddressFamily.Unix;
			sa[0] = 1;
			sa[1] = 0;

			for (int i = 0 ; i != p.Length ; i++)
				sa[i+2] = p[i];
			sa[2 + p.Length] = 0; //null suffix for sockets, see unix(7)

			UnixSocket client = new UnixSocket ();
			client.Connect (sa);
			//Console.Error.WriteLine ("client Handle: " + client.Handle);

			return client;
		}
	}
}

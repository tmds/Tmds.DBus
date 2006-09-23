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
		//TODO: big-endian testing

		//TODO: verify these
		[DllImport ("libc", SetLastError=true)]
			protected static extern int socket (int domain, int type, int protocol);

		[DllImport ("libc", SetLastError=true)]
			protected static extern int connect (int sockfd, byte[] serv_addr, uint addrlen);

		public int Handle;

		public UnixSocket ()
		{
			//TODO: don't hard-code PF_UNIX and SocketType.Stream
			//AddressFamily family, SocketType type, ProtocolType proto

			int r = socket (1, (int)SocketType.Stream, 0);
			//we should get the Exception from UnixMarshal and throw it here for a better stack trace, but the relevant API seems to be private
			UnixMarshal.ThrowExceptionForLastErrorIf (r);
			Handle = r;
		}

		//TODO: consider memory management
		public void Connect (byte[] remote_end)
		{
			int r = connect (Handle, remote_end, (uint)remote_end.Length);
			//we should get the Exception from UnixMarshal and throw it here for a better stack trace, but the relevant API seems to be private
			UnixMarshal.ThrowExceptionForLastErrorIf (r);
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

			sa[2] = 0; //null prefix for abstract domain socket addresses, see unix(7)
			for (int i = 0 ; i != p.Length ; i++)
				sa[3 + i] = p[i];

			UnixSocket client = new UnixSocket ();
			client.Connect (sa);

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
				sa[2 + i] = p[i];
			sa[2 + p.Length] = 0; //null suffix for domain socket addresses, see unix(7)

			UnixSocket client = new UnixSocket ();
			client.Connect (sa);

			return client;
		}
	}
}

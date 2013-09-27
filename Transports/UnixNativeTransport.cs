// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//We send BSD-style credentials on all platforms
//Doesn't seem to break Linux (but is redundant there)
//This may turn out to be a bad idea
#define HAVE_CMSGCRED

using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using DBus.Unix;
using DBus.Protocol;

namespace DBus.Transports
{
	class UnixNativeTransport : UnixTransport
	{
		internal UnixSocket socket;

		public override string AuthString ()
		{
			long uid = Mono.Unix.Native.Syscall.geteuid ();
			return uid.ToString ();
		}

		public override void Open (string path, bool @abstract)
		{
			if (String.IsNullOrEmpty (path))
				throw new ArgumentException ("path");

			if (@abstract)
				socket = OpenAbstractUnix (path);
			else
				socket = OpenUnix (path);

			//socket.Blocking = true;
			SocketHandle = (long)socket.Handle;
			//Stream = new UnixStream ((int)socket.Handle);
			Stream = new UnixStream (socket);
		}

		//send peer credentials null byte
		//different platforms do this in different ways
#if HAVE_CMSGCRED
		unsafe void WriteBsdCred ()
		{
			//null credentials byte
			byte buf = 0;

			IOVector iov = new IOVector ();
			//iov.Base = (IntPtr)(&buf);
			iov.Base = &buf;
			iov.Length = 1;

			msghdr msg = new msghdr ();
			msg.msg_iov = &iov;
			msg.msg_iovlen = 1;

			cmsg cm = new cmsg ();
			msg.msg_control = (IntPtr)(&cm);
			msg.msg_controllen = (uint)sizeof (cmsg);
			cm.hdr.cmsg_len = (uint)sizeof (cmsg);
			cm.hdr.cmsg_level = 0xffff; //SOL_SOCKET
			cm.hdr.cmsg_type = 0x03; //SCM_CREDS

			int written = socket.SendMsg (&msg, 0);
			if (written != 1)
				throw new Exception ("Failed to write credentials");
		}
#endif

		public override void WriteCred ()
		{
#if HAVE_CMSGCRED
			try {
				WriteBsdCred ();
				return;
			} catch {
				if (ProtocolInformation.Verbose)
					Console.Error.WriteLine ("Warning: WriteBsdCred() failed; falling back to ordinary WriteCred()");
			}
#endif
			//null credentials byte
			byte buf = 0;
			Stream.WriteByte (buf);
		}

		public static byte[] GetSockAddr (string path)
		{
			byte[] p = Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + p.Length + 1];

			//we use BitConverter to stay endian-safe
			byte[] afData = BitConverter.GetBytes (UnixSocket.AF_UNIX);
			sa[0] = afData[0];
			sa[1] = afData[1];

			for (int i = 0 ; i != p.Length ; i++)
				sa[2 + i] = p[i];
			sa[2 + p.Length] = 0; //null suffix for domain socket addresses, see unix(7)

			return sa;
		}

		public static byte[] GetSockAddrAbstract (string path)
		{
			byte[] p = Encoding.Default.GetBytes (path);

			byte[] sa = new byte[2 + 1 + p.Length];

			//we use BitConverter to stay endian-safe
			byte[] afData = BitConverter.GetBytes (UnixSocket.AF_UNIX);
			sa[0] = afData[0];
			sa[1] = afData[1];

			sa[2] = 0; //null prefix for abstract domain socket addresses, see unix(7)
			for (int i = 0 ; i != p.Length ; i++)
				sa[3 + i] = p[i];

			return sa;
		}

		internal UnixSocket OpenUnix (string path)
		{
			byte[] sa = GetSockAddr (path);
			UnixSocket client = new UnixSocket ();
			client.Connect (sa);
			return client;
		}

		internal UnixSocket OpenAbstractUnix (string path)
		{
			byte[] sa = GetSockAddrAbstract (path);
			UnixSocket client = new UnixSocket ();
			client.Connect (sa);
			return client;
		}
	}

#if HAVE_CMSGCRED
	unsafe struct msghdr
	{
		public IntPtr msg_name; //optional address
		public uint msg_namelen; //size of address
		public IOVector *msg_iov; //scatter/gather array
		public int msg_iovlen; //# elements in msg_iov
		public IntPtr msg_control; //ancillary data, see below
		public uint msg_controllen; //ancillary data buffer len
		public int msg_flags; //flags on received message
	}

	struct cmsghdr
	{
		public uint cmsg_len; //data byte count, including header
		public int cmsg_level; //originating protocol
		public int cmsg_type; //protocol-specific type
	}

	unsafe struct cmsgcred
	{
		const int CMGROUP_MAX = 16;

		public int cmcred_pid; //PID of sending process
		public uint cmcred_uid; //real UID of sending process
		public uint cmcred_euid; //effective UID of sending process
		public uint cmcred_gid; //real GID of sending process
		public short cmcred_ngroups; //number or groups
		public fixed uint cmcred_groups[CMGROUP_MAX]; //groups
	}

	struct cmsg
	{
		public cmsghdr hdr;
		public cmsgcred cred;
	}
#endif
}

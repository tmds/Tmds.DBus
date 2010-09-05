// Copyright 2008 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DBus.Unix
{
	// size_t
	using SizeT = System.UIntPtr;
	// ssize_t
	using SSizeT = System.IntPtr;
	// socklen_t: assumed to be 4 bytes
	// uid_t: assumed to be 4 bytes

	sealed class UnixStream : Stream //, IDisposable
	{
		public readonly UnixSocket usock;

		public UnixStream (int fd)
		{
			this.usock = new UnixSocket (fd);
		}

		public UnixStream (UnixSocket usock)
		{
			this.usock = usock;
		}

		public override bool CanRead
		{
			get {
				return true;
			}
		}

		public override bool CanSeek
		{
			get {
				return false;
			}
		}

		public override bool CanWrite
		{
			get {
				return true;
			}
		}

		public override long Length
		{
			get {
				throw new NotImplementedException ("Seeking is not implemented");
			}
		}

		public override long Position
		{
			get {
				throw new NotImplementedException ("Seeking is not implemented");
			} set {
				throw new NotImplementedException ("Seeking is not implemented");
			}
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException ("Seeking is not implemented");
		}

		public override void SetLength (long value)
		{
			throw new NotImplementedException ("Not implemented");
		}

		public override void Flush ()
		{
		}

		public override int Read ([In, Out] byte[] buffer, int offset, int count)
		{
			return usock.Read (buffer, offset, count);
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			usock.Write (buffer, offset, count);
		}

		unsafe public override int ReadByte ()
		{
			byte value;
			usock.Read (&value, 1);
			return value;
		}

		unsafe public override void WriteByte (byte value)
		{
			usock.Write (&value, 1);
		}
	}

	static class UnixUid
	{
		internal const string LIBC = "libc";

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=false)]
		static extern uint getuid ();

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=false)]
		static extern uint geteuid ();

		public static long GetUID ()
		{
			long uid = getuid ();
			return uid;
		}

		public static long GetEUID ()
		{
			long euid = geteuid ();
			return euid;
		}
	}

	static class UnixError
	{
		internal const string LIBC = "libc";

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=false)]
		static extern IntPtr strerror (int errnum);

		static string GetErrorString (int errnum)
		{
			IntPtr strPtr = strerror (errnum);

			if (strPtr == IntPtr.Zero)
				return "Unknown Unix error";

			return Marshal.PtrToStringAnsi (strPtr);
		}

		// FIXME: Don't hard-code this.
		const int EINTR = 4;

		public static bool ShouldRetry
		{
			get {
				int errno = System.Runtime.InteropServices.Marshal.GetLastWin32Error ();
				return errno == EINTR;
			}
		}

		public static Exception GetLastUnixException ()
		{
			int errno = System.Runtime.InteropServices.Marshal.GetLastWin32Error ();
			return new Exception (String.Format ("Error {0}: {1}", errno, GetErrorString (errno)));
		}
	}

	//[StructLayout(LayoutKind.Sequential, Pack=1)]
	unsafe struct IOVector
	{
		public IOVector (IntPtr bbase, int length)
		{
			this.Base = (void*)bbase;
			this.length = (SizeT)length;
		}

		//public IntPtr Base;
		public void* Base;

		public SizeT length;
		public int Length
		{
			get {
				return (int)length;
			} set {
				length = (SizeT)value;
			}
		}
	}

	/*
	unsafe class SockAddr
	{
		byte[] data;
	}
	*/

	unsafe class UnixSocket
	{
		internal const string LIBC = "libc";

		// Solaris provides socket functionality in libsocket rather than libc.
		// We use a dllmap in the .config to deal with this.
		internal const string LIBSOCKET = "libsocket";

		public const short AF_UNIX = 1;
		// FIXME: SOCK_STREAM is 2 on Solaris
		public const short SOCK_STREAM = 1;

		[DllImport (LIBC, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
		internal static extern IntPtr fork ();

		[DllImport (LIBC, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
		internal static extern int dup2 (int fd, int fd2);

		[DllImport (LIBC, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
		internal static extern int open ([MarshalAs(UnmanagedType.LPStr)] string path, int oflag);

		[DllImport (LIBC, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
		internal static extern IntPtr setsid ();


		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		internal static extern int close (int fd);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int socket (int domain, int type, int protocol);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int connect (int sockfd, byte[] serv_addr, uint addrlen);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int bind (int sockfd, byte[] my_addr, uint addrlen);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int listen (int sockfd, int backlog);

		//TODO: this prototype is probably wrong, fix it
		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int accept (int sockfd, void* addr, ref uint addrlen);

		//TODO: confirm and make use of these functions
		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int getsockopt (int s, int optname, IntPtr optval, ref uint optlen);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		protected static extern int setsockopt (int s, int optname, IntPtr optval, uint optlen);

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		unsafe static extern SSizeT read (int fd, byte* buf, SizeT count);

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		unsafe static extern SSizeT write (int fd, byte* buf, SizeT count);

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		unsafe static extern SSizeT readv (int fd, IOVector* iov, int iovcnt);

		[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		unsafe static extern SSizeT writev (int fd, IOVector* iov, int iovcnt);

		// Linux
		//[DllImport (LIBC, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		//static extern int vmsplice (int fd, IOVector* iov, uint nr_segs, uint flags);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		public static extern SSizeT recvmsg (int s, void* msg, int flags);

		[DllImport (LIBSOCKET, CallingConvention=CallingConvention.Cdecl, SetLastError=true)]
		public static extern SSizeT sendmsg (int s, void* msg, int flags);

		public int Handle;
		bool ownsHandle = false;

		public UnixSocket (int handle) : this (handle, false)
		{
		}

		public UnixSocket (int handle, bool ownsHandle)
		{
			this.Handle = handle;
			this.ownsHandle = ownsHandle;
			// TODO: SafeHandle?
		}

		public UnixSocket ()
		{
			//TODO: don't hard-code PF_UNIX and SOCK_STREAM or SocketType.Stream
			//AddressFamily family, SocketType type, ProtocolType proto

			int r = socket (AF_UNIX, SOCK_STREAM, 0);
			if (r < 0)
				throw UnixError.GetLastUnixException ();

			Handle = r;
			ownsHandle = true;
		}

		~UnixSocket ()
		{
			if (ownsHandle && Handle > 0)
				Close ();
		}

		protected bool connected = false;

		//TODO: consider memory management
		public void Close ()
		{
			int r = 0;

			do {
				r = close (Handle);
			} while (r < 0 && UnixError.ShouldRetry);

			if (r < 0)
				throw UnixError.GetLastUnixException ();

			Handle = -1;
			connected = false;
		}

		//TODO: consider memory management
		public void Connect (byte[] remote_end)
		{
			int r = 0;

			do {
				r = connect (Handle, remote_end, (uint)remote_end.Length);
			} while (r < 0 && UnixError.ShouldRetry);

			if (r < 0)
				throw UnixError.GetLastUnixException ();

			connected = true;
		}

		//assigns a name to the socket
		public void Bind (byte[] local_end)
		{
			int r = bind (Handle, local_end, (uint)local_end.Length);
			if (r < 0)
				throw UnixError.GetLastUnixException ();
		}

		public void Listen (int backlog)
		{
			int r = listen (Handle, backlog);
			if (r < 0)
				throw UnixError.GetLastUnixException ();
		}

		public UnixSocket Accept ()
		{
			byte[] addr = new byte[110];
			uint addrlen = (uint)addr.Length;

			fixed (byte* addrP = addr) {
				int r = 0;

				do {
					r = accept (Handle, addrP, ref addrlen);
				} while (r < 0 && UnixError.ShouldRetry);

				if (r < 0)
					throw UnixError.GetLastUnixException ();

				//TODO: use the returned addr
				//string str = Encoding.Default.GetString (addr, 0, (int)addrlen);
				return new UnixSocket (r, true);
			}
		}

		unsafe public int Read (byte[] buf, int offset, int count)
		{
			fixed (byte* bufP = buf)
				return Read (bufP + offset, count);
		}

		public int Write (byte[] buf, int offset, int count)
		{
			fixed (byte* bufP = buf)
				return Write (bufP + offset, count);
		}

		unsafe public int Read (byte* bufP, int count)
		{
			int r = 0;

			do {
				r = (int)read (Handle, bufP, (SizeT)count);
			} while (r < 0 && UnixError.ShouldRetry);

			if (r < 0)
				throw UnixError.GetLastUnixException ();

			return r;
		}

		public int Write (byte* bufP, int count)
		{
			int r = 0;

			do {
				r = (int)write (Handle, bufP, (SizeT)count);
			} while (r < 0 && UnixError.ShouldRetry);

			if (r < 0)
				throw UnixError.GetLastUnixException ();

			return r;
		}

		public int RecvMsg (void* bufP, int flags)
		{
			int r = 0;

			do {
				r = (int)recvmsg (Handle, bufP, flags);
			} while (r < 0 && UnixError.ShouldRetry);

			if (r < 0)
				throw UnixError.GetLastUnixException ();

			return r;
		}

		public int SendMsg (void* bufP, int flags)
		{
			int r = 0;

			do {
				r = (int)sendmsg (Handle, bufP, flags);
			} while (r < 0 && UnixError.ShouldRetry);

			if (r < 0)
				throw UnixError.GetLastUnixException ();

			return r;
		}

		public int ReadV (IOVector* iov, int count)
		{
			//FIXME: Handle EINTR here or elsewhere
			//FIXME: handle r != count
			//TODO: check offset correctness

			int r = (int)readv (Handle, iov, count);
			if (r < 0)
				throw UnixError.GetLastUnixException ();

			return r;
		}

		public int WriteV (IOVector* iov, int count)
		{
			//FIXME: Handle EINTR here or elsewhere
			//FIXME: handle r != count
			//TODO: check offset correctness

			int r = (int)writev (Handle, iov, count);
			if (r < 0)
				throw UnixError.GetLastUnixException ();

			return r;
		}

		public int Write (IOVector[] iov, int offset, int count)
		{
			//FIXME: Handle EINTR here or elsewhere
			//FIXME: handle r != count
			//TODO: check offset correctness

			fixed (IOVector* bufP = &iov[offset]) {
				int r = (int)writev (Handle, bufP + offset, count);
				if (r < 0)
					throw UnixError.GetLastUnixException ();

				return r;
			}
		}

		public int Write (IOVector[] iov)
		{
			return Write (iov, 0, iov.Length);
		}

	}
}

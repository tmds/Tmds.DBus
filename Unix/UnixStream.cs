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
}


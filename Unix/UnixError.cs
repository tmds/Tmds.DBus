// Copyright 2008 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DBus
{
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
}


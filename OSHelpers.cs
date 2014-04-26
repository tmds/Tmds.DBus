using System;

#if !NET35
using System.IO.MemoryMappedFiles;
#endif

namespace DBus
{
	internal class OSHelpers
	{

		static PlatformID platformid = Environment.OSVersion.Platform;

		public static bool PlatformIsUnixoid
		{
			get {
				switch (platformid) {
					case PlatformID.Win32S:       return false;
					case PlatformID.Win32Windows: return false;
					case PlatformID.Win32NT:      return false;
					case PlatformID.WinCE:        return false;
					case PlatformID.Unix:         return true;
					case PlatformID.Xbox:         return false;
					case PlatformID.MacOSX:       return true;
					default:                      return false;
				}
			}
		}

		// Reads a string from shared memory with the ID "id".
		// Optionally, a maximum length can be specified. A negative number means "no limit".
		public static string ReadSharedMemoryString (string id, long maxlen = -1)
		{
#if !NET35
			MemoryMappedFile shmem;
			try {
				shmem = MemoryMappedFile.OpenExisting (id);
			} catch {
				shmem = null;
			}
			if (shmem == null)
				return null;
			MemoryMappedViewStream s = shmem.CreateViewStream ();
			long len = s.Length;
			if (maxlen >= 0 && len > maxlen)
				len = maxlen;
			if (len == 0)
				return string.Empty;
			if (len > Int32.MaxValue)
				len = Int32.MaxValue;
			byte[] bytes = new byte[len];
			int count = s.Read (bytes, 0, (int)len);
			if (count <= 0)
				return string.Empty;

			count = 0;
			while (count < len && bytes[count] != 0)
				count++;

			return System.Text.Encoding.UTF8.GetString (bytes, 0, count);
#else
			return null;
#endif
		}

	}
}

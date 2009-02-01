// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

namespace NDesk.DBus
{
	using Authentication;

	unsafe struct UUID
	{
		// public to avoid a bizarre CS0649 warning
		//private fixed int data[4];
		public fixed int data[4];
		//private int a, b, c, d;

		public static readonly UUID Zero = new UUID ();

		public static bool operator == (UUID a, UUID b)
		{
			if (a.data[0] != b.data[0])
				return false;
			if (a.data[1] != b.data[1])
				return false;
			if (a.data[2] != b.data[2])
				return false;
			if (a.data[3] != b.data[3])
				return false;

			return true;
		}

		public static bool operator != (UUID a, UUID b)
		{
			return !(a == b);
		}

		public override bool Equals (object o)
		{
			if (o == null)
				return false;

			if (!(o is UUID))
				return false;

			return this == (UUID)o;
		}

		public override int GetHashCode ()
		{
			fixed (int* p = &data[0])
				return p[0] ^ p[1] ^ p[2] ^ p[3];
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder (16 * 2);

			fixed (int* p = &data[0]) {
					byte* bp = (byte*)p;
					for (int i = 0 ; i != 16 ; i++)
						sb.Append (bp[i].ToString ("x2", CultureInfo.InvariantCulture));
			}

			return sb.ToString ();
		}

		public static UUID Parse (string hex)
		{
			byte[] guidData = SaslClient.FromHex (hex);
			if (guidData.Length != 16)
				throw new Exception ("Cannot parse UUID/GUID of invalid length");

			UUID id = new UUID ();
			fixed (byte* bp = &guidData[0]) {
				int* p = (int*)bp;
				for (int i = 0 ; i != 4 ; i++)
					id.data[i] = p[i];
			}

			return id;
		}

		static Random rand = new Random ();
		public static UUID Generate (DateTime timestamp)
		{
			UUID id = new UUID ();

			fixed (int* p = &id.data[0]) {
				p[0] = rand.Next ();
				p[1] = rand.Next ();
				p[2] = rand.Next ();
			}

			//p[3] is assigned to by Timestamp
			id.Timestamp = timestamp;

			return id;
		}

		public static UUID Generate ()
		{
			return Generate (DateTime.Now);
		}

		public uint UnixTimestamp
		{
			get {
				uint unixTime;

				fixed (int* ip = &data[3]) {
					if (BitConverter.IsLittleEndian) {
						byte* p = (byte*)ip;
						byte* bp = (byte*)&unixTime;
						bp[0] = p[3];
						bp[1] = p[2];
						bp[2] = p[1];
						bp[3] = p[0];
					} else {
						unixTime = *(uint*)ip;
					}
				}

				return unixTime;
			} set {
				uint unixTime = value;

				fixed (int* ip = &data[3]) {
					if (BitConverter.IsLittleEndian) {
						byte* p = (byte*)&unixTime;
						byte* bp = (byte*)ip;
						bp[0] = p[3];
						bp[1] = p[2];
						bp[2] = p[1];
						bp[3] = p[0];
					} else {
						*(uint*)ip = unixTime;
					}
				}
			}
		}

		public DateTime Timestamp
		{
			get {
				return SaslClient.UnixToDateTime (UnixTimestamp);
			} set {
				UnixTimestamp = (uint)SaslClient.DateTimeToUnix (value);
			}
		}
	}
}

namespace NDesk.DBus.Authentication
{
	enum ClientState
	{
		WaitingForData,
		WaitingForOK,
		WaitingForReject,
	}

	enum ServerState
	{
		WaitingForAuth,
		WaitingForData,
		WaitingForBegin,
	}

	class SaslClient
	{
		protected Connection conn;

		protected SaslClient ()
		{
		}

		public SaslClient (Connection conn)
		{
			this.conn = conn;
		}

		public void Run ()
		{
			ActualId = UUID.Zero;

			StreamReader sr = new StreamReader (conn.Transport.Stream, Encoding.ASCII);
			StreamWriter sw = new StreamWriter (conn.Transport.Stream, Encoding.ASCII);

			sw.NewLine = "\r\n";

			string str = conn.Transport.AuthString ();
			byte[] bs = Encoding.ASCII.GetBytes (str);

			string authStr = ToHex (bs);

			sw.WriteLine ("AUTH EXTERNAL {0}", authStr);
			sw.Flush ();

			string ok_rep = sr.ReadLine ();

			string[] parts;
			parts = ok_rep.Split (' ');

			if (parts.Length < 1 || parts[0] != "OK")
				throw new Exception ("Authentication error: AUTH EXTERNAL was not OK: \"" + ok_rep + "\"");

			if (parts.Length > 1)
				ActualId = UUID.Parse (parts[1]);

			sw.WriteLine ("BEGIN");
			sw.Flush ();
		}

		public UUID ActualId = UUID.Zero;

		//From Mono.Unix.Native.NativeConvert
		//should these methods use long or (u)int?
		public static DateTime UnixToDateTime (long time)
		{
			DateTime LocalUnixEpoch = new DateTime (1970, 1, 1);
			TimeSpan LocalUtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset (DateTime.UtcNow);
			return LocalUnixEpoch.AddSeconds ((double) time + LocalUtcOffset.TotalSeconds);
		}

		public static long DateTimeToUnix (DateTime time)
		{
			DateTime LocalUnixEpoch = new DateTime (1970, 1, 1);
			TimeSpan LocalUtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset (DateTime.UtcNow);
			TimeSpan unixTime = time.Subtract (LocalUnixEpoch) - LocalUtcOffset;

			return (long) unixTime.TotalSeconds;
		}

		//From Mono.Security.Cryptography
		//Modified to output lowercase hex
		static public string ToHex (byte[] input)
		{
			if (input == null)
				return null;

			StringBuilder sb = new StringBuilder (input.Length * 2);
			foreach (byte b in input) {
				sb.Append (b.ToString ("x2", CultureInfo.InvariantCulture));
			}
			return sb.ToString ();
		}

		//From Mono.Security.Cryptography
		static private byte FromHexChar (char c)
		{
			if ((c >= 'a') && (c <= 'f'))
				return (byte) (c - 'a' + 10);
			if ((c >= 'A') && (c <= 'F'))
				return (byte) (c - 'A' + 10);
			if ((c >= '0') && (c <= '9'))
				return (byte) (c - '0');
			throw new ArgumentException ("Invalid hex char");
		}

		//From Mono.Security.Cryptography
		static public byte[] FromHex (string hex)
		{
			if (hex == null)
				return null;
			if ((hex.Length & 0x1) == 0x1)
				throw new ArgumentException ("Length must be a multiple of 2");

			byte[] result = new byte [hex.Length >> 1];
			int n = 0;
			int i = 0;
			while (n < result.Length) {
				result [n] = (byte) (FromHexChar (hex [i++]) << 4);
				result [n++] += FromHexChar (hex [i++]);
			}
			return result;
		}
	}
}

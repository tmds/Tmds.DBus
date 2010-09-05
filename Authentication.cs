// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DBus
{
	using Authentication;

	//using System.Runtime.InteropServices;
	//[StructLayout (LayoutKind.Sequential)]
	unsafe struct UUID
	{
		private int a, b, c, d;
		const int ByteLength = 16;

		public static readonly UUID Zero = new UUID ();

		public static bool operator == (UUID a, UUID b)
		{
			if (a.a == b.a && a.b == b.b && a.c == b.c && a.d == b.d)
				return true;
			else
				return false;
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
			return a ^ b ^ c ^ d;
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder (ByteLength * 2);

			fixed (int* p = &a) {
					byte* bp = (byte*)p;
					for (int i = 0 ; i != ByteLength ; i++)
						sb.Append (bp[i].ToString ("x2", CultureInfo.InvariantCulture));
			}

			return sb.ToString ();
		}

		public static UUID Parse (string hex)
		{
			if (hex.Length != ByteLength * 2)
				throw new Exception ("Cannot parse UUID/GUID of invalid length");

			UUID id = new UUID ();

			byte* result = (byte*)&id.a;
			int n = 0, i = 0;
			while (n < ByteLength) {
				result[n] = (byte)(Sasl.FromHexChar (hex[i++]) << 4);
				result[n++] += Sasl.FromHexChar (hex[i++]);
			}

			return id;
		}

		static Random rand = new Random ();
		static byte[] buf = new byte[12];
		public static UUID Generate (DateTime timestamp)
		{
			UUID id = new UUID ();

			lock (buf) {
				rand.NextBytes (buf);
				fixed (byte* bp = &buf[0]) {
					int* p = (int*)bp;
					id.a = p[0];
					id.b = p[1];
					id.c = p[2];
				}
			}

			//id.d is assigned to by Timestamp
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

				fixed (int* ip = &d) {
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

				fixed (int* ip = &d) {
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
				return Sasl.UnixToDateTime (UnixTimestamp);
			} set {
				UnixTimestamp = (uint)Sasl.DateTimeToUnix (value);
			}
		}
	}
}

namespace DBus.Authentication
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

	class AuthCommand
	{
		/*
		public AuthCommand (string value)
		{
			//this.Value = value;
			this.Value = value.Trim ();
		}
		*/


		public AuthCommand (string value)
		{
			//this.Value = value;
			this.Value = value.Trim ();
			Args.AddRange (Value.Split (' '));
		}

		readonly List<string> Args = new List<string> ();

		public string this[int index]
		{
			get {
				if (index >= Args.Count)
					return String.Empty;
				return Args[index];
			}
		}

		/*
		public AuthCommand (string value, params string[] args)
		{
			if (args.Length == 0)
				this.Value = value;
			else
				this.Value = value + " " + String.Join (" ", args);
		}
		*/

		public readonly string Value;
	}

	class SaslPeer : IEnumerable<AuthCommand>
	{
		//public Connection conn;
		public SaslPeer Peer;

		public Stream stream = null;
		public bool UseConsole = false;

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		internal bool isFinalRead = false;

		public virtual IEnumerator<AuthCommand> GetEnumerator ()
		{
			// Read the mandatory null credentials byte
			/*
			if (!UseConsole)
				if (conn.Transport.Stream.ReadByte () != 0)
					yield break;
			*/

			TextReader sr;
			sr = UseConsole ? Console.In : new StreamReader (stream, Encoding.ASCII);

			while (true) {
				string ln;
				bool isEnd = false;
				if (!UseConsole && isFinalRead) {
					StringBuilder sb = new StringBuilder ();

					while (true) {
						//MemoryStream ms = new MemoryStream ();
						// TODO: Use char instead? Check for -1?
						int a = stream.ReadByte ();

						if (a == -1) {
							isEnd = true;
							break;
						}

						if (a == '\r') {
							int b = stream.ReadByte ();
							if (b != '\n')
								throw new Exception ();
							break;
						}

						sb.Append ((char)a);
					}

					ln = sb.ToString ();
					//isFinalRead = false;
				} else {
					ln = sr.ReadLine ();
				}

				//if (isEnd && ln == string.Empty)
				//	yield break;
				if (ln == null)
					yield break;
				if (ln != String.Empty)
					yield return new AuthCommand (ln);
				if (isEnd)
					yield break;
			}
		}

		public bool Authenticate ()
		{
			return Run (this);
		}

		public bool AuthenticateSelf ()
		{
			//IEnumerator<AuthCommand> a = Peer.GetEnumerator ();
			IEnumerator<AuthCommand> b = GetEnumerator ();
			//bool ret = b.MoveNext ();
			while (b.MoveNext ()) {
				if (b.Current.Value == "BEGIN")
					return true;
			}
			return false;
		}

		public virtual bool Run (IEnumerable<AuthCommand> commands)
		{
			TextWriter sw;
			sw = UseConsole ? Console.Out : new StreamWriter (stream, Encoding.ASCII);
			if (!UseConsole)
				sw.NewLine = "\r\n";

			foreach (AuthCommand command in commands) {
				if (command == null) {
					// Disconnect here?
					return false;
				}
				sw.WriteLine (command.Value);
				sw.Flush ();
			}

			return true;
		}
	}

	class SaslClient : SaslPeer
	{
		public string Identity = String.Empty;

		//static Regex rejectedRegex = new Regex (@"^REJECTED(\s+(\w+))*$");

		// This enables simple support for multiple AUTH schemes
		enum AuthMech
		{
			External,
			Anonymous,
			None,
		}

		public override IEnumerator<AuthCommand> GetEnumerator ()
		{
			IEnumerator<AuthCommand> replies = Peer.GetEnumerator ();

			AuthMech currMech = AuthMech.External;

			while (true) {
				Peer.isFinalRead = false;

				if (currMech == AuthMech.External) {
					string str = Identity;
					byte[] bs = Encoding.ASCII.GetBytes (str);
					string initialData = Sasl.ToHex (bs);
					yield return new AuthCommand ("AUTH EXTERNAL " + initialData);
					currMech = AuthMech.Anonymous;
				} else if (currMech == AuthMech.Anonymous) {
					yield return new AuthCommand ("AUTH ANONYMOUS");
					currMech = AuthMech.None;
				} else {
					throw new Exception ("Authentication failure");
				}

				Peer.isFinalRead = true;

				AuthCommand reply;
				if (!replies.MoveNext ())
					yield break;
				reply = replies.Current;

				if (reply[0] == "REJECTED") {
					continue;
				}

				/*
				Match m = rejectedRegex.Match (reply.Value);
				if (m.Success) {
					string[] mechanisms = m.Groups[1].Value.Split (' ');
					//yield return new AuthCommand ("CANCEL");
					continue;
				}
				*/

				if (reply[0] != "OK") {
					yield return new AuthCommand ("ERROR");
					continue;
				}

				if (reply[1] == String.Empty)
					ActualId = UUID.Zero;
				else
					ActualId = UUID.Parse (reply[1]);

				yield return new AuthCommand ("BEGIN");
				yield break;
			}

		}

		public UUID ActualId = UUID.Zero;
	}

	class SaslServer : SaslPeer
	{
		//public int MaxFailures = 10;
		public UUID Guid = UUID.Zero;

		public long uid = 0;

		static Regex authRegex = new Regex (@"^AUTH\s+(\w+)(?:\s+(.*))?$");
		static string[] supportedMechanisms = {"EXTERNAL"};

		public override IEnumerator<AuthCommand> GetEnumerator ()
		{
			IEnumerator<AuthCommand> replies = Peer.GetEnumerator ();

			while (true) {
				Peer.isFinalRead = false;

				AuthCommand reply;
				if (!replies.MoveNext ()) {
					yield return null;
					yield break;
					//continue;
				}
				reply = replies.Current;

				Match m = authRegex.Match (reply.Value);
				if (!m.Success) {
					yield return new AuthCommand ("ERROR");
					continue;
				}

				string mechanism = m.Groups[1].Value;
				string initialResponse = m.Groups[2].Value;

				if (mechanism == "EXTERNAL") {
					try {
						byte[] bs = Sasl.FromHex (initialResponse);
						string authStr = Encoding.ASCII.GetString (bs);
						uid = UInt32.Parse (authStr);
					} catch {
						uid = 0;
					}
					//return RunExternal (Run (), initialResponse);
				} else {
					yield return new AuthCommand ("REJECTED " + String.Join (" ", supportedMechanisms));
					continue;
				}

				if (Guid == UUID.Zero)
					yield return new AuthCommand ("OK");
				else
					yield return new AuthCommand ("OK " + Guid.ToString ());

				Peer.isFinalRead = true;

				if (!replies.MoveNext ()) {
					/*
					yield break;
					continue;
					*/
					yield return null;
					yield break;
				}

				reply = replies.Current;
				if (reply.Value != "BEGIN") {
					yield return new AuthCommand ("ERROR");
					continue;
				}

				yield break;
			}
		}
	}

	static class Sasl
	{
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
		static public byte FromHexChar (char c)
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

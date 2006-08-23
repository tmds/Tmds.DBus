// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

using System.Runtime.InteropServices;

using System.Text;
using System.Globalization;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	public enum ClientState
	{
		WaitingForData,
		WaitingForOK,
		WaitingForReject,
	}

	public enum ServerState
	{
		WaitingForAuth,
		WaitingForData,
		WaitingForBegin,
	}

	public partial class Connection
	{
		public void Authenticate ()
		{
			//NetworkStream ns = new NetworkStream (sock);
			//UnixStream ns = new UnixStream ((int)sock.Handle);
			StreamReader sr = new StreamReader (ns, System.Text.Encoding.ASCII);
			StreamWriter sw = new StreamWriter (ns, System.Text.Encoding.ASCII);

			sw.NewLine = "\r\n";
			//sw.AutoFlush = true;

			sw.Write ('\0');

			string str = transport.AuthString ();
			byte[] bs = Encoding.ASCII.GetBytes (str);

			string authStr = ToHex (bs);

			sw.WriteLine ("AUTH EXTERNAL {0}", authStr);
			sw.Flush ();

			string ok_rep = sr.ReadLine ();

			string[] parts;
			parts = ok_rep.Split (' ');

			string guid = parts[1];
			//Console.WriteLine ("guid: " + guid);

			sw.WriteLine ("BEGIN");
			sw.Flush ();
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

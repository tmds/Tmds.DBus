// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class MessageDumper
	{
		public static byte[] ReadBlock (TextReader r)
		{
			//if (Body == null)
			//	return;

			MemoryStream ms = new MemoryStream ();

			while (true) {
				string ln = r.ReadLine ();
				if (ln == null)
					break;
				if (!ReadFromHex (ms, ln))
					break;
			}

			if (ms.Length == 0)
				return null;

			return ms.ToArray ();
		}

		public static void WriteComment (string comment, TextWriter w)
		{
			w.WriteLine ("# " + comment);
		}

		public static void WriteBlock (byte[] Body, TextWriter w)
		{
			//if (Body == null)
			//	return;
			if (Body != null)
			for (int i = 0 ; i != Body.Length ; i++) {
				if (i == 0) {}
				else if (i % 32 == 0)
					w.WriteLine ();
				else if (i % 4 == 0)
					w.Write (' ');

				w.Write (Body[i].ToString ("x2", System.Globalization.CultureInfo.InvariantCulture));
			}

			w.Write ('.');
			w.WriteLine ();
			w.Flush ();
		}

		public static void WriteMessage (Message msg, TextWriter w)
		{
			w.WriteLine ("# Message");
			w.WriteLine ("# Header");
			MessageDumper.WriteBlock (msg.GetHeaderData (), w);
			w.WriteLine ("# Body");
			MessageDumper.WriteBlock (msg.Body, w);
			w.WriteLine ();
			w.Flush ();
		}

		public static Message ReadMessage (TextReader r)
		{
			byte[] header = MessageDumper.ReadBlock (r);

			if (header == null)
				return null;

			byte[] body = MessageDumper.ReadBlock (r);
			
			return Message.FromReceivedBytes (null, header, body);			
		}

		static byte FromHexChar (char c)
		{
			if ((c >= 'a') && (c <= 'f'))
				return (byte) (c - 'a' + 10);
			if ((c >= 'A') && (c <= 'F'))
				return (byte) (c - 'A' + 10);
			if ((c >= '0') && (c <= '9'))
				return (byte) (c - '0');
			throw new ArgumentException ("Invalid hex char");
		}

		static bool ReadFromHex (Stream ms, string hex)
		{
			if (hex.StartsWith ("#"))
				return true;

			int i = 0;
			while (i < hex.Length) {
				if (char.IsWhiteSpace (hex [i])) {
					i++;
					continue;
				}

				if (hex [i] == '.') {
					ms.Flush ();
					return false;
				}

				byte res = (byte) (FromHexChar (hex [i++]) << 4);
				res += FromHexChar (hex [i++]);
				ms.WriteByte (res);
			}

			ms.Flush ();
			return true;
		}
	}
}
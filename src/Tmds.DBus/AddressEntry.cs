// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Collections.Generic;

namespace DBus
{
	class AddressEntry
	{
		public string Method;
		public readonly IDictionary<string,string> Properties = new Dictionary<string,string> ();
		public UUID GUID = UUID.Zero;

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (Method);
			sb.Append (':');

			bool first = true;
			foreach (KeyValuePair<string,string> prop in Properties) {
				if (first)
					first = false;
				else
					sb.Append (',');

				sb.Append (prop.Key);
				sb.Append ('=');
				sb.Append (Escape (prop.Value));
			}

			if (GUID != UUID.Zero) {
				if (Properties.Count != 0)
					sb.Append (',');
				sb.Append ("guid");
				sb.Append ('=');
				sb.Append (GUID.ToString ());
			}

			return sb.ToString ();
		}

		static string Escape (string str)
		{
			if (str == null)
				return String.Empty;

			StringBuilder sb = new StringBuilder ();
			int len = str.Length;

			for (int i = 0 ; i != len ; i++) {
				char c = str[i];

				//everything other than the optionally escaped chars _must_ be escaped
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
				    || c == '-' || c == '_' || c == '/' || c == '\\' || c == '.')
					sb.Append (c);
				else
					sb.Append (Uri.HexEscape (c));
			}

			return sb.ToString ();
		}

		static string Unescape (string str)
		{
			if (str == null)
				return String.Empty;

			StringBuilder sb = new StringBuilder ();
			int len = str.Length;
			int i = 0;
			while (i != len) {
				if (Uri.IsHexEncoding (str, i))
					sb.Append (Uri.HexUnescape (str, ref i));
				else
					sb.Append (str[i++]);
			}

			return sb.ToString ();
		}


		public static AddressEntry Parse (string s)
		{
			AddressEntry entry = new AddressEntry ();

			string[] parts = s.Split (':');

			if (parts.Length < 2)
				throw new InvalidAddressException ("No colon found");
			if (parts.Length > 2)
				throw new InvalidAddressException ("Too many colons found");

			entry.Method = parts[0];
         
			if (parts[1].Length > 0) {
				foreach (string propStr in parts[1].Split (',')) {
					parts = propStr.Split ('=');

					if (parts.Length < 2)
						throw new InvalidAddressException ("No equals sign found");
					if (parts.Length > 2)
						throw new InvalidAddressException ("Too many equals signs found");

					if (parts[0] == "guid") {
						try {
							entry.GUID = UUID.Parse (parts[1]);
						} catch {
							throw new InvalidAddressException ("Invalid guid specified");
						}
						continue;
					}

					entry.Properties[parts[0]] = Unescape (parts[1]);
				}
			}

			return entry;
		}
	}
}

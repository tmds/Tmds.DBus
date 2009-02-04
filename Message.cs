// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace NDesk.DBus
{
	class Message
	{
		public Message ()
		{
			Header.Endianness = Connection.NativeEndianness;
			Header.MessageType = MessageType.MethodCall;
			Header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			Header.MajorVersion = Protocol.Version;
			Header.Fields = new Dictionary<FieldCode,object> ();
		}

		public Header Header;

		public Connection Connection;

		public Signature Signature
		{
			get {
				object o;
				if (Header.Fields.TryGetValue (FieldCode.Signature, out o))
					return (Signature)o;
				else
					return Signature.Empty;
			} set {
				if (value == Signature.Empty)
					Header.Fields.Remove (FieldCode.Signature);
				else
					Header.Fields[FieldCode.Signature] = value;
			}
		}

		public bool ReplyExpected
		{
			get {
				return (Header.Flags & HeaderFlag.NoReplyExpected) == HeaderFlag.None;
			} set {
				if (value)
					Header.Flags &= ~HeaderFlag.NoReplyExpected; //flag off
				else
					Header.Flags |= HeaderFlag.NoReplyExpected; //flag on
			}
		}

		//public HeaderField[] HeaderFields;
		//public Dictionary<FieldCode,object>;

		public byte[] Body;

		//TODO: make use of Locked
		/*
		protected bool locked = false;
		public bool Locked
		{
			get {
				return locked;
			}
		}
		*/

		public void SetHeaderData (byte[] data)
		{
			EndianFlag endianness = (EndianFlag)data[0];
			MessageReader reader = new MessageReader (endianness, data);

			Header = (Header)reader.ReadStruct (typeof (Header));
		}

		public byte[] GetHeaderData ()
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);
			writer.WriteValueType (Header, typeof (Header));
			writer.CloseWrite ();

			return writer.ToArray ();
		}
	}

	class MessageDumper
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

			Message msg = new Message ();
			msg.SetHeaderData (header);
			msg.Body = body;

			return msg;
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

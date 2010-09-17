// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class Message
	{
		static System.Reflection.MethodInfo hHandler = typeof (Message).GetMethod ("HandleHeader");

		Header header = new Header ();
		Connection connection;
		byte[] body;

		public Message ()
		{
			header.Endianness = Connection.NativeEndianness;
			header.MessageType = MessageType.MethodCall;
			header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			header.MajorVersion = ProtocolInformations.Version;
			header.Fields = new Dictionary<byte, object> ();
		}

		public static Message FromReceivedBytes (Connection connection, byte[] header, byte[] body)
		{
			Message message = new Message ();
			message.connection = connection;
			message.body = body;
			message.SetHeaderData (header);

			return message;
		}

		public byte[] Body {
			get {
				return body;
			}
		}

		public Header Header {
			get {
				return header;
			}
		}

		public Connection Connection {
			get {
				return connection;
			}
		}

		public Signature Signature {
			get {
				object o = Header[FieldCode.Signature];
				if (o == null)
					return Signature.Empty;
				else
					return (Signature)o;
			}
			set {
				if (value == Signature.Empty)
					Header[FieldCode.Signature] = null;
				else
					Header[FieldCode.Signature] = value;
			}
		}

		public bool ReplyExpected {
			get {
				return (Header.Flags & HeaderFlag.NoReplyExpected) == HeaderFlag.None;
			}
			set {
				if (value)
					Header.Flags &= ~HeaderFlag.NoReplyExpected; //flag off
				else
					Header.Flags |= HeaderFlag.NoReplyExpected; //flag on
			}
		}

		public void AttachBodyTo (MessageWriter writer)
		{
			body = writer.ToArray ();
		}

		public void HandleHeader (Header headerIn)
		{
			header = headerIn;
		}

		public void SetHeaderData (byte[] data)
		{
			EndianFlag endianness = (EndianFlag)data[0];
			MessageReader reader = new MessageReader (endianness, data);

			MethodCaller2 mCaller = ExportObject.GetMCaller (hHandler);
			mCaller (this, reader, null, new MessageWriter ());
		}

		public byte[] GetHeaderData ()
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);
			WriteHeaderToMessage (writer);
			return writer.ToArray ();
		}

		public void GetHeaderDataToStream (Stream stream)
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);
			WriteHeaderToMessage (writer);
			writer.ToStream (stream);
		}

		void WriteHeaderToMessage (MessageWriter writer)
		{
			writer.Write ((byte)Header.Endianness);
			writer.Write ((byte)Header.MessageType);
			writer.Write ((byte)Header.Flags);
			writer.Write (Header.MajorVersion);
			writer.Write (Header.Length);
			writer.Write (Header.Serial);
			writer.WriteHeaderFields (Header.Fields);

			writer.CloseWrite ();
		}
	}
}

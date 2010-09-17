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
		public Message ()
		{
			Header.Endianness = Connection.NativeEndianness;
			Header.MessageType = MessageType.MethodCall;
			Header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			Header.MajorVersion = Protocol.Version;
			Header.Fields = new Dictionary<byte, object> ();
		}

		public Header Header = new Header ();

		public Connection Connection;

		public Signature Signature
		{
			get {
				object o = Header[FieldCode.Signature];
				if (o == null)
					return Signature.Empty;
				else
					return (Signature)o;
			} set {
				if (value == Signature.Empty)
					Header[FieldCode.Signature] = null;
				else
					Header[FieldCode.Signature] = value;
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

		public void HandleHeader (Header headerIn)
		{
			Header = headerIn;
		}

		static System.Reflection.MethodInfo hHandler = typeof (Message).GetMethod ("HandleHeader");
		public void SetHeaderData (byte[] data)
		{
			EndianFlag endianness = (EndianFlag)data[0];
			MessageReader reader = new MessageReader (endianness, data);

			MethodCaller2 mCaller = ExportObject.GetMCaller (hHandler);
			mCaller (this, reader, null, new MessageWriter ());
		}

		//public HeaderField[] Fields;

		/*
		public void SetHeaderData (byte[] data)
		{
			EndianFlag endianness = (EndianFlag)data[0];
			MessageReader reader = new MessageReader (endianness, data);

			Header = (Header)reader.ReadStruct (typeof (Header));
		}
		*/

		//TypeWriter<Header> headerWriter = TypeImplementer.GetTypeWriter<Header> ();
		public byte[] GetHeaderData ()
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);

			//writer.stream.Capacity = 512;
			//headerWriter (writer, Header);

			writer.Write ((byte)Header.Endianness);
			writer.Write ((byte)Header.MessageType);
			writer.Write ((byte)Header.Flags);
			writer.Write (Header.MajorVersion);
			writer.Write (Header.Length);
			writer.Write (Header.Serial);
			writer.WriteHeaderFields (Header.Fields);

			writer.CloseWrite ();

			return writer.ToArray ();
		}

		public void GetHeaderDataToStream (Stream stream)
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);

			//headerWriter (writer, Header);

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

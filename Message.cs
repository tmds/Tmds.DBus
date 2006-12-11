// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace NDesk.DBus
{
	public class Message
	{
		public Message ()
		{
			Header.Endianness = Connection.NativeEndianness;
			Header.MessageType = MessageType.MethodCall;
			//hdr->Flags = HeaderFlag.None;
			Header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			Header.MajorVersion = Protocol.Version;
			Header.Length = 0;
			//Header.Serial = conn.GenerateSerial ();
			Header.Fields = new Dictionary<FieldCode,object> ();
		}

		public Header Header;
		public byte[] HeaderData;

		public Connection Connection;

		public Signature Signature
		{
			get {
				if (Header.Fields.ContainsKey (FieldCode.Signature))
					return (Signature)Header.Fields[FieldCode.Signature];
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
		protected bool locked = false;
		public bool Locked
		{
			get {
				return locked;
			}
		}

		public void ParseHeader ()
		{
			//GetValue (stream, typeof (Header), out Header);

			EndianFlag endianness = (EndianFlag)HeaderData[0];
			MessageReader reader = new MessageReader (endianness, HeaderData);

			object valT;
			reader.GetValueStruct (typeof (Header), out valT);
			Header = (Header)valT;

			/*
			//foreach (HeaderField field in HeaderFields)
			foreach (KeyValuePair<FieldCode,object> field in Header.Fields)
			{
				//Console.WriteLine (field.Key + " = " + field.Value);
				switch (field.Key)
				{
					case FieldCode.Invalid:
						break;
					case FieldCode.Path:
						Path = (ObjectPath)field.Value;
						break;
					case FieldCode.Interface:
						Interface = (string)field.Value;
						break;
					case FieldCode.Member:
						Member = (string)field.Value;
						break;
					case FieldCode.ErrorName:
						ErrorName = (string)field.Value;
						break;
					case FieldCode.ReplySerial:
						ReplySerial = (uint)field.Value;
						break;
					case FieldCode.Destination:
						Destination = (string)field.Value;
						break;
					case FieldCode.Sender:
						Sender = (string)field.Value;
						break;
					case FieldCode.Signature:
						Signature = (Signature)field.Value;
						break;
				}
			}
			*/
		}

		public void WriteHeader ()
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Connection.NativeEndianness);
			writer.WriteStruct (typeof (Header), Header);
			//writer.WriteFromDict (typeof (FieldCode), typeof (object), Header.Fields);
			writer.CloseWrite ();
			HeaderData = writer.ToArray ();
		}
	}
}

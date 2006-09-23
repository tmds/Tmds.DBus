// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

//for filling padding with random
//using System.Security.Cryptography;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	public class Message
	{
		public Message ()
		{
			Header.Endianness = EndianFlag.Little;
			Header.MessageType = MessageType.MethodCall;
			//hdr->Flags = HeaderFlag.None;
			Header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			Header.MajorVersion = 1;
			Header.Length = 0;
			//Header.Serial = conn.GenerateSerial ();
			Header.Fields = new Dictionary<FieldCode,object> ();
		}

		public Header Header;
		public byte[] HeaderData;

		public Signature Signature
		{
			get {
				if (Header.Fields.ContainsKey (FieldCode.Signature))
					return (Signature)Header.Fields[FieldCode.Signature];
				else
					return new Signature ("");
			} set {
				//TODO: remove from dict if value empty or null
				Header.Fields[FieldCode.Signature] = value;
			}
		}

		//FIXME: hacked to work for the common cases since bit logic is broken
		public bool ReplyExpected
		{
			get {
				//return (Header.Flags & HeaderFlag.NoReplyExpected) != HeaderFlag.NoReplyExpected;
				return (Header.Flags != HeaderFlag.NoReplyExpected);
			} set {
				if (value)
					//Header.Flags &= ~HeaderFlag.NoReplyExpected; //flag off
					Header.Flags = HeaderFlag.None; //flag off
				else
					//Header.Flags |= ~HeaderFlag.NoReplyExpected; //flag on
					Header.Flags = HeaderFlag.NoReplyExpected; //flag on
			}
		}

		//public HeaderField[] HeaderFields;
		//public Dictionary<FieldCode,object>;

		//public MemoryStream Body;
		public byte[] Body;
		//public int DataSize;

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

			MessageReader reader = new MessageReader (HeaderData);

			ValueType valT;
			reader.GetValue (typeof (Header), out valT);
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
				//FIXME
				//Header.Length = (uint)Body.Position;
				Header.Length = (uint)Body.Length;

			//pad the end of the message body
			//this could be done elsewhere
			//MessageStream.CloseWrite (Body);

			MessageWriter writer = new MessageWriter ();

			writer.Write (typeof (Header), Header);
			//writer.WriteFromDict (typeof (FieldCode), typeof (object), Header.Fields);
			writer.CloseWrite ();

			HeaderData = writer.ToArray ();
		}
	}
}

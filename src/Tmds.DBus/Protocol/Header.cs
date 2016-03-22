// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Collections.Generic;

namespace DBus.Protocol
{
	public class Header
	{
		public EndianFlag Endianness;
		public MessageType MessageType;
		public HeaderFlag Flags;
		public byte MajorVersion;
		public uint Length;
		public uint Serial;

		Dictionary<byte, object> fields = new Dictionary<byte, object> ();

		public object this[FieldCode key]
		{
			get {
				object value = null;
				fields.TryGetValue ((byte)key, out value);
				return value;
			}
			set {
				if (value == null)
					fields.Remove((byte)key);
				else
					fields[(byte)key] = value;
			}
		}

		public bool TryGetField (FieldCode code, out object value)
		{
			return fields.TryGetValue ((byte)code, out value);
		}

		public int FieldsCount {
			get {
				return fields.Count;
			}
		}

		public static Header FromBytes (byte[] data)
		{
			Header header = new Header ();
			EndianFlag endianness = (EndianFlag)data[0];

			header.Endianness = endianness;
			header.MessageType = (MessageType)data[1];
			header.Flags = (HeaderFlag)data[2];
			header.MajorVersion = data[3];

			var reader = new MessageReader (endianness, data);
			reader.Seek (4);
			header.Length = reader.ReadUInt32 ();
			header.Serial = reader.ReadUInt32 ();

			FieldCodeEntry[] fields = reader.ReadArray<FieldCodeEntry> ();
			foreach (var f in fields) {
				header[(FieldCode)f.Code] = f.Value;
			}

			return header;
		}

		public void GetHeaderDataToStream (Stream stream)
		{
			MessageWriter writer = new MessageWriter (Endianness);
			WriteHeaderToMessage (writer);
			writer.ToStream (stream);
		}

		internal void WriteHeaderToMessage (MessageWriter writer)
		{
			writer.Write ((byte)Endianness);
			writer.Write ((byte)MessageType);
			writer.Write ((byte)Flags);
			writer.Write (MajorVersion);
			writer.Write (Length);
			writer.Write (Serial);
			writer.WriteHeaderFields (fields);

			writer.CloseWrite ();
		}

		struct FieldCodeEntry
		{
			public byte Code;
			public object Value;
		}

		/*
		public static DType TypeForField (FieldCode f)
		{
			switch (f) {
				case FieldCode.Invalid:
					return DType.Invalid;
				case FieldCode.Path:
					return DType.ObjectPath;
				case FieldCode.Interface:
					return DType.String;
				case FieldCode.Member:
					return DType.String;
				case FieldCode.ErrorName:
					return DType.String;
				case FieldCode.ReplySerial:
					return DType.UInt32;
				case FieldCode.Destination:
					return DType.String;
				case FieldCode.Sender:
					return DType.String;
				case FieldCode.Signature:
					return DType.Signature;
#if PROTO_REPLY_SIGNATURE
				case FieldCode.ReplySignature: //note: not supported in dbus
					return DType.Signature;
#endif
				default:
					return DType.Invalid;
			}
		}
		*/
	}
}

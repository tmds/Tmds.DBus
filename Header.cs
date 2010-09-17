// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
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
		//public HeaderField[] Fields;

		// Dictionary keyed by Enum has performance issues on .NET
		// So we key by byte and use an indexer instead.
		public Dictionary<byte, object> Fields;
		public object this[FieldCode key]
		{
			get
			{
				object value = null;
				Fields.TryGetValue ((byte)key, out value);
				return value;
			} set {
				if (value == null)
					Fields.Remove((byte)key);
				else
					Fields[(byte)key] = value;
			}
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

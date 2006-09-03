// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NDesk.DBus
{
	//yyyyuua{yv}
	[StructLayout (LayoutKind.Sequential, Pack=1)]
	public struct Header
	{
		public EndianFlag Endianness;
		public MessageType MessageType;
		public HeaderFlag Flags;
		public byte MajorVersion;
		public uint Length;
		public uint Serial;
		//public HeaderField[] Fields;
		public IDictionary<FieldCode,object> Fields;

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
				default:
					return DType.Invalid;
			}
		}
	}

	/*
	public struct HeaderField
	{
		//public HeaderField (FieldCode code, object value)
		//{
		//	this.Code = code;
		//	this.Value = value;
		//}

		public static HeaderField Create (FieldCode code, object value)
		{
			HeaderField hf;

			hf.Code = code;
			hf.Value = value;

			return hf;
		}

		public FieldCode Code;
		public object Value;
	}
	*/

	public enum MessageType : byte
	{
		//This is an invalid type.
		Invalid,
		//Method call.
		MethodCall,
		//Method reply with returned data.
		MethodReturn,
		//Error reply. If the first argument exists and is a string, it is an error message.
		Error,
		//Signal emission.
		Signal,
	}

	public enum FieldCode : byte
	{
		Invalid,
			Path,
			Interface,
			Member,
			ErrorName,
			ReplySerial,
			Destination,
			Sender,
			Signature,
	}

	public enum EndianFlag : byte
	{
		Little = (byte)'l',
		Big = (byte)'B',
	}

	[Flags]
	public enum HeaderFlag : byte
	{
		None = 0,
		NoReplyExpected = 0x1,
		NoAutoStart = 0x2,
	}

	public struct ObjectPath
	{
		public static ObjectPath Root = new ObjectPath ("/");

		public string Value;

		public ObjectPath (string value)
		{
			this.Value = value;
		}

		public override string ToString ()
		{
			return Value;
		}
	}

	public static class Padding
	{
		public static int GetAlignment (DType dtype)
		{
			switch (dtype) {
				case DType.Byte:
					return 1;
				case DType.Boolean:
					return 4;
				case DType.Int16:
				case DType.UInt16:
					return 2;
				case DType.Int32:
				case DType.UInt32:
					return 4;
				case DType.Int64:
				case DType.UInt64:
					return 8;
				case DType.Single: //Not yet supported!
					return 4;
				case DType.Double:
					return 8;
				case DType.String:
					return 4;
				case DType.ObjectPath:
					return 4;
				case DType.Signature:
					return 1;
				case DType.Array:
					return 4;
				case DType.Struct:
					return 8;
				case DType.Variant:
					return 1;
				case DType.DictEntry:
					return 8;
				case DType.Invalid:
				default:
					throw new Exception ("Cannot determine alignment of " + dtype);
			}
		}
	}
}

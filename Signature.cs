// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Runtime.InteropServices;

namespace NDesk.DBus
{
	public struct Signature
	{
		//TODO: this class needs some work

		public Signature (string value)
		{
			this.Value = value;
		}

		public Signature (params DType[] value)
		{
			string val = "";

			foreach (DType t in value)
				val += (char)t;

			this.Value = val;
		}

		//DType[] Value;
		public string Value;

		public override string ToString ()
		{
			//return Value;

			byte[] ts = (byte[])System.Text.Encoding.ASCII.GetBytes (Value);

			string ret = "";

			foreach (DType t in ts)
				ret += t.ToString () + " ";

			return ret;
		}

		public static DType TypeToDType (Type type)
		{
			if (type == null)
				return DType.Invalid;
			else if (type == typeof (byte))
				return DType.Byte;
			else if (type == typeof (bool))
				return DType.Boolean;
			else if (type == typeof (short))
				return DType.Int16;
			else if (type == typeof (ushort))
				return DType.UInt16;
			else if (type == typeof (int))
				return DType.Int32;
			else if (type == typeof (uint))
				return DType.UInt32;
			else if (type == typeof (long))
				return DType.Int64;
			else if (type == typeof (ulong))
				return DType.UInt64;
			else if (type == typeof (float)) //FIXME: this isn't supported by DBus yet
				return DType.Float;
			else if (type == typeof (double))
				return DType.Double;
			else if (type == typeof (string))
				return DType.String;
			else if (type == typeof (ObjectPath))
				return DType.ObjectPath;
			else if (type == typeof (Signature))
				return DType.Signature;
			else
				return DType.Invalid;
		}

		public static Type DTypeToType (DType dtype)
		{
			switch (dtype) {
				case DType.Invalid:
					return null;
				case DType.Byte:
					return typeof (byte);
				case DType.Boolean:
					return typeof (bool);
				case DType.Int16:
					return typeof (short);
				case DType.UInt16:
					return typeof (ushort);
				case DType.Int32:
					return typeof (int);
				case DType.UInt32:
					return typeof (uint);
				case DType.Int64:
					return typeof (long);
				case DType.UInt64:
					return typeof (ulong);
				case DType.Float: //Not yet supported!
					return typeof (float);
				case DType.Double:
					return typeof (double);
				case DType.String:
					return typeof (string);
				case DType.ObjectPath:
					return typeof (ObjectPath);
				case DType.Signature:
					return typeof (Signature);
		/*
		Array
		Struct
		DictEntry
		Variant
		*/
				default:
					return null;
			}
		}
	}

	public enum DType : byte
	{
		Invalid = (byte)'\0',

		Byte = (byte)'y',
		Boolean = (byte)'b',
		Int16 = (byte)'n',
		UInt16 = (byte)'q',
		Int32 = (byte)'i',
		UInt32 = (byte)'u',
		Int64 = (byte)'x',
		UInt64 = (byte)'t',
		Float = (byte)'f', //This is not yet supported!
		Double = (byte)'d',
		String = (byte)'s',
		ObjectPath = (byte)'o',
		Signature = (byte)'g',

		Array = (byte)'a',
		Struct = (byte)'r',
		DictEntry = (byte)'e',
		Variant = (byte)'v'
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//defined by default, since this is not a controversial extension
#define PROTO_TYPE_SINGLE

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NDesk.DBus
{
	public class MessageReader
	{
		protected EndianFlag endianness;
		protected byte[] data;
		//TODO: this should be uint or long to handle long messages
		protected int pos = 0;
		protected Message message;

		public MessageReader (EndianFlag endianness, byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			this.endianness = endianness;
			this.data = data;
		}

		public MessageReader (Message message) : this (message.Header.Endianness, message.Body)
		{
			if (message == null)
				throw new ArgumentNullException ("message");

			this.message = message;
		}

		public void GetValue (Type type, out object val)
		{
			if (type == typeof (void)) {
				val = null;
				return;
			}

			if (type.IsArray) {
				Array valArr;
				GetValue (type, out valArr);
				val = valArr;
			} else if (type == typeof (ObjectPath)) {
				ObjectPath valOP;
				GetValue (out valOP);
				val = valOP;
			} else if (type == typeof (Signature)) {
				Signature valSig;
				GetValue (out valSig);
				val = valSig;
			} else if (type == typeof (object)) {
				GetValue (out val);
			} else if (type == typeof (string)) {
				string valStr;
				GetValue (out valStr);
				val = valStr;
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IDictionary<,>)) {
				Type[] genArgs = type.GetGenericArguments ();
				Type dictType = typeof (Dictionary<,>).MakeGenericType (genArgs);
				val = Activator.CreateInstance(dictType, new object[0]);
				System.Collections.IDictionary idict = (System.Collections.IDictionary)val;
				GetValueToDict (genArgs[0], genArgs[1], idict);
			} else if (Mapper.IsPublic (type)) {
				GetObject (type, out val);
			} else if (!type.IsPrimitive && !type.IsEnum) {
				GetValueStruct (type, out val);
			} else {
				DType dtype = Signature.TypeToDType (type);
				GetValue (dtype, out val);
			}

			if (type.IsEnum)
				val = Enum.ToObject (type, val);
		}

		//helper method, should not be used generally
		public void GetValue (DType dtype, out object val)
		{
			switch (dtype)
			{
				case DType.Byte:
				{
					byte vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.Boolean:
				{
					bool vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.Int16:
				{
					short vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.UInt16:
				{
					ushort vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.Int32:
				{
					int vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.UInt32:
				{
					uint vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.Int64:
				{
					long vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.UInt64:
				{
					ulong vval;
					GetValue (out vval);
					val = vval;
				}
				break;
#if PROTO_TYPE_SINGLE
				case DType.Single:
				{
					float vval;
					GetValue (out vval);
					val = vval;
				}
				break;
#endif
				case DType.Double:
				{
					double vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.String:
				{
					string vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.ObjectPath:
				{
					ObjectPath vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.Signature:
				{
					Signature vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				case DType.Variant:
				{
					object vval;
					GetValue (out vval);
					val = vval;
				}
				break;
				default:
				val = null;
				throw new Exception ("Unhandled D-Bus type: " + dtype);
			}
		}

		public void GetObject (Type type, out object val)
		{
			ObjectPath path;
			GetValue (out path);
			val = message.Connection.GetObject (type, (string)message.Header.Fields[FieldCode.Sender], path);
		}

		public void GetValue (out byte val)
		{
			val = data[pos++];
		}

		public void GetValue (out bool val)
		{
			uint intval;
			GetValue (out intval);

			switch (intval) {
				case 0:
					val = false;
					break;
				case 1:
					val = true;
					break;
				default:
					throw new Exception ("Read value " + intval + " at position " + pos + " while expecting boolean (0/1)");
			}
		}

		unsafe protected void MarshalUShort (byte *dst)
		{
			ReadPad (2);

			if (endianness == Connection.NativeEndianness) {
				dst[0] = data[pos + 0];
				dst[1] = data[pos + 1];
			} else {
				dst[0] = data[pos + 1];
				dst[1] = data[pos + 0];
			}

			pos += 2;
		}

		unsafe public void GetValue (out short val)
		{
			fixed (short* ret = &val)
				MarshalUShort ((byte*)ret);
		}

		unsafe public void GetValue (out ushort val)
		{
			fixed (ushort* ret = &val)
				MarshalUShort ((byte*)ret);
		}

		unsafe protected void MarshalUInt (byte *dst)
		{
			ReadPad (4);

			if (endianness == Connection.NativeEndianness) {
				dst[0] = data[pos + 0];
				dst[1] = data[pos + 1];
				dst[2] = data[pos + 2];
				dst[3] = data[pos + 3];
			} else {
				dst[0] = data[pos + 3];
				dst[1] = data[pos + 2];
				dst[2] = data[pos + 1];
				dst[3] = data[pos + 0];
			}

			pos += 4;
		}

		unsafe public void GetValue (out int val)
		{
			fixed (int* ret = &val)
				MarshalUInt ((byte*)ret);
		}

		unsafe public void GetValue (out uint val)
		{
			fixed (uint* ret = &val)
				MarshalUInt ((byte*)ret);
		}

		unsafe protected void MarshalULong (byte *dst)
		{
			ReadPad (8);

			if (endianness == Connection.NativeEndianness) {
				for (int i = 0; i < 8; ++i)
					dst[i] = data[pos + i];
			} else {
				for (int i = 0; i < 8; ++i)
					dst[i] = data[pos + (7 - i)];
			}

			pos += 8;
		}

		unsafe public void GetValue (out long val)
		{
			fixed (long* ret = &val)
				MarshalULong ((byte*)ret);
		}

		unsafe public void GetValue (out ulong val)
		{
			fixed (ulong* ret = &val)
				MarshalULong ((byte*)ret);
		}

#if PROTO_TYPE_SINGLE
		unsafe public void GetValue (out float val)
		{
			fixed (float* ret = &val)
				MarshalUInt ((byte*)ret);
		}
#endif

		unsafe public void GetValue (out double val)
		{
			fixed (double* ret = &val)
				MarshalULong ((byte*)ret);
		}

		public void GetValue (out string val)
		{
			uint ln;
			GetValue (out ln);

			val = Encoding.UTF8.GetString (data, pos, (int)ln);
			pos += (int)ln;
			ReadNull ();
		}

		public void GetValue (out ObjectPath val)
		{
			//exactly the same as string
			string sval;
			GetValue (out sval);
			val = new ObjectPath (sval);
		}

		public void GetValue (out Signature val)
		{
			byte ln;
			GetValue (out ln);

			byte[] sigData = new byte[ln];
			Array.Copy (data, pos, sigData, 0, (int)ln);
			val = new Signature (sigData);
			pos += (int)ln;
			ReadNull ();
		}

		//variant
		public void GetValue (out object val)
		{
			Signature sig;
			GetValue (out sig);

			GetValue (sig, out val);
		}

		public void GetValue (Signature sig, out object val)
		{
			GetValue (sig.ToType (), out val);
		}

		//not pretty or efficient but works
		public void GetValueToDict (Type keyType, Type valType, System.Collections.IDictionary val)
		{
			uint ln;
			GetValue (out ln);

			//advance to the alignment of the element
			//ReadPad (Protocol.GetAlignment (Signature.TypeToDType (type)));
			ReadPad (8);

			int endPos = pos + (int)ln;

			//while (stream.Position != endPos)
			while (pos < endPos)
			{
				ReadPad (8);

				object keyVal;
				GetValue (keyType, out keyVal);

				object valVal;
				GetValue (valType, out valVal);

				val.Add (keyVal, valVal);
			}

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);
		}

		//this could be made generic to avoid boxing
		public void GetValue (Type type, out Array val)
		{
			Type elemType = type.GetElementType ();

			uint ln;
			GetValue (out ln);

			//advance to the alignment of the element
			ReadPad (Protocol.GetAlignment (Signature.TypeToDType (elemType)));

			int endPos = pos + (int)ln;

			//List<T> vals = new List<T> ();
			System.Collections.ArrayList vals = new System.Collections.ArrayList ();

			//while (stream.Position != endPos)
			while (pos < endPos)
			{
				object elem;
				//GetValue (Signature.TypeToDType (elemType), out elem);
				GetValue (elemType, out elem);
				vals.Add (elem);
			}

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);

			val = vals.ToArray (elemType);
			//val = Array.CreateInstance (elemType.UnderlyingSystemType, vals.Count);
		}

		//struct
		//probably the wrong place for this
		//there might be more elegant solutions
		public void GetValueStruct (Type type, out object val)
		{
			ReadPad (8);

			val = Activator.CreateInstance (type);

			/*
			if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (KeyValuePair<,>)) {
				object elem;

				System.Reflection.PropertyInfo key_prop = type.GetProperty ("Key");
				GetValue (key_prop.PropertyType, out elem);
				key_prop.SetValue (val, elem, null);

				System.Reflection.PropertyInfo val_prop = type.GetProperty ("Value");
				GetValue (val_prop.PropertyType, out elem);
				val_prop.SetValue (val, elem, null);

				return;
			}
			*/

			FieldInfo[] fis = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (System.Reflection.FieldInfo fi in fis) {
				object elem;
				//GetValue (Signature.TypeToDType (fi.FieldType), out elem);
				GetValue (fi.FieldType, out elem);
				//public virtual void SetValueDirect (TypedReference obj, object value);
				fi.SetValue (val, elem);
			}
		}

		public void ReadNull ()
		{
			if (data[pos] != 0)
				throw new Exception ("Read non-zero byte at position " + pos + " while expecting null terminator");
			pos++;
		}

		/*
		public void ReadPad (int alignment)
		{
			pos = Protocol.Padded (pos, alignment);
		}
		*/

		public void ReadPad (int alignment)
		{
			for (int endPos = Protocol.Padded (pos, alignment) ; pos != endPos ; pos++)
				if (data[pos] != 0)
					throw new Exception ("Read non-zero byte at position " + pos + " while expecting padding");
		}
	}
}

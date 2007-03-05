// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace NDesk.DBus
{
	class MessageReader
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
				val = ReadObjectPath ();
			} else if (type == typeof (Signature)) {
				val = ReadSignature ();
			} else if (type == typeof (object)) {
				val = ReadVariant ();
			} else if (type == typeof (string)) {
				val = ReadString ();
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IDictionary<,>)) {
				Type[] genArgs = type.GetGenericArguments ();
				//Type dictType = typeof (Dictionary<,>).MakeGenericType (genArgs);
				//workaround for Mono bug #81035 (memory leak)
				Type dictType = Mapper.GetGenericType (typeof (Dictionary<,>), genArgs);
				val = Activator.CreateInstance(dictType, new object[0]);
				System.Collections.IDictionary idict = (System.Collections.IDictionary)val;
				GetValueToDict (genArgs[0], genArgs[1], idict);
			} else if (Mapper.IsPublic (type)) {
				GetObject (type, out val);
			} else if (!type.IsPrimitive && !type.IsEnum) {
				GetValueStruct (type, out val);
			} else {
				DType dtype = Signature.TypeToDType (type);
				val = ReadValue (dtype);

				if (type.IsEnum)
					val = Enum.ToObject (type, val);
			}
		}

		//helper method, should not be used generally
		public object ReadValue (DType dtype)
		{
			switch (dtype)
			{
				case DType.Byte:
					return ReadByte ();

				case DType.Boolean:
					return ReadBoolean ();

				case DType.Int16:
					return ReadInt16 ();

				case DType.UInt16:
					return ReadUInt16 ();

				case DType.Int32:
					return ReadInt32 ();

				case DType.UInt32:
					return ReadUInt32 ();

				case DType.Int64:
					return ReadInt64 ();

				case DType.UInt64:
					return ReadUInt64 ();

#if !DISABLE_SINGLE
				case DType.Single:
					return ReadSingle ();
#endif

				case DType.Double:
					return ReadDouble ();

				case DType.String:
					return ReadString ();

				case DType.ObjectPath:
					return ReadObjectPath ();

				case DType.Signature:
					return ReadSignature ();

				case DType.Variant:
					return ReadVariant ();

				default:
					throw new Exception ("Unhandled D-Bus type: " + dtype);
			}
		}

		public void GetObject (Type type, out object val)
		{
			ObjectPath path = ReadObjectPath ();

			val = message.Connection.GetObject (type, (string)message.Header.Fields[FieldCode.Sender], path);
		}

		public byte ReadByte ()
		{
			return data[pos++];
		}

		public bool ReadBoolean ()
		{
			uint intval = ReadUInt32 ();

			switch (intval) {
				case 0:
					return false;
				case 1:
					return true;
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

		unsafe public short ReadInt16 ()
		{
			short val;

			MarshalUShort ((byte*)&val);

			return val;
		}

		unsafe public ushort ReadUInt16 ()
		{
			ushort val;

			MarshalUShort ((byte*)&val);

			return val;
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

		unsafe public int ReadInt32 ()
		{
			int val;

			MarshalUInt ((byte*)&val);

			return val;
		}

		unsafe public uint ReadUInt32 ()
		{
			uint val;

			MarshalUInt ((byte*)&val);

			return val;
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

		unsafe public long ReadInt64 ()
		{
			long val;

			MarshalULong ((byte*)&val);

			return val;
		}

		unsafe public ulong ReadUInt64 ()
		{
			ulong val;

			MarshalULong ((byte*)&val);

			return val;
		}

#if !DISABLE_SINGLE
		unsafe public float ReadSingle ()
		{
			float val;

			MarshalUInt ((byte*)&val);

			return val;
		}
#endif

		unsafe public double ReadDouble ()
		{
			double val;

			MarshalULong ((byte*)&val);

			return val;
		}

		public string ReadString ()
		{
			uint ln = ReadUInt32 ();

			string val = Encoding.UTF8.GetString (data, pos, (int)ln);
			pos += (int)ln;
			ReadNull ();

			return val;
		}

		public ObjectPath ReadObjectPath ()
		{
			//exactly the same as string
			return new ObjectPath (ReadString ());
		}

		public Signature ReadSignature ()
		{
			byte ln = ReadByte ();

			byte[] sigData = new byte[ln];
			Array.Copy (data, pos, sigData, 0, (int)ln);
			pos += (int)ln;
			ReadNull ();

			return new Signature (sigData);
		}

		public object ReadVariant ()
		{
			return ReadVariant (ReadSignature ());
		}

		object ReadVariant (Signature sig)
		{
			object val;

			GetValue (sig.ToType (), out val);

			return val;
		}

		//not pretty or efficient but works
		public void GetValueToDict (Type keyType, Type valType, System.Collections.IDictionary val)
		{
			uint ln = ReadUInt32 ();

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

			uint ln = ReadUInt32 ();

			//TODO: more fast paths for primitive arrays
			if (elemType == typeof (byte)) {
				byte[] valb = new byte[ln];
				Array.Copy (data, pos, valb, 0, (int)ln);
				val = valb;
				pos += (int)ln;
				return;
			}

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

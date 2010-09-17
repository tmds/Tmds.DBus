// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DBus.Protocol
{
	public class MessageReader
	{
		protected EndianFlag endianness;
		//protected byte[] data;
		public byte[] data;
		//TODO: this should be uint or long to handle long messages
		//internal int pos = 0;
		public int pos = 0;
		protected Message message;

		public MessageReader (EndianFlag endianness, byte[] data)
		{
			//if (data == null)
			//	throw new ArgumentNullException ("data");
			if (data == null)
				data = new byte[0];

			this.endianness = endianness;
			this.IsNativeEndian = endianness == Connection.NativeEndianness;
			this.data = data;
		}

		public MessageReader (Message message) : this (message.Header.Endianness, message.Body)
		{
			if (message == null)
				throw new ArgumentNullException ("message");

			this.message = message;
		}

		public readonly bool IsNativeEndian;

		public object ReadValue (Type type)
		{
			if (type == typeof (void))
				return null;

			if (type.IsArray) {
				return ReadArray (type.GetElementType ());
			} else if (type == typeof (ObjectPath)) {
				return ReadObjectPath ();
			} else if (type == typeof (Signature)) {
				return ReadSignature ();
			} else if (type == typeof (object)) {
				return ReadVariant ();
			} else if (type == typeof (string)) {
				return ReadString ();
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IDictionary<,>)) {
				Type[] genArgs = type.GetGenericArguments ();
				//Type dictType = typeof (Dictionary<,>).MakeGenericType (genArgs);
				//workaround for Mono bug #81035 (memory leak)
				Type dictType = Mapper.GetGenericType (typeof (Dictionary<,>), genArgs);
				System.Collections.IDictionary idict = (System.Collections.IDictionary)Activator.CreateInstance(dictType, new object[0]);
				GetValueToDict (genArgs[0], genArgs[1], idict);
				return idict;
			} else if (Mapper.IsPublic (type)) {
				return GetObject (type);
			} else if (!type.IsPrimitive && !type.IsEnum) {
				return ReadStruct (type);
			} else {
				object val;
				DType dtype = Signature.TypeToDType (type);
				val = ReadValue (dtype);

				if (type.IsEnum)
					val = Enum.ToObject (type, val);

				return val;
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

		public object GetObject (Type type)
		{
			ObjectPath path = ReadObjectPath ();

			return message.Connection.GetObject (type, (string)message.Header[FieldCode.Sender], path);
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

		unsafe protected void MarshalUShort (void* dstPtr)
		{
			ReadPad (2);

			if (data.Length < pos + 2)
				throw new Exception ("Cannot read beyond end of data");

			if (endianness == Connection.NativeEndianness) {
				fixed (byte* p = &data[pos])
					*((ushort*)dstPtr) = *((ushort*)p);
			} else {
				byte* dst = (byte*)dstPtr;
				dst[0] = data[pos + 1];
				dst[1] = data[pos + 0];
			}

			pos += 2;
		}

		unsafe public short ReadInt16 ()
		{
			short val;

			MarshalUShort (&val);

			return val;
		}

		unsafe public ushort ReadUInt16 ()
		{
			ushort val;

			MarshalUShort (&val);

			return val;
		}

		unsafe protected void MarshalUInt (void* dstPtr)
		{
			ReadPad (4);

			if (data.Length < pos + 4)
				throw new Exception ("Cannot read beyond end of data");

			if (endianness == Connection.NativeEndianness) {
				fixed (byte* p = &data[pos])
					*((uint*)dstPtr) = *((uint*)p);
			} else {
				byte* dst = (byte*)dstPtr;
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

			MarshalUInt (&val);

			return val;
		}

		unsafe public uint ReadUInt32 ()
		{
			uint val;

			MarshalUInt (&val);

			return val;
		}

		unsafe protected void MarshalULong (void* dstPtr)
		{
			ReadPad (8);

			if (data.Length < pos + 8)
				throw new Exception ("Cannot read beyond end of data");

			if (endianness == Connection.NativeEndianness) {
				fixed (byte* p = &data[pos])
					*((ulong*)dstPtr) = *((ulong*)p);
			} else {
				byte* dst = (byte*)dstPtr;
				for (int i = 0; i < 8; ++i)
					dst[i] = data[pos + (7 - i)];
			}

			pos += 8;
		}

		unsafe public long ReadInt64 ()
		{
			long val;

			MarshalULong (&val);

			return val;
		}

		unsafe public ulong ReadUInt64 ()
		{
			ulong val;

			MarshalULong (&val);

			return val;
		}

#if !DISABLE_SINGLE
		unsafe public float ReadSingle ()
		{
			float val;

			MarshalUInt (&val);

			return val;
		}
#endif

		unsafe public double ReadDouble ()
		{
			double val;

			MarshalULong (&val);

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

			// Avoid an array allocation for small signatures
			if (ln == 1) {
				DType dtype = (DType)ReadByte ();
				ReadNull ();
				return new Signature (dtype);
			}

			if (ln > ProtocolInformations.MaxSignatureLength)
				throw new Exception ("Signature length " + ln + " exceeds maximum allowed " + ProtocolInformations.MaxSignatureLength + " bytes");

			byte[] sigData = new byte[ln];
			Array.Copy (data, pos, sigData, 0, (int)ln);
			pos += (int)ln;
			ReadNull ();

			return Signature.Take (sigData);
		}

		public object ReadVariant ()
		{
			return ReadValue (ReadSignature ());
		}

		// Used primarily for reading variant values
		object ReadValue (Signature sig)
		{
			if (sig.IsPrimitive)
				return ReadValue (sig[0]);

			Type t = null;
			try {
				t = sig.ToType ();
			} catch (NotSupportedException e) {
				// We don't catch other exceptions as they indicate a malformed signature
				if (ProtocolInformations.Verbose)
					Console.Error.WriteLine (e.Message);
			}

			/*
			if (t == null) {
				StepOver (sig);
				return null;
			}
			*/

			if (t == null) {
				ReadPad (sig.Alignment);
				int startPos = pos;
				StepOver (sig);
				int ln = pos - startPos;

				DValue dv = new DValue();
				dv.endianness = endianness;
				dv.signature = sig;
				dv.data = new byte[ln];
				Array.Copy (data, startPos, dv.data, 0, ln);
				return dv;
			}

			return ReadValue (t);
		}

		//not pretty or efficient but works
		public void GetValueToDict (Type keyType, Type valType, System.Collections.IDictionary val)
		{
			uint ln = ReadUInt32 ();

			if (ln > ProtocolInformations.MaxArrayLength)
				throw new Exception ("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformations.MaxArrayLength + " bytes");

			//advance to the alignment of the element
			//ReadPad (Protocol.GetAlignment (Signature.TypeToDType (type)));
			ReadPad (8);

			int endPos = pos + (int)ln;

			//while (stream.Position != endPos)
			while (pos < endPos)
			{
				ReadPad (8);

				val.Add (ReadValue (keyType), ReadValue (valType));
			}

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);
		}

		//this could be made generic to avoid boxing
		public Array ReadArray (Type elemType)
		{
			uint ln = ReadUInt32 ();

			if (ln > Protocol.MaxArrayLength)
				throw new Exception ("Array length " + ln + " exceeds maximum allowed " + Protocol.MaxArrayLength + " bytes");

			//TODO: more fast paths for primitive arrays
			if (elemType == typeof (byte)) {
				byte[] valb = new byte[ln];
				Array.Copy (data, pos, valb, 0, (int)ln);
				pos += (int)ln;
				return valb;
			}

			//advance to the alignment of the element
			ReadPad (Protocol.GetAlignment (Signature.TypeToDType (elemType)));

			int endPos = pos + (int)ln;

			//List<T> vals = new List<T> ();
			System.Collections.ArrayList vals = new System.Collections.ArrayList ();

			//while (stream.Position != endPos)
			while (pos < endPos)
				vals.Add (ReadValue (elemType));

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);

			return vals.ToArray (elemType);
		}

		//struct
		//probably the wrong place for this
		//there might be more elegant solutions
		public object ReadStruct (Type type)
		{
			ReadPad (8);

			object val = Activator.CreateInstance (type);

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

			foreach (System.Reflection.FieldInfo fi in fis)
				fi.SetValue (val, ReadValue (fi.FieldType));

			return val;
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

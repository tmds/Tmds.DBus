// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//defined by default, since this is not a controversial extension
#define PROTO_TYPE_SINGLE

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace NDesk.DBus
{
	public class MessageReader
	{
		//FIXME: use endianness instead of failing on non-native endianness
		protected EndianFlag endianness;
		protected byte[] data;
		//TODO: this should be uint or long to handle long messages
		protected int pos = 0;

		public MessageReader (EndianFlag endianness, byte[] data)
		{
			if (endianness != Connection.NativeEndianness)
				throw new NotImplementedException ("Only native-endian message reading is currently supported");

			if (data == null)
				throw new ArgumentNullException ("data");

			this.endianness = endianness;
			this.data = data;
		}

		public MessageReader (Message message) : this (message.Header.Endianness, message.Body)
		{
			if (message == null)
				throw new ArgumentNullException ("message");
		}

		public void CloseRead ()
		{
			ReadPad (8);
			//this needs more thought
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
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IDictionary<,>)) {
				Type[] genArgs = type.GetGenericArguments ();
				Type dictType = typeof (Dictionary<,>).MakeGenericType (genArgs);
				val = Activator.CreateInstance(dictType, new object[0]);
				System.Collections.IDictionary idict = (System.Collections.IDictionary)val;
				GetValueToDict (genArgs[0], genArgs[1], idict);
				/*
			} else if (type == typeof (ObjectPath)) {
				//FIXME: find a better way of specifying structs that must be marshaled by value and not as a struct like ObjectPath
				//this is just a quick proof of concept fix
				//TODO: are there others we should special case in this hack?
				//TODO: this code has analogues elsewhere that need both this quick fix, and the real fix when it becomes available
				DType dtype = Signature.TypeToDType (type);
				GetValue (dtype, out val);
				*/
			} else if (!type.IsPrimitive && type.IsValueType && !type.IsEnum) {
				ValueType valV;
				GetValue (type, out valV);
				val = valV;
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

		/*
		public void GetValue (out byte val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (1);
			val = br.ReadByte ();
		}

		public void GetValue (out bool val)
		{
			uint intval;
			GetValue (out intval);

			//TODO: confirm semantics of dbus boolean
			val = intval == 0 ? false : true;
		}

		public void GetValue (out short val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (2);
			val = br.ReadInt16 ();
		}

		public void GetValue (out ushort val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (2);
			val = br.ReadUInt16 ();
		}

		public void GetValue (out int val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (4);
			val = br.ReadInt32 ();
		}

		public void GetValue (out uint val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (4);
			val = br.ReadUInt32 ();
		}

		public void GetValue (out long val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (8);
			val = br.ReadInt64 ();
		}

		public void GetValue (out ulong val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (8);
			val = br.ReadUInt64 ();
		}

#if PROTO_TYPE_SINGLE
		public void GetValue (out float val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (4);
			val = br.ReadSingle ();
		}
#endif

		public void GetValue (out double val)
		{
			BinaryReader br = new BinaryReader (stream);

			ReadPad (8);
			val = br.ReadDouble ();
		}

		public void GetValue (out string val)
		{
			BinaryReader br = new BinaryReader (stream);

			uint ln;
			GetValue (out ln);

			byte[] rbytes = br.ReadBytes ((int)ln);
			val = Encoding.UTF8.GetString (rbytes);
			br.ReadByte (); //null string terminator
		}

		public void GetValue (out ObjectPath val)
		{
			//exactly the same as string
			GetValue (out val.Value);
		}

		public void GetValue (out Signature val)
		{
			BinaryReader br = new BinaryReader (stream);

			//ReadPad (1); //alignment for signature is 1
			byte ln;
			GetValue (out ln);

			val.Data = br.ReadBytes ((int)ln);
			br.ReadByte (); //null signature terminator
		}
		*/

		//alternative GetValue() implementations
		//needed for reading messages in machine-native format, until we do this properly
		//TODO: don't ignore the endian flag in the header

		public void GetValue (out byte val)
		{
			val = data[pos++];
		}

		public void GetValue (out bool val)
		{
			uint intval;
			GetValue (out intval);

			//TODO: confirm semantics of dbus boolean
			val = intval == 0 ? false : true;
		}

		public void GetValue (out short val)
		{
			ReadPad (2);
			val = BitConverter.ToInt16 (data, pos);
			pos += 2;
		}

		public void GetValue (out ushort val)
		{
			ReadPad (2);
			val = BitConverter.ToUInt16 (data, pos);
			pos += 2;
		}

		public void GetValue (out int val)
		{
			ReadPad (4);
			val = BitConverter.ToInt32 (data, pos);
			pos += 4;
		}

		public void GetValue (out uint val)
		{
			ReadPad (4);
			val = BitConverter.ToUInt32 (data, pos);
			pos += 4;
		}

		public void GetValue (out long val)
		{
			ReadPad (8);
			val = BitConverter.ToInt64 (data, pos);
			pos += 8;
		}

		public void GetValue (out ulong val)
		{
			ReadPad (8);
			val = BitConverter.ToUInt64 (data, pos);
			pos += 8;
		}

#if PROTO_TYPE_SINGLE
		public void GetValue (out float val)
		{
			ReadPad (4);
			val = BitConverter.ToSingle (data, pos);
			pos += 4;
		}
#endif

		public void GetValue (out double val)
		{
			ReadPad (8);
			val = BitConverter.ToDouble (data, pos);
			pos += 8;
		}

		public void GetValue (out string val)
		{
			uint ln;
			GetValue (out ln);

			val = Encoding.UTF8.GetString (data, pos, (int)ln);
			pos += (int)ln + 1; //+1 is null string terminator
		}

		public void GetValue (out ObjectPath val)
		{
			//exactly the same as string
			GetValue (out val.Value);
		}

		public void GetValue (out Signature val)
		{
			byte ln;
			GetValue (out ln);

			byte[] sigData = new byte[ln];
			Array.Copy (data, pos, sigData, 0, (int)ln);
			val = new Signature (sigData);
			pos += (int)ln + 1; //+1 is null signature terminator
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
			if (sig == Signature.Empty) {
				val = null;
				return;
			}

			if (sig.Length == 1) {
				GetValue (sig[0], out val);
				return;
			}

			if (sig.Length == 2 && sig[0] == DType.Array) {
				DType elem_t = sig[1];
				Type elem_type = Signature.DTypeToType (elem_t);
				Type array_type = elem_type.MakeArrayType ();
				GetValue (array_type, out val);
				return;
			}

			//TODO: more flexibilty needed here

			throw new NotSupportedException ("Reading variants with more than one primitive value or primitive array is not supported (signature of variant was '" + sig + "')");
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
		//restricted to primitive elements because of the DType bottleneck
		public void GetValue (Type type, out Array val)
		{
			if (type.IsArray)
			type = type.GetElementType ();

			uint ln;
			GetValue (out ln);

			//advance to the alignment of the element
			ReadPad (Protocol.GetAlignment (Signature.TypeToDType (type)));

			int endPos = pos + (int)ln;

			//List<T> vals = new List<T> ();
			System.Collections.ArrayList vals = new System.Collections.ArrayList ();

			//while (stream.Position != endPos)
			while (pos < endPos)
			{
				object elem;
				//GetValue (Signature.TypeToDType (type), out elem);
				GetValue (type, out elem);
				vals.Add (elem);
			}

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);

			val = vals.ToArray (type);
			//val = Array.CreateInstance (type.UnderlyingSystemType, vals.Count);
		}

		//struct
		//probably the wrong place for this
		//there might be more elegant solutions
		public void GetValue (Type type, out ValueType val)
		{
			System.Reflection.ConstructorInfo[] cis = type.GetConstructors ();
			if (cis.Length != 0) {
				System.Reflection.ConstructorInfo ci = cis[0];
				//Console.WriteLine ("ci: " + ci);
				System.Reflection.ParameterInfo[]  parms = ci.GetParameters ();

				/*
				Type[] sig = new Type[parms.Length];
				for (int i = 0 ; i != parms.Length ; i++)
					sig[i] = parms[i].ParameterType;
				object retObj = ci.Invoke (null, MessageHelper.GetDynamicValues (msg, sig));
				*/

				//TODO: use MessageHelper.GetDynamicValues() when it's refactored to be applicable
				/*
				object[] vals;
				vals = MessageHelper.GetDynamicValues (msg, parms);
				*/

				List<object> vals = new List<object> (parms.Length);
				foreach (System.Reflection.ParameterInfo parm in parms) {
					object arg;
					GetValue (parm.ParameterType, out arg);
					vals.Add (arg);
				}

				//object retObj = ci.Invoke (val, vals.ToArray ());
				val = (ValueType)Activator.CreateInstance (type, vals.ToArray ());
				return;
			}

			//no suitable ctor, marshal as a struct
			ReadPad (8);

			val = (ValueType)Activator.CreateInstance (type);

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

			System.Reflection.FieldInfo[] fis = type.GetFields ();

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
			if (data[pos++] != 0)
				throw new Exception ("Read non-zero null terminator");
		}

		/*
		public void ReadPad (int alignment)
		{
			pos = Protocol.Padded (pos, alignment);
		}
		*/

		public void ReadPad (int alignment)
		{
			//make sure the pos-1 is right for the exception message when we have a test suite
			while (pos != Protocol.Padded (pos, alignment))
				if (data[pos++] != 0)
					throw new Exception ("Read non-zero padding byte at pos " + (pos-1) + ", pad value was " + data[pos-1]);
		}
	}
}

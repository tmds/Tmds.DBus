// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace NDesk.DBus
{
	public class MessageReader
	{
		protected Stream stream;

		public MessageReader (byte[] body)
		{
			stream = new MemoryStream (body, false);
		}

		public MessageReader (Message msg) : this (msg.Body)
		{
		}

		public void CloseRead ()
		{
			ReadPad (8);
			//this needs more thought
		}

		public void GetValue (Type type, out object val)
		{
			if (type.IsArray) {
				Array valArr;
				GetValue (type, out valArr);
				val = valArr;
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
			val = System.Text.Encoding.UTF8.GetString (rbytes);
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
			val = (byte)stream.ReadByte ();
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
			byte[] buf = new byte[2];
			stream.Read (buf, 0, 2);
			val = BitConverter.ToInt16 (buf, 0);
		}

		public void GetValue (out ushort val)
		{
			ReadPad (2);
			byte[] buf = new byte[2];
			stream.Read (buf, 0, 2);
			val = BitConverter.ToUInt16 (buf, 0);
		}

		public void GetValue (out int val)
		{
			ReadPad (4);
			byte[] buf = new byte[4];
			stream.Read (buf, 0, 4);
			val = BitConverter.ToInt32 (buf, 0);
		}

		public void GetValue (out uint val)
		{
			ReadPad (4);
			byte[] buf = new byte[4];
			stream.Read (buf, 0, 4);
			val = BitConverter.ToUInt32 (buf, 0);
		}

		public void GetValue (out long val)
		{
			ReadPad (8);
			byte[] buf = new byte[8];
			stream.Read (buf, 0, 8);
			val = BitConverter.ToInt64 (buf, 0);
		}

		public void GetValue (out ulong val)
		{
			ReadPad (8);
			byte[] buf = new byte[8];
			stream.Read (buf, 0, 8);
			val = BitConverter.ToUInt64 (buf, 0);
		}

#if PROTO_TYPE_SINGLE
		public void GetValue (out float val)
		{
			ReadPad (4);
			byte[] buf = new byte[4];
			stream.Read (buf, 0, 4);
			val = BitConverter.ToSingle (buf, 0);
		}
#endif

		public void GetValue (out double val)
		{
			ReadPad (8);
			byte[] buf = new byte[8];
			stream.Read (buf, 0, 8);
			val = BitConverter.ToDouble (buf, 0);
		}

		public void GetValue (out string val)
		{
			uint ln;
			GetValue (out ln);

			byte[] buf = new byte[(int)ln];
			stream.Read (buf, 0, (int)ln);
			val = System.Text.Encoding.UTF8.GetString (buf);
			stream.ReadByte (); //null string terminator
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

			byte[] buf = new byte[ln];
			stream.Read (buf, 0, ln);
			val.Data = buf;
			stream.ReadByte (); //null signature terminator
		}

		//variant
		public void GetValue (out object val)
		{
			Signature sig;
			GetValue (out sig);
			//TODO: more flexibilty needed here
			DType t = (DType)sig.Data[0];
			//Console.WriteLine ("var type " + t);
			GetValue (t, out val);
		}

		//not pretty or efficient but works
		public void GetValueToDict (Type keyType, Type valType, System.Collections.IDictionary val)
		{
			uint ln;
			GetValue (out ln);

			//advance to the alignment of the element
			//ReadPad (Padding.GetAlignment (Signature.TypeToDType (type)));
			ReadPad (8);

			int endPos = (int)stream.Position + (int)ln;

			//while (stream.Position != endPos)
			while (stream.Position < endPos)
			{
				ReadPad (8);

				object keyVal;
				GetValue (keyType, out keyVal);

				object valVal;
				GetValue (valType, out valVal);

				val.Add (keyVal, valVal);
			}

			if (stream.Position != endPos)
				throw new Exception ("Read pos " + stream.Position + " != ep " + endPos);
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
			ReadPad (Padding.GetAlignment (Signature.TypeToDType (type)));

			int endPos = (int)stream.Position + (int)ln;

			//List<T> vals = new List<T> ();
			System.Collections.ArrayList vals = new System.Collections.ArrayList ();

			//while (stream.Position != endPos)
			while (stream.Position < endPos)
			{
				object elem;
				//GetValue (Signature.TypeToDType (type), out elem);
				GetValue (type, out elem);
				vals.Add (elem);
			}

			if (stream.Position != endPos)
				throw new Exception ("Read pos " + stream.Position + " != ep " + endPos);

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
				object retObj = ci.Invoke (null, GetDynamicValues (msg, sig));
				*/

				//TODO: use GetDynamicValues() when it's refactored to be applicable
				/*
				object[] vals;
				vals = GetDynamicValues (msg, parms);
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

		public void ReadPad (int alignment)
		{
			//byte[] pad = new byte[8];
			//rng.GetNonZeroBytes (pad);
			//stream.Position = Padding.Padded ((int)stream.Position, alignment);

			//int end = Padding.Padded ((int)stream.Position, alignment);

			int len = Padding.PadNeeded ((int)stream.Position, alignment);
			for (int i = 0 ; i != len ; i++) {
				int b = stream.ReadByte ();
				if (b != 0)
					throw new Exception ("Read non-zero padding byte at pos " + i + " (offset " + stream.Position + "), pad value was " + b);
			}
		}
	}
}

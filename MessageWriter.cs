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
	public class MessageWriter
	{
		//TODO: use endianness instead of writing the message is in native format
		protected EndianFlag endianness;
		protected MemoryStream stream;
		protected BinaryWriter bw;

		//TODO: enable this ctor instead of the current one when endian support is done
		//public MessageWriter () : this (Connection.NativeEndianness)
		public MessageWriter () : this (EndianFlag.Little)
		{
		}

		public MessageWriter (EndianFlag endianness)
		{
			if (endianness != EndianFlag.Little)
				throw new NotImplementedException ("Only little-endian message writing is currently supported");

			this.endianness = endianness;
			stream = new MemoryStream ();
			bw = new BinaryWriter (stream);
		}

		public byte[] ToArray ()
		{
			//TODO: mark the writer locked or something here
			return stream.ToArray ();
		}

		public void CloseWrite ()
		{
			int needed = Protocol.PadNeeded ((int)stream.Position, 8);
			for (int i = 0 ; i != needed ; i++)
				stream.WriteByte (0);
		}

		public void Write (byte val)
		{
			WritePad (1);
			bw.Write (val);
		}

		public void Write (bool val)
		{
			WritePad (4);
			bw.Write ((uint) (val ? 1 : 0));
		}

		public void Write (short val)
		{
			WritePad (2);
			bw.Write (val);
		}

		public void Write (ushort val)
		{
			WritePad (2);
			bw.Write (val);
		}

		public void Write (int val)
		{
			WritePad (4);
			bw.Write (val);
		}

		public void Write (uint val)
		{

			WritePad (4);
			bw.Write (val);
		}

		public void Write (long val)
		{
			WritePad (8);
			bw.Write (val);
		}

		public void Write (ulong val)
		{
			WritePad (8);
			bw.Write (val);
		}

#if PROTO_TYPE_SINGLE
		public void Write (float val)
		{
			WritePad (4);
			bw.Write (val);
		}
#endif

		public void Write (double val)
		{
			WritePad (8);
			bw.Write (val);
		}

		public void Write (string val)
		{
			byte[] utf8_data = Encoding.UTF8.GetBytes (val);
			Write ((uint)utf8_data.Length);
			bw.Write (utf8_data);
			bw.Write ((byte)0); //NULL string terminator
		}

		public void Write (ObjectPath val)
		{
			Write (val.Value);
		}

		public void Write (Signature val)
		{
			WritePad (1);
			Write ((byte)val.Length);
			bw.Write (val.GetBuffer ());
			bw.Write ((byte)0); //NULL signature terminator
		}

		public void Write (Type type, object val)
		{
			if (type == typeof (void))
				return;

			if (type.IsArray) {
				Write (type, (Array)val);
			} else if (type == typeof (ObjectPath)) {
				Write ((ObjectPath)val);
			} else if (type == typeof (Signature)) {
				Write ((Signature)val);
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();
				System.Collections.IDictionary idict = (System.Collections.IDictionary)val;
				WriteFromDict (genArgs[0], genArgs[1], idict);
			} else if (!type.IsPrimitive && type.IsValueType && !type.IsEnum) {
				Write (type, (ValueType)val);
				/*
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (Nullable<>)) {
				//is it possible to support nullable types?
				Type[] genArgs = type.GetGenericArguments ();
				WriteVariant (genArgs[0], val);
				*/
			} else {
				Write (Signature.TypeToDType (type), val);
			}
		}

		//helper method, should not be used as it boxes needlessly
		public void Write (DType dtype, object val)
		{
			switch (dtype)
			{
				case DType.Byte:
				{
					Write ((byte)val);
				}
				break;
				case DType.Boolean:
				{
					Write ((bool)val);
				}
				break;
				case DType.Int16:
				{
					Write ((short)val);
				}
				break;
				case DType.UInt16:
				{
					Write ((ushort)val);
				}
				break;
				case DType.Int32:
				{
					Write ((int)val);
				}
				break;
				case DType.UInt32:
				{
					Write ((uint)val);
				}
				break;
				case DType.Int64:
				{
					Write ((long)val);
				}
				break;
				case DType.UInt64:
				{
					Write ((ulong)val);
				}
				break;
#if PROTO_TYPE_SINGLE
				case DType.Single:
				{
					Write ((float)val);
				}
				break;
#endif
				case DType.Double:
				{
					Write ((double)val);
				}
				break;
				case DType.String:
				{
					Write ((string)val);
				}
				break;
				case DType.ObjectPath:
				{
					Write ((ObjectPath)val);
				}
				break;
				case DType.Signature:
				{
					Write ((Signature)val);
				}
				break;
				case DType.Variant:
				{
					Write ((object)val);
				}
				break;
				default:
				throw new Exception ("Unhandled D-Bus type: " + dtype);
			}
		}

		//variant
		public void Write (object val)
		{
			//TODO: maybe support sending null variants

			if (val == null)
				throw new NotSupportedException ("Cannot send null variant");

			Type type = val.GetType ();

			WriteVariant (type, val);
		}

		public void WriteVariant (Type type, object val)
		{
			Signature sig = Signature.GetSig (type);

			Write (sig);
			Write (type, val);
		}

		//this requires a seekable stream for now
		public void Write (Type type, Array val)
		{
			//if (type.IsArray)
			type = type.GetElementType ();

			Write ((uint)0);
			long lengthPos = stream.Position - 4;

			//advance to the alignment of the element
			WritePad (Protocol.GetAlignment (Signature.TypeToDType (type)));

			long startPos = stream.Position;

			foreach (object elem in val)
				Write (type, elem);

			long endPos = stream.Position;

			stream.Position = lengthPos;
			Write ((uint)(endPos - startPos));

			stream.Position = endPos;
		}

		public void WriteFromDict (Type keyType, Type valType, System.Collections.IDictionary val)
		{
			Write ((uint)0);
			long lengthPos = stream.Position - 4;

			//advance to the alignment of the element
			//WritePad (Protocol.GetAlignment (Signature.TypeToDType (type)));
			WritePad (8);

			long startPos = stream.Position;

			foreach (System.Collections.DictionaryEntry entry in val)
			{
				WritePad (8);

				Write (keyType, entry.Key);
				Write (valType, entry.Value);
			}

			long endPos = stream.Position;

			stream.Position = lengthPos;
			Write ((uint)(endPos - startPos));

			stream.Position = endPos;
		}

		public void Write (Type type, ValueType val)
		{
			WritePad (8); //offset for structs, right?

			/*
			ConstructorInfo[] cis = type.GetConstructors ();
			if (cis.Length != 0) {
				System.Reflection.ParameterInfo[]  parms = ci.GetParameters ();

				foreach (ParameterInfo parm in parms) {
				}
			}
			*/
			if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (KeyValuePair<,>)) {
				System.Reflection.PropertyInfo key_prop = type.GetProperty ("Key");
				Write (key_prop.PropertyType, key_prop.GetValue (val, null));

				System.Reflection.PropertyInfo val_prop = type.GetProperty ("Value");
				Write (val_prop.PropertyType, val_prop.GetValue (val, null));

				return;
			}

			System.Reflection.FieldInfo[] fis = type.GetFields ();

			foreach (System.Reflection.FieldInfo fi in fis) {
				object elem;
				//public virtual object GetValueDirect (TypedReference obj);
				elem = fi.GetValue (val);
				//Write (Signature.TypeToDType (fi.FieldType), elem);
				Write (fi.FieldType, elem);
			}
		}

		//RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider ();
		public void WritePad (int alignment)
		{
			stream.Position = Protocol.Padded ((int)stream.Position, alignment);
		}
	}
}

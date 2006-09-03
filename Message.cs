// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

//for filling padding with random
//using System.Security.Cryptography;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	public class Message
	{
		public Message ()
		{
			Header.Endianness = EndianFlag.Little;
			Header.MessageType = MessageType.MethodCall;
			//hdr->Flags = HeaderFlag.None;
			Header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			Header.MajorVersion = 1;
			Header.Length = 0;
			//Header.Serial = conn.GenerateSerial ();
			Header.Fields = new Dictionary<FieldCode,object> ();
		}

		//maybe better to do this in Wrapper.cs
		/*
		public static Message MethodCall (ObjectPath path, string @interface, string member, string destination)
		{
			Message message = new Message ();

			message.Header.MessageType = MessageType.MethodCall;
			message.ReplyExpected = false;
			message.Header.Fields[FieldCode.Path] = path;
			message.Header.Fields[FieldCode.Interface] = @interface;
			message.Header.Fields[FieldCode.Member] = member;
			message.Header.Fields[FieldCode.Destination] = destination;

			return message;
		}
		*/

		public Header Header;
		public byte[] HeaderData;

		public Signature Signature
		{
			get {
				if (Header.Fields.ContainsKey (FieldCode.Signature))
					return (Signature)Header.Fields[FieldCode.Signature];
				else
					return new Signature ("");
			} set {
				//TODO: remove from dict if value empty or null
				Header.Fields[FieldCode.Signature] = value;
			}
		}

		//FIXME: hacked to work for the common cases since bit logic is broken
		public bool ReplyExpected
		{
			get {
				//return (Header.Flags & HeaderFlag.NoReplyExpected) != HeaderFlag.NoReplyExpected;
				return (Header.Flags != HeaderFlag.NoReplyExpected);
			} set {
				if (value)
					//Header.Flags &= ~HeaderFlag.NoReplyExpected; //flag off
					Header.Flags = HeaderFlag.None; //flag off
				else
					//Header.Flags |= ~HeaderFlag.NoReplyExpected; //flag on
					Header.Flags = HeaderFlag.NoReplyExpected; //flag on
			}
		}

		//public HeaderField[] HeaderFields;
		//public Dictionary<FieldCode,object>;

		public MemoryStream Body;
		//public byte[] Data;
		//public int DataSize;

		public bool Locked;

		public static void Close (Stream stream)
		{
			Pad (stream, 8);
			//this needs more thought
		}

		public static void CloseWrite (Stream stream)
		{
			int needed = PadNeeded ((int)stream.Position, 8);
			for (int i = 0 ; i != needed ; i++)
				stream.WriteByte (0);
		}

		public static void Write (Stream stream, byte val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 1);
			bw.Write (val);
		}

		public static void Write (Stream stream, bool val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 4);
			bw.Write ((uint) (val ? 1 : 0));
		}

		public static void Write (Stream stream, short val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 2);
			bw.Write (val);
		}

		public static void Write (Stream stream, ushort val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 2);
			bw.Write (val);
		}

		public static void Write (Stream stream, int val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 4);
			bw.Write (val);
		}

		public static void Write (Stream stream, uint val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 4);
			bw.Write (val);
		}

		public static void Write (Stream stream, long val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 8);
			bw.Write (val);
		}

		public static void Write (Stream stream, ulong val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 8);
			bw.Write (val);
		}

		public static void Write (Stream stream, float val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 4);
			bw.Write (val);
		}

		public static void Write (Stream stream, double val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 8);
			bw.Write (val);
		}

		public static void Write (Stream stream, string val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Write (stream, (uint)val.Length);

			bw.Write (System.Text.Encoding.UTF8.GetBytes (val));
			bw.Write ((byte)0); //NULL string terminator
		}

		public static void Write (Stream stream, ObjectPath val)
		{
			Write (stream, val.Value);
		}

		public static void Write (Stream stream, Signature val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 1);
			Write (stream, (byte)val.Value.Length);
			bw.Write (val.Data);
			bw.Write ((byte)0); //NULL signature terminator
		}

		public static void Write (Stream stream, Type type, object val)
		{
			if (type.IsArray) {
				Write (stream, type, (Array)val);
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();
				System.Collections.IDictionary idict = (System.Collections.IDictionary)val;
				WriteFromDict (stream, genArgs[0], genArgs[1], idict);
			} else if (!type.IsPrimitive && type.IsValueType && !type.IsEnum) {
				Write (stream, type, (ValueType)val);
			} else {
				Write (stream, Signature.TypeToDType (type), val);
			}
		}

		//helper method, should not be used as it boxes needlessly
		public static void Write (Stream stream, DType dtype, object val)
		{
			switch (dtype)
			{
				case DType.Byte:
				{
					Write (stream, (byte)val);
				}
				break;
				case DType.Boolean:
				{
					Write (stream, (bool)val);
				}
				break;
				case DType.Int16:
				{
					Write (stream, (short)val);
				}
				break;
				case DType.UInt16:
				{
					Write (stream, (ushort)val);
				}
				break;
				case DType.Int32:
				{
					Write (stream, (int)val);
				}
				break;
				case DType.UInt32:
				{
					Write (stream, (uint)val);
				}
				break;
				case DType.Int64:
				{
					Write (stream, (long)val);
				}
				break;
				case DType.UInt64:
				{
					Write (stream, (ulong)val);
				}
				break;
				case DType.Single:
				{
					Write (stream, (float)val);
				}
				break;
				case DType.Double:
				{
					Write (stream, (double)val);
				}
				break;
				case DType.String:
				{
					Write (stream, (string)val);
				}
				break;
				case DType.ObjectPath:
				{
					Write (stream, (ObjectPath)val);
				}
				break;
				case DType.Signature:
				{
					Write (stream, (Signature)val);
				}
				break;
				case DType.Variant:
				{
					Write (stream, (object)val);
				}
				break;
				default:
				throw new Exception ("Unhandled DBus type: " + dtype);
			}
		}

		//variant
		public static void Write (Stream stream, object val)
		{
			Type type = val.GetType ();
			DType t = Signature.TypeToDType (type);

			Signature sig = new Signature (t);
			Write (stream, sig);

			Write (stream, t, val);
		}

		public static void GetValue (Stream stream, Type type, out object val)
		{
			if (type.IsArray) {
				Array valArr;
				GetValue (stream, type, out valArr);
				val = valArr;
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IDictionary<,>)) {
				Type[] genArgs = type.GetGenericArguments ();
				Type dictType = typeof (Dictionary<,>).MakeGenericType (genArgs);
				val = Activator.CreateInstance(dictType, new object[0]);
				System.Collections.IDictionary idict = (System.Collections.IDictionary)val;
				GetValueToDict (stream, genArgs[0], genArgs[1], idict);
				/*
			} else if (type == typeof (ObjectPath)) {
				//FIXME: find a better way of specifying structs that must be marshaled by value and not as a struct like ObjectPath
				//this is just a quick proof of concept fix
				//TODO: are there others we should special case in this hack?
				//TODO: this code has analogues elsewhere that need both this quick fix, and the real fix when it becomes available
				DType dtype = Signature.TypeToDType (type);
				GetValue (stream, dtype, out val);
				*/
			} else if (!type.IsPrimitive && type.IsValueType && !type.IsEnum) {
				ValueType valV;
				GetValue (stream, type, out valV);
				val = valV;
			} else {
				DType dtype = Signature.TypeToDType (type);
				GetValue (stream, dtype, out val);
			}

			if (type.IsEnum)
				val = Enum.ToObject (type, val);
		}

		//helper method, should not be used generally
		public static void GetValue (Stream stream, DType dtype, out object val)
		{
			switch (dtype)
			{
				case DType.Byte:
				{
					byte vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Boolean:
				{
					bool vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Int16:
				{
					short vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.UInt16:
				{
					ushort vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Int32:
				{
					int vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.UInt32:
				{
					uint vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Int64:
				{
					long vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.UInt64:
				{
					ulong vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Single:
				{
					float vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Double:
				{
					double vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.String:
				{
					string vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.ObjectPath:
				{
					ObjectPath vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Signature:
				{
					Signature vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Variant:
				{
					object vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				default:
				val = null;
				throw new Exception ("Unhandled DBus type: " + dtype);
			}
		}

		public static void GetValue (Stream stream, out byte val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 1);
			val = br.ReadByte ();
		}

		public static void GetValue (Stream stream, out bool val)
		{
			uint intval;
			GetValue (stream, out intval);

			//TODO: confirm semantics of dbus boolean
			val = intval == 0 ? false : true;
		}

		public static void GetValue (Stream stream, out short val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 2);
			val = br.ReadInt16 ();
		}

		public static void GetValue (Stream stream, out ushort val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 2);
			val = br.ReadUInt16 ();
		}

		public static void GetValue (Stream stream, out int val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 4);
			val = br.ReadInt32 ();
		}

		public static void GetValue (Stream stream, out uint val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 4);
			val = br.ReadUInt32 ();
		}

		public static void GetValue (Stream stream, out long val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 8);
			val = br.ReadInt64 ();
		}

		public static void GetValue (Stream stream, out ulong val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 8);
			val = br.ReadUInt64 ();
		}

		public static void GetValue (Stream stream, out float val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 4);
			val = br.ReadSingle ();
		}

		public static void GetValue (Stream stream, out double val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 8);
			val = br.ReadDouble ();
		}

		public static void GetValue (Stream stream, out string val)
		{
			BinaryReader br = new BinaryReader (stream);

			uint ln;
			GetValue (stream, out ln);

			byte[] rbytes = br.ReadBytes ((int)ln);
			val = System.Text.Encoding.UTF8.GetString (rbytes);
			br.ReadByte (); //null string terminator
		}

		public static void GetValue (Stream stream, out ObjectPath val)
		{
			//exactly the same as string
			GetValue (stream, out val.Value);
		}

		public static void GetValue (Stream stream, out Signature val)
		{
			BinaryReader br = new BinaryReader (stream);

			//Pad (stream, 1); //alignment for signature is 1
			byte ln;
			GetValue (stream, out ln);

			val.Data = br.ReadBytes ((int)ln);
			br.ReadByte (); //null signature terminator
		}

		//variant
		public static void GetValue (Stream stream, out object val)
		{
			Signature sig;
			GetValue (stream, out sig);
			//TODO: more flexibilty needed here
			DType t = (DType)sig.Data[0];
			//Console.WriteLine ("var type " + t);
			GetValue (stream, t, out val);
		}

		//this requires a seekable stream for now
		public static void Write (Stream stream, Type type, Array val)
		{
			//if (type.IsArray)
			type = type.GetElementType ();

			Write (stream, (uint)0);
			long lengthPos = stream.Position - 4;

			//advance to the alignment of the element
			Pad (stream, Padding.GetAlignment (Signature.TypeToDType (type)));

			long startPos = stream.Position;

			foreach (object elem in val)
				Write (stream, type, elem);

			long endPos = stream.Position;

			stream.Position = lengthPos;
			Write (stream, (uint)(endPos - startPos));

			stream.Position = endPos;
		}

		public static void WriteFromDict (Stream stream, Type keyType, Type valType, System.Collections.IDictionary val)
		{
			Write (stream, (uint)0);
			long lengthPos = stream.Position - 4;

			//advance to the alignment of the element
			//Pad (stream, Padding.GetAlignment (Signature.TypeToDType (type)));
			Pad (stream, 8);

			long startPos = stream.Position;

			foreach (System.Collections.DictionaryEntry entry in val)
			{
				Pad (stream, 8);

				Write (stream, keyType, entry.Key);
				Write (stream, valType, entry.Value);
			}

			long endPos = stream.Position;

			stream.Position = lengthPos;
			Write (stream, (uint)(endPos - startPos));

			stream.Position = endPos;
		}

		//not pretty or efficient but works
		public static void GetValueToDict (Stream stream, Type keyType, Type valType, System.Collections.IDictionary val)
		{
			uint ln;
			GetValue (stream, out ln);

			//advance to the alignment of the element
			//Pad (stream, Padding.GetAlignment (Signature.TypeToDType (type)));
			Pad (stream, 8);

			int endPos = (int)stream.Position + (int)ln;

			//while (stream.Position != endPos)
			while (stream.Position < endPos)
			{
				Pad (stream, 8);

				object keyVal;
				GetValue (stream, keyType, out keyVal);

				object valVal;
				GetValue (stream, valType, out valVal);

				val.Add (keyVal, valVal);
			}

			if (stream.Position != endPos)
				throw new Exception ("Read pos " + stream.Position + " != ep " + endPos);
		}

		//this could be made generic to avoid boxing
		//restricted to primitive elements because of the DType bottleneck
		public static void GetValue (Stream stream, Type type, out Array val)
		{
			if (type.IsArray)
			type = type.GetElementType ();

			uint ln;
			GetValue (stream, out ln);

			//advance to the alignment of the element
			Pad (stream, Padding.GetAlignment (Signature.TypeToDType (type)));

			int endPos = (int)stream.Position + (int)ln;

			//List<T> vals = new List<T> ();
			System.Collections.ArrayList vals = new System.Collections.ArrayList ();

			//while (stream.Position != endPos)
			while (stream.Position < endPos)
			{
				object elem;
				//GetValue (stream, Signature.TypeToDType (type), out elem);
				GetValue (stream, type, out elem);
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
		public static void GetValue (Stream stream, Type type, out ValueType val)
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
					Message.GetValue (stream, parm.ParameterType, out arg);
					vals.Add (arg);
				}

				//object retObj = ci.Invoke (val, vals.ToArray ());
				val = (ValueType)Activator.CreateInstance (type, vals.ToArray ());
				return;
			}

			//no suitable ctor, marshal as a struct
			Pad (stream, 8);

			val = (ValueType)Activator.CreateInstance (type);

			/*
			if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (KeyValuePair<,>)) {
				object elem;

				System.Reflection.PropertyInfo key_prop = type.GetProperty ("Key");
				GetValue (stream, key_prop.PropertyType, out elem);
				key_prop.SetValue (val, elem, null);

				System.Reflection.PropertyInfo val_prop = type.GetProperty ("Value");
				GetValue (stream, val_prop.PropertyType, out elem);
				val_prop.SetValue (val, elem, null);

				return;
			}
			*/

			System.Reflection.FieldInfo[] fis = type.GetFields ();

			foreach (System.Reflection.FieldInfo fi in fis) {
				object elem;
				//GetValue (stream, Signature.TypeToDType (fi.FieldType), out elem);
				GetValue (stream, fi.FieldType, out elem);
				//public virtual void SetValueDirect (TypedReference obj, object value);
				fi.SetValue (val, elem);
			}
		}

		public static void Write (Stream stream, Type type, ValueType val)
		{
			Pad (stream, 8); //offset for structs, right?

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
				Write (stream, key_prop.PropertyType, key_prop.GetValue (val, null));

				System.Reflection.PropertyInfo val_prop = type.GetProperty ("Value");
				Write (stream, val_prop.PropertyType, val_prop.GetValue (val, null));

				return;
			}

			System.Reflection.FieldInfo[] fis = type.GetFields ();

			foreach (System.Reflection.FieldInfo fi in fis) {
				object elem;
				//public virtual object GetValueDirect (TypedReference obj);
				elem = fi.GetValue (val);
				//Write (stream, Signature.TypeToDType (fi.FieldType), elem);
				Write (stream, fi.FieldType, elem);
			}
		}

		/*
		public ObjectPath Path = new ObjectPath ("");
		public string Interface = "";
		public string Member = "";
		public string ErrorName = "";
		public uint ReplySerial = 0;
		public string Destination = "";
		public string Sender = "";
		public Signature Signature = new Signature ("");
		*/

		//only in values for MethodCall, only out valuess for MethodReturn?
		//public DType[] Signature;

		public void ParseHeader ()
		{
			//GetValue (stream, typeof (Header), out Header);

			MemoryStream stream = new MemoryStream (HeaderData);

			ValueType valT;
			GetValue (stream, typeof (Header), out valT);
			Header = (Header)valT;

			/*
			//foreach (HeaderField field in HeaderFields)
			foreach (KeyValuePair<FieldCode,object> field in Header.Fields)
			{
				//TODO: maybe make this more efficient, less ugly

				//Console.WriteLine (field.Key + " = " + field.Value);
				switch (field.Key)
				{
					case FieldCode.Invalid:
						break;
					case FieldCode.Path:
						Path = (ObjectPath)field.Value;
						break;
					case FieldCode.Interface:
						Interface = (string)field.Value;
						break;
					case FieldCode.Member:
						Member = (string)field.Value;
						break;
					case FieldCode.ErrorName:
						ErrorName = (string)field.Value;
						break;
					case FieldCode.ReplySerial:
						ReplySerial = (uint)field.Value;
						break;
					case FieldCode.Destination:
						Destination = (string)field.Value;
						break;
					case FieldCode.Sender:
						Sender = (string)field.Value;
						break;
					case FieldCode.Signature:
						Signature = (Signature)field.Value;
						break;
				}
			}
			*/
		}

		public void WriteHeader ()
		{
			if (Body != null)
				Header.Length = (uint)Body.Position;

			//pad the end of the message body
			//this could be done elsewhere
			//Message.CloseWrite (Body);

			MemoryStream stream = new MemoryStream ();
			Message.Write (stream, typeof (Header), Header);
			//WriteFromDict (stream, typeof (FieldCode), typeof (object), Header.Fields);
			Message.CloseWrite (stream);

			//HeaderData = stream.GetBuffer ();
			HeaderData = stream.ToArray ();
		}

		public static int PadNeeded (int len, int alignment)
		{
			int pad = len % alignment;
			pad = pad == 0 ? 0 : alignment - pad;

			return pad;
		}

		public static int Padded (int len, int alignment)
		{
			return len + PadNeeded (len, alignment);
		}

		//static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider ();
		public static void Pad (Stream stream, int alignment)
		{
			//byte[] pad = new byte[8];
			//rng.GetNonZeroBytes (pad);
			//stream.Position = Padded ((int)stream.Position, alignment);

			//int end = Padded ((int)stream.Position, alignment);
			if (!IsReading)
			{
				stream.Position = Padded ((int)stream.Position, alignment);
				return;
			}

			//BinaryReader br = new BinaryReader (stream);

			int len = PadNeeded ((int)stream.Position, alignment);
			for (int i = 0 ; i != len ; i++) {
				int b = stream.ReadByte ();
				if (b != 0)
					throw new Exception ("Read non-zero padding byte at pos " + i + " (offset " + stream.Position + "), pad value was " + b);
			}
		}

		//TODO: get rid of this hack
		public static bool IsReading = false;
	}
}

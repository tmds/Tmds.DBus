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
	public unsafe class Message
	{
		public DHeader* Header;

		public int HeaderSize;
		public byte[] HeaderData;

		public MessageType MessageType
		{
			get {
				return Header->MessageType;
			} set {
				Header->MessageType = value;
			}
		}

		public uint Serial
		{
			get {
				return Header->Serial;
			} set {
				Header->Serial = value;
			}
		}

		public bool ReplyExpected
		{
			get {
				return (Header->Flags & HeaderFlag.NoReplyExpected) != HeaderFlag.NoReplyExpected;
			} set {
				if (value)
					Header->Flags &= ~HeaderFlag.NoReplyExpected; //flag off
				else
					Header->Flags |= ~HeaderFlag.NoReplyExpected; //flag on
			}
		}

		public HeaderField[] HeaderFields;
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

		public static void Write (Stream stream, Signature val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 1);
			Write (stream, (byte)val.Value.Length);
			bw.Write (val.Data);
			bw.Write ((byte)0); //NULL signature terminator
		}

		public static void Write (Stream stream, ObjectPath val)
		{
			Write (stream, val.Value);
		}

		public static void Write (Stream stream, byte val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Pad (stream, 1);
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

		public static void Write (Stream stream, string val)
		{
			BinaryWriter bw = new BinaryWriter (stream);

			Write (stream, (uint)val.Length);

			bw.Write (System.Text.Encoding.UTF8.GetBytes (val));
			bw.Write ((byte)0); //NULL string terminator
		}

		/*
		public static void Write (Stream stream, HeaderField[] val)
		{
			//FIXME: write the proper length
			Write (stream, (uint)0);

			foreach (HeaderField item in val)
				Write (stream, item);
		}
		*/

		/*
		public static void Write (Stream stream, HeaderField[] val)
		{
			//inefficient, but good for now
			MemoryStream ms = new MemoryStream ();

			foreach (HeaderField item in val)
				Write (ms, item);

			Write (stream, (uint)ms.Position);

			//byte[] buf = ms.GetBuffer ();
			//stream.Write (buf, 0, (int)ms.Position);

			ms.WriteTo (stream);
		}
		*/

		/*
		public static void Write (Stream stream, HeaderField val)
		{
			Pad (stream, 8); //offset for structs, right?
			Write (stream, (byte)val.Code);
			Write (stream, val.Value);
		}
		*/

		public static void GetValue (Stream stream, out Signature val)
		{
			BinaryReader br = new BinaryReader (stream);

			//Pad (stream, 1); //TODO: alignment for signature is meant to be 1?
			//Pad (stream, 4);
			byte ln;
			GetValue (stream, out ln);

			val.Data = br.ReadBytes ((int)ln);
			br.ReadByte (); //null string terminator
		}

		public static void GetValue (Stream stream, out bool val)
		{
			uint intval;
			GetValue (stream, out intval);

			//TODO: confirm semantics of dbus boolean
			val = intval == 0 ? false : true;
		}

		public static void Write (Stream stream, Type type, object val)
		{
			//hacky
			if (type.IsArray) {
				Type elem_type = type.GetElementType ();
				DType elem_dtype = Signature.TypeToDType (elem_type);

				//FIXME: signature writing
				//ms.WriteByte ((byte)elem_dtype);
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
				case DType.String:
				{
					Write (stream, (string)val);
				}
				break;
				case DType.Byte:
				{
					Write (stream, (byte)val);
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
				case DType.Boolean:
				{
					Write (stream, (bool)val);
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
				break;
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

		public static void GetValue (Stream stream, Type type, out object val)
		{
			//FIXME
			//hacky

			//special case arrays for now
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
				case DType.String:
				{
					string vval;
					GetValue (stream, out vval);
					val = vval;
				}
				break;
				case DType.Byte:
				{
					byte vval;
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
				case DType.Boolean:
				{
					bool vval;
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
				Console.Error.WriteLine ("Error: Unhandled variant type: " + dtype);
				val = null;
				break;
			}
		}

		/*
		public static void GetValue (Stream stream, out HeaderField val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 8); //alignment for struct, right?
			val.Code = (FieldCode)br.ReadByte ();
			//GetValue (stream, out val.Code);
			GetValue (stream, out val.Value);
		}
		*/

		public static void GetValue (Stream stream, out byte val)
		{
			BinaryReader br = new BinaryReader (stream);

			Pad (stream, 1);
			val = br.ReadByte ();
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

		public static void GetValue (Stream stream, out ObjectPath val)
		{
			//exactly the same as string
			GetValue (stream, out val.Value);
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

		/*
		public static void GetValue (Stream stream, out HeaderField[] val)
		{
			uint ln;
			GetValue (stream, out ln);

			int endPos = (int)stream.Position + (int)ln;

			List<HeaderField> lvals = new List<HeaderField> ();

			while (stream.Position != endPos)
			{
				HeaderField sval;
				GetValue (stream, out sval);
				lvals.Add (sval);
			}

			val = lvals.ToArray ();
		}
		*/


		//this could be made generic to avoid boxing
		//restricted to primitive elements because of the DType bottleneck
		//FIXME: this has become a mess and doesn't work fully, clean it up
		/*
		public static void Write (Stream stream, Type type, Array val)
		{
			//if (type.IsArray)
			type = type.GetElementType ();

			//inefficient, but good for now
			MemoryStream ms = new MemoryStream ();

			//simulate the alignment offset of the current stream in the new one
			//int offset = PadNeeded ((int)stream.Position + 4, 8);
			//ms.Position = offset;

			//int offset = PadNeeded (Padded ((int)stream.Position, 4)+4, 16);
			//int offset = PadNeeded (Padded ((int)stream.Position, 4)+4, Padding.GetAlignment (Signature.TypeToDType (type)));
			int offset = Padded (PadNeeded ((int)stream.Position, 4)+4, Padding.GetAlignment (Signature.TypeToDType (type)));
			ms.Position = offset;
			//TODO: advance to the alignment of the element
			//Pad (stream, Padding.GetAlignment (Signature.TypeToDType (type)));

			foreach (object elem in val)
				//Write (ms, Signature.TypeToDType (type), elem);
				Write (ms, type, elem);

			//Write (stream, (uint)ms.Position);
			Write (stream, (uint)(ms.Position - offset));

			//Pad (stream, Padding.GetAlignment (Signature.TypeToDType (type)));

			//byte[] buf = ms.GetBuffer ();
			//stream.Write (buf, 0, (int)ms.Position);

			//ms.WriteTo (stream);
			{
				byte[] bytes = ms.ToArray ();
				stream.Write (bytes, offset, bytes.Length - offset);
				//stream.Write (bytes, 0, bytes.Length);
			}
		}
		*/

		//non copying version of above
		//the old code may be needed when streams are non-seekable
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
				Console.Error.WriteLine ("Warning: pos " + stream.Position + " != ep " + endPos);
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

			//FIXME: why are we not reaching the precise endPos?
			//while (stream.Position != endPos)
			while (stream.Position < endPos)
			{
				object elem;
				//GetValue (stream, Signature.TypeToDType (type), out elem);
				GetValue (stream, type, out elem);
				vals.Add (elem);
			}

			if (stream.Position != endPos)
				Console.Error.WriteLine ("Warning: pos " + stream.Position + " != ep " + endPos);

			val = vals.ToArray (type);
			//val = Array.CreateInstance (type.UnderlyingSystemType, vals.Count);
		}


		/*
		public static void GetValue (Stream stream, out string[] val)
		{
			uint ln;
			GetValue (stream, out ln);

			int endPos = (int)stream.Position + (int)ln;

			List<string> lvals = new List<string> ();

			while (stream.Position != endPos)
			{
				string sval;
				GetValue (stream, out sval);
				lvals.Add (sval);
			}

			val = lvals.ToArray ();
		}
		*/

		//struct
		//probably the wrong place for this
		//there might be more elegant solutions
		public static void GetValue (Stream stream, Type type, out ValueType val)
		{
			Pad (stream, 8); //offset for structs, right?

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

				List<object> vals = new List<object> ();
				foreach (System.Reflection.ParameterInfo parm in parms) {
					object arg;
					Message.GetValue (stream, parm.ParameterType, out arg);
					vals.Add (arg);
				}

				//object retObj = ci.Invoke (val, vals.ToArray ());
				val = (ValueType)Activator.CreateInstance (type, vals.ToArray ());
				return;
			}

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

		public ObjectPath Path = new ObjectPath ("");
		public string Interface = "";
		public string Member = "";
		public string ErrorName = "";
		public uint ReplySerial = 0;
		public string Destination = "";
		public string Sender = "";
		public Signature Signature = new Signature ("");

		//only in values for MethodCall, only out valuess for MethodReturn?
		//public DType[] Signature;

		public void ParseHeader ()
		{
			MemoryStream ms = new MemoryStream (HeaderData);

			ms.Position = 12; //ugly
			//GetValue (ms, out HeaderFields);

			Array tmpArr;
			GetValue (ms, typeof (HeaderField[]), out tmpArr);
			HeaderFields = (HeaderField[])tmpArr;

			foreach (HeaderField field in HeaderFields)
			{
				//TODO: more fields, maybe make this more efficient, less ugly

				switch (field.Code)
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
		}

		//TODO: clean this up
		public unsafe void WriteHeader (params HeaderField[] fields)
		{
			Message msg = this;

			//dbus-send --print-reply=literal --dest=org.freedesktop.DBus /org/freedesktop/DBus org.freedesktop.DBus.ListNames

			//TODO: make this a Message, cleanup
			MemoryStream ms = new MemoryStream ();
			byte[] buf = new byte[12];

			fixed (byte* pbuf = buf) {
				DHeader* hdr = (DHeader*)pbuf;
				msg.Header = hdr;
			}

			fixed (byte* pbuf = buf) {
				DHeader* hdr = (DHeader*)pbuf;
				hdr->Endianness = EndianFlag.Little;
				hdr->MessageType = MessageType.MethodCall;
				//hdr->Flags = HeaderFlag.None;
				hdr->Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
				hdr->MajorVersion = 1;
				hdr->Length = 0;
				//hdr->Serial = GenerateSerial ();
			}

			//FIXME
			//ms.Write (buf, 0, sizeof(DHeader));
			ms.Write (buf, 0, 12);
			//ms.Position = sizeof (DHeader) - sizeof (uint);
			//ms.Position = 12;

			//Message.Write (ms, fields);
			Message.Write (ms, typeof (HeaderField[]), (Array)fields);
			Message.Close (ms);

			msg.HeaderData = ms.GetBuffer ();

			//hacky header length setting
			fixed (byte* pbuf = msg.HeaderData) {
				DHeader* hdr = (DHeader*)pbuf;
				msg.Header = hdr;
			}

			//hack to provide length of header data including padding
			msg.HeaderSize = (int)ms.Position;


			//hacky body length setting
			if (msg.Body != null) {
				//TODO: should be data length, unpadded?
				//GOOD msg.Header->Length = (uint)msg.BodySize;
				msg.Header->Length = (uint)msg.Body.Position;
				//msg.Header->Length = (uint)(msg.HeaderSize + msg.BodySize);
				//Console.WriteLine ("DataSize: " + msg.BodySize);

				//pad the end of the message body
				//this could be done elsewhere
				Message.Close (msg.Body);
			}
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
				//Console.WriteLine ("got pad: " + br.ReadByte ());
				//byte b = br.ReadByte ();
				int b = stream.ReadByte ();
				if (b != 0) {
					StackTrace st = new System.Diagnostics.StackTrace (true);
					//System.Diagnostics.StackTrace ();
					Console.Error.WriteLine ();
					Console.Error.WriteLine ("Warning: Read non-zero padding byte:");
					Console.Error.WriteLine ("At pos " + i + " (offset " + stream.Position + "), pad value was " + b + ":");
					Console.Error.WriteLine (st);
					Console.Error.WriteLine ();
				}
			}
		}

		public static bool IsReading = false;
	}
}

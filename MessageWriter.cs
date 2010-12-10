// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DBus.Protocol
{
	public sealed class MessageWriter
	{
		EndianFlag endianness;
		internal MemoryStream stream;
		Connection connection;

		//a default constructor is a bad idea for now as we want to make sure the header and content-type match
		public MessageWriter () : this (Connection.NativeEndianness) {}

		public MessageWriter (Connection connection) : this(Connection.NativeEndianness)
		{
			this.connection = connection;
		}

		public MessageWriter (EndianFlag endianness)
		{
			this.endianness = endianness;
			stream = new MemoryStream ();
		}

		public Connection Connection {
			get {
				return connection;
			}
			set {
				connection = value;
			}
		}

		public byte[] ToArray ()
		{
			//TODO: mark the writer locked or something here
			return stream.ToArray ();
		}

		public void ToStream (Stream dest)
		{
			stream.WriteTo (dest);
		}

		public void CloseWrite ()
		{
			WritePad (8);
		}

		public void Write (byte val)
		{
			stream.WriteByte (val);
		}

		public void Write (bool val)
		{
			Write ((uint) (val ? 1 : 0));
		}

		// Buffer for integer marshaling
		byte[] dst = new byte[8];
		unsafe void MarshalUShort (void* dataPtr)
		{
			WritePad (2);

			if (endianness == Connection.NativeEndianness) {
				fixed (byte* p = &dst[0])
					*((ushort*)p) = *((ushort*)dataPtr);
			} else {
				byte* data = (byte*)dataPtr;
				dst[0] = data[1];
				dst[1] = data[0];
			}

			stream.Write (dst, 0, 2);
		}

		unsafe public void Write (short val)
		{
			MarshalUShort (&val);
		}

		unsafe public void Write (ushort val)
		{
			MarshalUShort (&val);
		}

		unsafe void MarshalUInt (void* dataPtr)
		{
			WritePad (4);

			if (endianness == Connection.NativeEndianness) {
				fixed (byte* p = &dst[0])
					*((uint*)p) = *((uint*)dataPtr);
			} else {
				byte* data = (byte*)dataPtr;
				dst[0] = data[3];
				dst[1] = data[2];
				dst[2] = data[1];
				dst[3] = data[0];
			}

			stream.Write (dst, 0, 4);
		}

		unsafe public void Write (int val)
		{
			MarshalUInt (&val);
		}

		unsafe public void Write (uint val)
		{
			MarshalUInt (&val);
		}

		unsafe void MarshalULong (void* dataPtr)
		{
			WritePad (8);

			if (endianness == Connection.NativeEndianness) {
				fixed (byte* p = &dst[0])
					*((ulong*)p) = *((ulong*)dataPtr);
			} else {
				byte* data = (byte*)dataPtr;
				for (int i = 0; i < 8; ++i)
					dst[i] = data[7 - i];
			}

			stream.Write (dst, 0, 8);
		}

		unsafe public void Write (long val)
		{
			MarshalULong (&val);
		}

		unsafe public void Write (ulong val)
		{
			MarshalULong (&val);
		}

#if !DISABLE_SINGLE
		unsafe public void Write (float val)
		{
			MarshalUInt (&val);
		}
#endif

		unsafe public void Write (double val)
		{
			MarshalULong (&val);
		}

		public void Write (string val)
		{
			byte[] utf8_data = Encoding.UTF8.GetBytes (val);
			Write ((uint)utf8_data.Length);
			stream.Write (utf8_data, 0, utf8_data.Length);
			WriteNull ();
		}

		public void Write (ObjectPath val)
		{
			Write (val.Value);
		}

		public void Write (Signature val)
		{
			byte[] ascii_data = val.GetBuffer ();

			if (ascii_data.Length > ProtocolInformations.MaxSignatureLength)
				throw new Exception ("Signature length " + ascii_data.Length + " exceeds maximum allowed " + ProtocolInformations.MaxSignatureLength + " bytes");

			Write ((byte)ascii_data.Length);
			stream.Write (ascii_data, 0, ascii_data.Length);
			WriteNull ();
		}

		[Obsolete]
		public void WriteComplex (object val, Type type)
		{
			if (type == typeof (void))
				return;

			if (type.IsArray) {
				MethodInfo miDict = typeof (MessageWriter).GetMethod ("WriteArray");
				MethodInfo mi = miDict.MakeGenericMethod (type.GetElementType ());
				mi.Invoke (this, new object[] {val});
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();
				MethodInfo miDict = typeof (MessageWriter).GetMethod ("WriteFromDict");
				MethodInfo mi = miDict.MakeGenericMethod (genArgs);
				mi.Invoke (this, new object[] {val});
			} else if (Mapper.IsPublic (type)) {
				WriteObject (type, val);
			} else if (!type.IsPrimitive && !type.IsEnum) {
				WriteValueType (val, type);
				/*
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (Nullable<>)) {
				//is it possible to support nullable types?
				Type[] genArgs = type.GetGenericArguments ();
				WriteVariant (genArgs[0], val);
				*/
			} else {
				throw new Exception ("Can't write");
			}
		}

		[Obsolete]
		public void Write (Type type, object val)
		{
			if (type == typeof (void))
				return;

			if (type.IsArray) {
				MethodInfo miDict = typeof (MessageWriter).GetMethod ("WriteArray");
				MethodInfo mi = miDict.MakeGenericMethod (type.GetElementType ());
				mi.Invoke (this, new object[] {val});
			} else if (type == typeof (ObjectPath)) {
				Write ((ObjectPath)val);
			} else if (type == typeof (Signature)) {
				Write ((Signature)val);
			} else if (type == typeof (object)) {
				Write (val);
			} else if (type == typeof (string)) {
				Write ((string)val);
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();
				MethodInfo miDict = typeof (MessageWriter).GetMethod ("WriteFromDict");
				MethodInfo mi = miDict.MakeGenericMethod (genArgs);
				mi.Invoke (this, new object[] {val});
			} else if (Mapper.IsPublic (type)) {
				WriteObject (type, val);
			} else if (!type.IsPrimitive && !type.IsEnum) {
				WriteValueType (val, type);
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
#if !DISABLE_SINGLE
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

		public void WriteObject (Type type, object val)
		{
			ObjectPath path;

			BusObject bobj = val as BusObject;

			if (bobj == null && val is MarshalByRefObject) {
				bobj = ((MarshalByRefObject)val).GetLifetimeService () as BusObject;
			}

			if (bobj == null)
				throw new Exception ("No object reference to write");

			path = bobj.Path;

			Write (path);
		}

		//variant
		public void Write (object val)
		{
			if (val == null)
				throw new NotSupportedException ("Cannot send null variant");

			if (val is DValue) {
				DValue dv = (DValue)val;

				if (dv.endianness != endianness)
					throw new NotImplementedException ("Writing opposite endian DValues not yet implemented.");

				Write (dv.signature);
				WritePad (dv.signature.Alignment);
				stream.Write (dv.data, 0, dv.data.Length);
				return;
			}

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
		public unsafe void WriteArray<T> (T[] val)
		{
			Type elemType = typeof (T);

			if (elemType == typeof (byte)) {
				if (val.Length > ProtocolInformations.MaxArrayLength)
					ThrowArrayLengthException ((uint)val.Length);

				Write ((uint)val.Length);
				stream.Write ((byte[])(object)val, 0, val.Length);
				return;
			}

			if (elemType.IsEnum)
				elemType = Enum.GetUnderlyingType (elemType);

			Signature sigElem = Signature.GetSig (elemType);
			int fixedSize = 0;
			// TODO: similar to MessageReader's ReadArray, make the fast path available to both endianess
			if (endianness == Connection.NativeEndianness && elemType.IsValueType && !sigElem.IsStruct && elemType != typeof(bool) && sigElem.GetFixedSize (ref fixedSize)) {
				int byteLength = fixedSize * val.Length;
				if (byteLength > ProtocolInformations.MaxArrayLength)
					ThrowArrayLengthException ((uint)byteLength);

				Write ((uint)byteLength);
				WritePad (sigElem.Alignment);

				GCHandle valHandle = GCHandle.Alloc (val, GCHandleType.Pinned);
				IntPtr p = valHandle.AddrOfPinnedObject ();
				byte[] data = new byte[byteLength];
				Marshal.Copy (p, data, 0, byteLength);
				valHandle.Free ();

				return;
			}

			long origPos = stream.Position;
			Write ((uint)0);

			//advance to the alignment of the element
			WritePad (sigElem.Alignment);

			long startPos = stream.Position;

			TypeWriter<T> tWriter = TypeImplementer.GetTypeWriter<T> ();

			foreach (T elem in val)
				tWriter (this, elem);

			long endPos = stream.Position;
			uint ln = (uint)(endPos - startPos);
			stream.Position = origPos;

			if (ln > ProtocolInformations.MaxArrayLength)
				ThrowArrayLengthException (ln);

			Write (ln);
			stream.Position = endPos;
		}

		static void ThrowArrayLengthException (uint ln)
		{
			throw new Exception ("Array length " + ln.ToString () + " exceeds maximum allowed " + ProtocolInformations.MaxArrayLength + " bytes");
		}

		public void WriteValueType (object val, Type type)
		{
			MethodInfo mi = TypeImplementer.GetWriteMethod (type);
			mi.Invoke (null, new object[] {this, val});
		}

		public void WriteStructure<T> (T value)
		{
			TypeWriter<T> tWriter = TypeImplementer.GetTypeWriter<T> ();
			tWriter (this, value);
		}

		public void WriteFromDict<TKey,TValue> (IDictionary<TKey,TValue> val)
		{
			long origPos = stream.Position;
			Write ((uint)0);

			WritePad (8);

			long startPos = stream.Position;

			TypeWriter<TKey> keyWriter = TypeImplementer.GetTypeWriter<TKey> ();
			TypeWriter<TValue> valueWriter = TypeImplementer.GetTypeWriter<TValue> ();

			foreach (KeyValuePair<TKey,TValue> entry in val)
			{
				WritePad (8);
				keyWriter (this, entry.Key);
				valueWriter (this, entry.Value);
			}

			long endPos = stream.Position;
			uint ln = (uint)(endPos - startPos);
			stream.Position = origPos;

			if (ln > ProtocolInformations.MaxArrayLength)
				throw new Exception ("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformations.MaxArrayLength + " bytes");

			Write (ln);
			stream.Position = endPos;
		}

		/*
		public void Write (IDictionary<FieldCode, object> val)
		{
			WriteHeaderDict(val);
		}

		public void Write (Dictionary<FieldCode, object> val)
		{
			WriteHeaderDict (val);
		}
		*/

		/*
		public void Write (Dictionary<byte, object> val)
		{
			long origPos = stream.Position;
			Write ((uint)0);

			WritePad (8);

			long startPos = stream.Position;

			foreach (KeyValuePair<byte, object> entry in val) {
				WritePad (8);
				Write (entry.Key);
				Write (entry.Value);
			}

			long endPos = stream.Position;
			uint ln = (uint)(endPos - startPos);
			stream.Position = origPos;

			if (ln > Protocol.MaxArrayLength)
				throw new Exception ("Dict length " + ln + " exceeds maximum allowed " + Protocol.MaxArrayLength + " bytes");

			Write (ln);
			stream.Position = endPos;
		}
		*/

		internal void WriteHeaderFields (Dictionary<byte, object> val)
		{
			long origPos = stream.Position;
			Write ((uint)0);

			WritePad (8);

			long startPos = stream.Position;

			foreach (KeyValuePair<byte, object> entry in val) {
				WritePad (8);
				Write (entry.Key);
				switch ((FieldCode)entry.Key) {
					case FieldCode.Destination:
					case FieldCode.ErrorName:
					case FieldCode.Interface:
					case FieldCode.Member:
					case FieldCode.Sender:
						Write (Signature.StringSig);
						Write ((string)entry.Value);
						break;
					case FieldCode.Path:
						Write (Signature.ObjectPathSig);
						Write ((ObjectPath)entry.Value);
						break;
					case FieldCode.ReplySerial:
						Write (Signature.UInt32Sig);
						Write ((uint)entry.Value);
						break;
					default:
						Write (entry.Value);
						break;
				}
			}

			long endPos = stream.Position;
			uint ln = (uint)(endPos - startPos);
			stream.Position = origPos;

			if (ln > ProtocolInformations.MaxArrayLength)
				throw new Exception ("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformations.MaxArrayLength + " bytes");

			Write (ln);
			stream.Position = endPos;
		}

		public void WriteNull ()
		{
			stream.WriteByte (0);
		}

		// Source buffer for zero-padding
		static readonly byte[] nullBytes = new byte[8];
		public void WritePad (int alignment)
		{
			int needed = ProtocolInformations.PadNeeded ((int)stream.Position, alignment);
			stream.Write (nullBytes, 0, needed);
		}
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections;

namespace DBus.Protocol
{
	public class MessageReader
	{
		public class PaddingException : Exception
		{
			int position;
			byte element;

			internal PaddingException (int position, byte element)
				: base ("Read non-zero byte at position " + position + " while expecting padding. Value given: " + element)
			{
				this.position = position;
				this.element = element;
			}

			public int Position {
				get {
					return position;
				}
			}

			public byte Byte {
				get {
					return element;
				}
			}
		}

		readonly EndianFlag endianness;
		readonly byte[] data;
		readonly Message message;

		int pos = 0;
		static Dictionary<Type, bool> isPrimitiveStruct = new Dictionary<Type, bool> ();
		static readonly Encoding stringEncoding = Encoding.UTF8;
		Dictionary<Type, Func<object>> readValueCache = new Dictionary<Type, Func<object>> ();

		public MessageReader (EndianFlag endianness, byte[] data)
		{
			if (data == null)
				data = new byte[0];

			this.endianness = endianness;
			this.data = data;
		}

		public MessageReader (Message message) : this (message.Header.Endianness, message.Body)
		{
			if (message == null)
				throw new ArgumentNullException ("message");

			this.message = message;
		}

		public bool DataAvailable {
			get {
				return pos < data.Length;
			}
		}

		public object ReadValue (Type type)
		{
			if (type == typeof (void))
				return null;

			Func<object> fastAccess;
			if (readValueCache.TryGetValue (type, out fastAccess))
				return fastAccess ();

			if (type.IsArray) {
				readValueCache[type] = () => ReadArray (type.GetElementType ());
				return ReadArray (type.GetElementType ());
			} else if (type == typeof (ObjectPath)) {
				readValueCache[type] = () => ReadObjectPath ();
				return ReadObjectPath ();
			} else if (type == typeof (Signature)) {
				readValueCache[type] = () => ReadSignature ();
				return ReadSignature ();
			} else if (type == typeof (object)) {
				readValueCache[type] = () => ReadVariant ();
				return ReadVariant ();
			} else if (type == typeof (string)) {
				readValueCache[type] = () => ReadString ();
				return ReadString ();
			} else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (Dictionary<,>)) {
				Type[] genArgs = type.GetGenericArguments ();
				readValueCache[type] = () => ReadDictionary (genArgs[0], genArgs[1]);
				return ReadDictionary (genArgs[0], genArgs[1]);
			} else if (Mapper.IsPublic (type)) {
				readValueCache[type] = () => GetObject (type);
				return GetObject (type);
			} else if (!type.IsPrimitive && !type.IsEnum) {
				readValueCache[type] = () => ReadStruct (type);
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

		public object PeekValue (DType sig)
		{
			int savedPos = pos;
			object result = ReadValue (sig);
			pos = savedPos;

			return result;
		}

		public void Seek (int stride)
		{
			var check = pos + stride;
			if (check < 0 || check > data.Length)
				throw new ArgumentOutOfRangeException ("stride");
			pos = check;
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

			string val = stringEncoding.GetString (data, pos, (int)ln);
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

			if (ln > ProtocolInformation.MaxSignatureLength)
				throw new Exception ("Signature length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxSignatureLength + " bytes");

			byte[] sigData = new byte[ln];
			Array.Copy (data, pos, sigData, 0, (int)ln);
			pos += (int)ln;
			ReadNull ();

			return Signature.Take (sigData);
		}

		public object ReadVariant ()
		{
			var sig = ReadSignature ();
			if (!sig.IsSingleCompleteType)
				throw new InvalidOperationException (string.Format ("ReadVariant need a single complete type signature, {0} was given", sig.ToString ()));
			return ReadValue (sig);
		}

		// Used primarily for reading variant values
		public object ReadValue (Signature sig)
		{
			if (!sig.IsSingleCompleteType)
				throw new ArgumentException (string.Format ("ReadVariant need a single complete type signature, {0} was given", sig.ToString ()));

			var val = ReadValue (sig.ToType ());
			return val;
		}

		public IDictionary ReadDictionary (Type keyType, Type valType)
		{
			MethodInfo mi = this.GetType ().GetMethod ("ReadDictionary", Type.EmptyTypes).MakeGenericMethod (new [] { keyType, valType });
			return (IDictionary)mi.Invoke (this, null);
		}

		public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue> ()
		{
			uint ln = ReadUInt32 ();

			if (ln > ProtocolInformation.MaxArrayLength)
				throw new Exception ("Dict length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

			var val = new Dictionary<TKey, TValue> ((int)(ln / 8));
			ReadPad (8);

			int endPos = pos + (int)ln;

			while (pos < endPos) {
				ReadPad (8);
				TKey k = (TKey)ReadValue (typeof (TKey));
				TValue v = (TValue)ReadValue (typeof (TValue));
				val.Add (k, v);
			}

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);

			return val;
		}

		public Array ReadArray (Type elemType)
		{
			MethodInfo mi = this.GetType ().GetMethod ("ReadArray", Type.EmptyTypes).MakeGenericMethod (new [] { elemType });
			return (Array)mi.Invoke (this, null);
		}

		public TArray[] ReadArray<TArray> ()
		{
			uint ln = ReadUInt32 ();
			Type elemType = typeof (TArray);

			if (ln > ProtocolInformation.MaxArrayLength)
				throw new Exception ("Array length " + ln + " exceeds maximum allowed " + ProtocolInformation.MaxArrayLength + " bytes");

			//advance to the alignment of the element
			ReadPad (ProtocolInformation.GetAlignment (Signature.TypeToDType (elemType)));

			if (elemType.IsPrimitive) {
				// Fast path for primitive types (except bool which isn't blittable and take another path)
				if (elemType != typeof (bool))
					return MarshalArray<TArray> (ln);
				else
					return (TArray[])(Array)MarshalBoolArray (ln);
		    }

			var list = new List<TArray> ();
			int endPos = pos + (int)ln;

			while (pos < endPos)
				list.Add ((TArray)ReadValue (elemType));

			if (pos != endPos)
				throw new Exception ("Read pos " + pos + " != ep " + endPos);

			return list.ToArray ();
		}

		TArray[] MarshalArray<TArray> (uint length)
		{
			int sof = Marshal.SizeOf (typeof (TArray));
			TArray[] array = new TArray[(int)(length / sof)];

			if (endianness == Connection.NativeEndianness) {
				Buffer.BlockCopy (data, pos, array, 0, (int)length);
				pos += (int)length;
			} else {
				GCHandle handle = GCHandle.Alloc (array, GCHandleType.Pinned);
				DirectCopy (sof, length, handle);
				handle.Free ();
			}

			return array;
		}

		void DirectCopy (int sof, uint length, GCHandle handle)
		{
			DirectCopy (sof, length, handle.AddrOfPinnedObject ());
		}

		unsafe void DirectCopy (int sof, uint length, IntPtr handle)
		{
			if (endianness == Connection.NativeEndianness) {
				Marshal.Copy (data, pos, handle, (int)length);
			} else {
				byte* ptr = (byte*)(void*)handle;
				for (int i = pos; i < pos + length; i += sof)
					for (int j = i; j < i + sof; j++)
						ptr[2 * i - pos + (sof - 1) - j] = data[j];
			}

			pos += (int)length * sof;
		}

		bool[] MarshalBoolArray (uint length)
		{
			bool[] array = new bool [length];
			for (int i = 0; i < length; i++)
				array[i] = ReadBoolean ();

			return array;
		}

		public object ReadStruct (Type type)
		{
			ReadPad (8);

			FieldInfo[] fis = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			// Empty struct? No need for processing
			if (fis.Length == 0)
				return Activator.CreateInstance (type);

			if (IsEligibleStruct (type, fis))
				return MarshalStruct (type, fis);

			object val = Activator.CreateInstance (type);

			foreach (System.Reflection.FieldInfo fi in fis)
				fi.SetValue (val, ReadValue (fi.FieldType));

			return val;
		}

		public T ReadStruct<T> () where T : struct
		{
			ReadPad (8);

			FieldInfo[] fis = typeof (T).GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			// Empty struct? No need for processing
			if (fis.Length == 0)
				return default (T);

			if (IsEligibleStruct (typeof(T), fis))
				return (T)MarshalStruct (typeof(T), fis);

			object val = Activator.CreateInstance<T> ();

			foreach (System.Reflection.FieldInfo fi in fis)
				fi.SetValue (val, ReadValue (fi.FieldType));

			return (T)val;
		}

		object MarshalStruct (Type structType, FieldInfo[] fis)
		{
			object strct = Activator.CreateInstance (structType);
			int sof = Marshal.SizeOf (fis[0].FieldType);
			GCHandle handle = GCHandle.Alloc (strct, GCHandleType.Pinned);
			DirectCopy (sof, (uint)(fis.Length * sof), handle);
			handle.Free ();

			return strct;
		}

		// Unfortunately in newer Mono, the compiler is not able to infer
		// that T is a structure for the purpose of acquiring a pointer
		// causing a compilation error
		/*T NewMarshalStruct<T> (FieldInfo[] fis) where T : struct
		{
			T val = default (T);
			int sof = Marshal.SizeOf (fis[0].FieldType);

			unsafe {
				byte* pVal = (byte*)&val;
				DirectCopy (sof, (uint)(fis.Length * sof), (IntPtr)pVal);
			}

			return val;
		}*/

		public void ReadNull ()
		{
			if (data[pos] != 0)
				throw new Exception ("Read non-zero byte at position " + pos + " while expecting null terminator");
			pos++;
		}

		public void ReadPad (int alignment)
		{
			for (int endPos = ProtocolInformation.Padded (pos, alignment) ; pos != endPos ; pos++)
				if (data[pos] != 0)
					throw new PaddingException (pos, data[pos]);
		}

		// Note: This method doesn't support aggregate signatures
		public bool StepOver (Signature sig)
		{
			if (sig == Signature.VariantSig) {
				Signature valueSig = ReadSignature ();
				return StepOver (valueSig);
			}

			if (sig == Signature.StringSig) {
				uint valueLength = ReadUInt32 ();
				pos += (int)valueLength;
				pos++;
				return true;
			}

			if (sig == Signature.ObjectPathSig) {
				uint valueLength = ReadUInt32 ();
				pos += (int)valueLength;
				pos++;
				return true;
			}

			if (sig == Signature.SignatureSig) {
				byte valueLength = ReadByte ();
				pos += valueLength;
				pos++;
				return true;
			}

			// No need to handle dicts specially. IsArray does the job
			if (sig.IsArray) {
				Signature elemSig = sig.GetElementSignature ();
				uint ln = ReadUInt32 ();
				pos = ProtocolInformation.Padded (pos, elemSig.Alignment);
				pos += (int)ln;
				return true;
			}

			int endPos = pos;
			if (sig.GetFixedSize (ref endPos)) {
				pos = endPos;
				return true;
			}

			if (sig.IsDictEntry) {
				pos = ProtocolInformation.Padded (pos, sig.Alignment);
				Signature sigKey, sigValue;
				sig.GetDictEntrySignatures (out sigKey, out sigValue);
				if (!StepOver (sigKey))
					return false;
				if (!StepOver (sigValue))
					return false;
				return true;
			}

			if (sig.IsStruct) {
				pos = ProtocolInformation.Padded (pos, sig.Alignment);
				foreach (Signature fieldSig in sig.GetFieldSignatures ())
					if (!StepOver (fieldSig))
						return false;
				return true;
			}

			throw new Exception ("Can't step over '" + sig + "'");
		}

		public IEnumerable<Signature> StepInto (Signature sig)
		{
			if (sig == Signature.VariantSig) {
				Signature valueSig = ReadSignature ();
				yield return valueSig;
				yield break;
			}

			// No need to handle dicts specially. IsArray does the job
			if (sig.IsArray) {
				Signature elemSig = sig.GetElementSignature ();
				uint ln = ReadUInt32 ();
				ReadPad (elemSig.Alignment);
				int endPos = pos + (int)ln;
				while (pos < endPos)
					yield return elemSig;
				yield break;
			}

			if (sig.IsDictEntry) {
				pos = ProtocolInformation.Padded (pos, sig.Alignment);
				Signature sigKey, sigValue;
				sig.GetDictEntrySignatures (out sigKey, out sigValue);
				yield return sigKey;
				yield return sigValue;
				yield break;
			}

			if (sig.IsStruct) {
				pos = ProtocolInformation.Padded (pos, sig.Alignment);
				foreach (Signature fieldSig in sig.GetFieldSignatures ())
					yield return fieldSig;
				yield break;
			}

			throw new Exception ("Can't step into '" + sig + "'");
		}

		// If a struct is only composed of primitive type fields (i.e. blittable types)
		// then this method return true. Result is cached in isPrimitiveStruct dictionary.
		internal static bool IsEligibleStruct (Type structType, FieldInfo[] fields)
		{
			bool result;
			if (isPrimitiveStruct.TryGetValue (structType, out result))
				return result;

			if (!(isPrimitiveStruct[structType] = fields.All ((f) => f.FieldType.IsPrimitive && f.FieldType != typeof (bool))))
				return false;

			int alignement = GetAlignmentFromPrimitiveType (fields[0].FieldType);

			return isPrimitiveStruct[structType] = !fields.Any ((f) => GetAlignmentFromPrimitiveType (f.FieldType) != alignement);
		}

		static int GetAlignmentFromPrimitiveType (Type type)
		{
			return ProtocolInformation.GetAlignment (Signature.TypeCodeToDType (Type.GetTypeCode (type)));
		}
	}
}

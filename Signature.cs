// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;

using System.Collections.Generic;
//TODO: Reflection should be done at a higher level than this class
using System.Reflection;

namespace DBus
{
	//maybe this should be nullable?
	struct Signature
	{
		//TODO: this class needs some work
		//Data should probably include the null terminator

		public static readonly Signature Empty = new Signature (String.Empty);
		public static readonly Signature ByteSig = Allocate (DType.Byte);
		public static readonly Signature UInt16Sig = Allocate (DType.UInt16);
		public static readonly Signature UInt32Sig = Allocate (DType.UInt32);
		public static readonly Signature StringSig = Allocate (DType.String);
		public static readonly Signature ObjectPathSig = Allocate (DType.ObjectPath);
		public static readonly Signature SignatureSig = Allocate (DType.Signature);
		public static readonly Signature VariantSig = Allocate (DType.Variant);

		public static bool operator == (Signature a, Signature b)
		{
			if (a.data == b.data)
				return true;

			if (a.data == null)
				return false;

			if (b.data == null)
				return false;

			if (a.data.Length != b.data.Length)
				return false;

			for (int i = 0 ; i != a.data.Length ; i++)
				if (a.data[i] != b.data[i])
					return false;

			return true;
		}

		public static bool operator != (Signature a, Signature b)
		{
			return !(a == b);
		}

		public override bool Equals (object o)
		{
			if (o == null)
				return false;

			if (!(o is Signature))
				return false;

			return this == (Signature)o;
		}

		public override int GetHashCode ()
		{
			// TODO: Avoid string conversion
			return Value.GetHashCode ();
		}

		public static Signature operator + (Signature s1, Signature s2)
		{
			return Concat (s1, s2);
		}

		public static Signature Concat (Signature s1, Signature s2)
		{
			if (s1.data == null && s2.data == null)
				return Signature.Empty;

			if (s1.data == null)
				return s2;

			if (s2.data == null)
				return s1;

			if (s1.Length + s2.Length == 0)
				return Signature.Empty;

			byte[] data = new byte[s1.data.Length + s2.data.Length];
			s1.data.CopyTo (data, 0);
			s2.data.CopyTo (data, s1.data.Length);
			return Signature.Take (data);
		}

		public Signature (string value)
		{
			if (value.Length == 0) {
				this.data = Empty.data;
				return;
			}

			if (value.Length == 1) {
				this.data = DataForDType ((DType)value[0]);
				return;
			}

			this.data = Encoding.ASCII.GetBytes (value);
		}

		internal static Signature Take (byte[] value)
		{
			Signature sig;

			if (value.Length == 0) {
				sig.data = Empty.data;
				return sig;
			}

			if (value.Length == 1) {
				sig.data = DataForDType ((DType)value[0]);
				return sig;
			}

			sig.data = value;
			return sig;
		}

		static byte[] DataForDType (DType value)
		{
			// Reduce heap allocations.
			// For now, we only cache the most common protocol signatures.
			switch (value) {
				case DType.Byte:
					return ByteSig.data;
				case DType.UInt16:
					return UInt16Sig.data;
				case DType.UInt32:
					return UInt32Sig.data;
				case DType.String:
					return StringSig.data;
				case DType.ObjectPath:
					return ObjectPathSig.data;
				case DType.Signature:
					return SignatureSig.data;
				case DType.Variant:
					return VariantSig.data;
				default:
					return new byte[] {(byte)value};
			}
		}

		private static Signature Allocate (DType value)
		{
			Signature sig;
			sig.data = new byte[] {(byte)value};
			return sig;
		}

		internal Signature (DType value)
		{
			this.data = DataForDType (value);
		}

		internal Signature (DType[] value)
		{
			if (value.Length == 0) {
				this.data = Empty.data;
				return;
			}

			if (value.Length == 1) {
				this.data = DataForDType (value[0]);
				return;
			}

			this.data = new byte[value.Length];

			for (int i = 0 ; i != value.Length ; i++)
				this.data[i] = (byte)value[i];
		}

		byte[] data;

		//TODO: this should be private, but MessageWriter and Monitor still use it
		//[Obsolete]
		public byte[] GetBuffer ()
		{
			return data;
		}

		internal DType this[int index]
		{
			get {
				return (DType)data[index];
			}
		}

		public int Length
		{
			get {
				return data.Length;
			}
		}

		//[Obsolete]
		public string Value
		{
			get {
				//FIXME: hack to handle bad case when Data is null
				if (data == null)
					return String.Empty;

				return Encoding.ASCII.GetString (data);
			}
		}

		public override string ToString ()
		{
			return Value;

			/*
			StringBuilder sb = new StringBuilder ();

			foreach (DType t in data) {
				//we shouldn't rely on object mapping here, but it's an easy way to get string representations for now
				Type type = DTypeToType (t);
				if (type != null) {
					sb.Append (type.Name);
				} else {
					char c = (char)t;
					if (!Char.IsControl (c))
						sb.Append (c);
					else
						sb.Append (@"\" + (int)c);
				}
				sb.Append (" ");
			}

			return sb.ToString ();
			*/
		}

		public Signature MakeArraySignature ()
		{
			return new Signature (DType.Array) + this;
		}

		public static Signature MakeStruct (params Signature[] elems)
		{
			Signature sig = Signature.Empty;

			sig += new Signature (DType.StructBegin);

			foreach (Signature elem in elems)
				sig += elem;

			sig += new Signature (DType.StructEnd);

			return sig;
		}

		public static Signature MakeDictEntry (Signature keyType, Signature valueType)
		{
			Signature sig = Signature.Empty;

			sig += new Signature (DType.DictEntryBegin);

			sig += keyType;
			sig += valueType;

			sig += new Signature (DType.DictEntryEnd);

			return sig;
		}

		public static Signature MakeDict (Signature keyType, Signature valueType)
		{
			return MakeDictEntry (keyType, valueType).MakeArraySignature ();
		}

		public int Alignment
		{
			get {
				if (data.Length == 0)
					return 0;

				return Protocol.GetAlignment (this[0]);
			}
		}

		static int GetSize (DType dtype)
		{
			switch (dtype) {
				case DType.Byte:
					return 1;
				case DType.Boolean:
					return 4;
				case DType.Int16:
				case DType.UInt16:
					return 2;
				case DType.Int32:
				case DType.UInt32:
					return 4;
				case DType.Int64:
				case DType.UInt64:
					return 8;
#if !DISABLE_SINGLE
				case DType.Single: //Not yet supported!
					return 4;
#endif
				case DType.Double:
					return 8;
				case DType.String:
				case DType.ObjectPath:
				case DType.Signature:
				case DType.Array:
				case DType.StructBegin:
				case DType.Variant:
				case DType.DictEntryBegin:
					return -1;
				case DType.Invalid:
				default:
					throw new Exception ("Cannot determine size of " + dtype);
			}
		}

		public bool GetFixedSize (ref int size)
		{
			if (size < 0)
				return false;

			if (data.Length == 0)
				return true;

			// Sensible?
			size = Protocol.Padded (size, Alignment);

			if (data.Length == 1) {
				int valueSize = GetSize (this[0]);

				if (valueSize == -1)
					return false;

				size += valueSize;
				return true;
			}

			if (IsStructlike) {
				foreach (Signature sig in GetParts ())
						if (!sig.GetFixedSize (ref size))
							return false;
				return true;
			}

			if (IsArray || IsDict)
				return false;

			if (IsStruct) {
				foreach (Signature sig in GetFieldSignatures ())
						if (!sig.GetFixedSize (ref size))
							return false;
				return true;
			}

			// Any other cases?
			throw new Exception ();
		}

		public bool IsFixedSize
		{
			get {
				if (data.Length == 0)
					return true;

				if (data.Length == 1) {
					int size = GetSize (this[0]);
					return size != -1;
				}

				if (IsStructlike) {
					foreach (Signature sig in GetParts ())
						if (!sig.IsFixedSize)
							return false;
					return true;
				}

				if (IsArray || IsDict)
					return false;

				if (IsStruct) {
					foreach (Signature sig in GetFieldSignatures ())
						if (!sig.IsFixedSize)
							return false;
					return true;
				}

				// Any other cases?
				throw new Exception ();
			}
		}

		//TODO: complete this
		public bool IsPrimitive
		{
			get {
				if (data.Length != 1)
					return false;

				if (this[0] == DType.Variant)
					return false;

				if (this[0] == DType.Invalid)
					return false;

				return true;
			}
		}

		public bool IsStruct
		{
			get {
				if (Length < 2)
					return false;

				if (this[0] != DType.StructBegin)
					return false;

				// FIXME: Incorrect! What if this is in fact a Structlike starting and finishing with structs?
				if (this[Length - 1] != DType.StructEnd)
					return false;

				return true;
			}
		}

		public bool IsDictEntry
		{
			get {
				if (Length < 2)
					return false;

				if (this[0] != DType.DictEntryBegin)
					return false;

				// FIXME: Incorrect! What if this is in fact a Structlike starting and finishing with structs?
				if (this[Length - 1] != DType.DictEntryEnd)
					return false;

				return true;
			}
		}

		public bool IsStructlike
		{
			get {
				if (Length < 2)
					return false;

				if (IsArray)
					return false;

				if (IsDict)
					return false;

				if (IsStruct)
					return false;

				return true;
			}
		}

		public bool IsDict
		{
			get {
				if (Length < 3)
					return false;

				if (!IsArray)
					return false;

				// 0 is 'a'
				if (this[1] != DType.DictEntryBegin)
					return false;

				return true;
			}
		}

		public bool IsArray
		{
			get {
				if (Length < 2)
					return false;

				if (this[0] != DType.Array)
					return false;

				return true;
			}
		}

		public Signature GetElementSignature ()
		{
			if (!IsArray)
				throw new Exception ("Cannot get the element signature of a non-array (signature was '" + this + "')");

			//TODO: improve this
			//if (IsDict)
			//	throw new NotSupportedException ("Parsing dict signature is not supported (signature was '" + this + "')");

			// Skip over 'a'
			int pos = 1;
			return GetNextSignature (ref pos);
		}

		public Type[] ToTypes ()
		{
			// TODO: Find a way to avoid these null checks everywhere.
			if (data == null)
				return Type.EmptyTypes;

			List<Type> types = new List<Type> ();
			for (int i = 0 ; i != data.Length ; types.Add (ToType (ref i)));
			return types.ToArray ();
		}

		public Type ToType ()
		{
			int pos = 0;
			Type ret = ToType (ref pos);
			if (pos != data.Length)
				throw new Exception ("Signature '" + Value + "' is not a single complete type");
			return ret;
		}

		internal static DType TypeCodeToDType (TypeCode typeCode)
		{
			switch (typeCode)
			{
				case TypeCode.Empty:
					return DType.Invalid;
				case TypeCode.Object:
					return DType.Invalid;
				case TypeCode.DBNull:
					return DType.Invalid;
				case TypeCode.Boolean:
					return DType.Boolean;
				case TypeCode.Char:
					return DType.UInt16;
				case TypeCode.SByte:
					return DType.Byte;
				case TypeCode.Byte:
					return DType.Byte;
				case TypeCode.Int16:
					return DType.Int16;
				case TypeCode.UInt16:
					return DType.UInt16;
				case TypeCode.Int32:
					return DType.Int32;
				case TypeCode.UInt32:
					return DType.UInt32;
				case TypeCode.Int64:
					return DType.Int64;
				case TypeCode.UInt64:
					return DType.UInt64;
				case TypeCode.Single:
					return DType.Single;
				case TypeCode.Double:
					return DType.Double;
				case TypeCode.Decimal:
					return DType.Invalid;
				case TypeCode.DateTime:
					return DType.Invalid;
				case TypeCode.String:
					return DType.String;
				default:
					return DType.Invalid;
			}
		}

		//FIXME: this method is bad, get rid of it
		internal static DType TypeToDType (Type type)
		{
			if (type == typeof (void))
				return DType.Invalid;

			if (type == typeof (string))
				return DType.String;

			if (type == typeof (ObjectPath))
				return DType.ObjectPath;

			if (type == typeof (Signature))
				return DType.Signature;

			if (type == typeof (object))
				return DType.Variant;

			if (type.IsPrimitive)
				return TypeCodeToDType (Type.GetTypeCode (type));

			if (type.IsEnum)
				return TypeToDType (Enum.GetUnderlyingType (type));

			//needs work
			if (type.IsArray)
				return DType.Array;

			//if (type.UnderlyingSystemType != null)
			//	return TypeToDType (type.UnderlyingSystemType);
			if (Mapper.IsPublic (type))
				return DType.ObjectPath;

			if (!type.IsPrimitive && !type.IsEnum)
				return DType.Struct;

			//TODO: maybe throw an exception here
			return DType.Invalid;
		}

		/*
		public static DType TypeToDType (Type type)
		{
			if (type == null)
				return DType.Invalid;
			else if (type == typeof (byte))
				return DType.Byte;
			else if (type == typeof (bool))
				return DType.Boolean;
			else if (type == typeof (short))
				return DType.Int16;
			else if (type == typeof (ushort))
				return DType.UInt16;
			else if (type == typeof (int))
				return DType.Int32;
			else if (type == typeof (uint))
				return DType.UInt32;
			else if (type == typeof (long))
				return DType.Int64;
			else if (type == typeof (ulong))
				return DType.UInt64;
			else if (type == typeof (float)) //not supported by libdbus at time of writing
				return DType.Single;
			else if (type == typeof (double))
				return DType.Double;
			else if (type == typeof (string))
				return DType.String;
			else if (type == typeof (ObjectPath))
				return DType.ObjectPath;
			else if (type == typeof (Signature))
				return DType.Signature;
			else
				return DType.Invalid;
		}
		*/

		public IEnumerable<Signature> GetFieldSignatures ()
		{
			if (this == Signature.Empty || this[0] != DType.StructBegin)
				throw new Exception ("Not a struct");

			for (int pos = 1 ; pos < data.Length - 1 ;)
				yield return GetNextSignature (ref pos);
		}

		public void GetDictEntrySignatures (out Signature sigKey, out Signature sigValue)
		{
			if (this == Signature.Empty || this[0] != DType.DictEntryBegin)
				throw new Exception ("Not a DictEntry");

			int pos = 1;
			sigKey = GetNextSignature (ref pos);
			sigValue = GetNextSignature (ref pos);
		}

		public IEnumerable<Signature> GetParts ()
		{
			if (data == null)
				yield break;
			for (int pos = 0 ; pos < data.Length ;) {
				yield return GetNextSignature (ref pos);
			}
		}

		public Signature GetNextSignature (ref int pos)
		{
			if (data == null)
				return Signature.Empty;

			DType dtype = (DType)data[pos++];

			switch (dtype) {
				//case DType.Invalid:
				//	return typeof (void);
				case DType.Array:
					//peek to see if this is in fact a dictionary
					if ((DType)data[pos] == DType.DictEntryBegin) {
						//skip over the {
						pos++;
						Signature keyType = GetNextSignature (ref pos);
						Signature valueType = GetNextSignature (ref pos);
						//skip over the }
						pos++;
						return Signature.MakeDict (keyType, valueType);
					} else {
						Signature elementType = GetNextSignature (ref pos);
						return elementType.MakeArraySignature ();
					}
				//case DType.DictEntryBegin: // FIXME: DictEntries should be handled separately.
				case DType.StructBegin:
					//List<Signature> fieldTypes = new List<Signature> ();
					Signature fieldsSig = Signature.Empty;
					while ((DType)data[pos] != DType.StructEnd)
						fieldsSig += GetNextSignature (ref pos);
					//skip over the )
					pos++;
					return Signature.MakeStruct (fieldsSig);
					//return fieldsSig;
				case DType.DictEntryBegin:
					Signature sigKey = GetNextSignature (ref pos);
					Signature sigValue = GetNextSignature (ref pos);
					//skip over the }
					pos++;
					return Signature.MakeDictEntry (sigKey, sigValue);
				default:
					return new Signature (dtype);
			}
		}

		public Type ToType (ref int pos)
		{
			// TODO: Find a way to avoid these null checks everywhere.
			if (data == null)
				return typeof (void);

			DType dtype = (DType)data[pos++];

			switch (dtype) {
				case DType.Invalid:
					return typeof (void);
				case DType.Byte:
					return typeof (byte);
				case DType.Boolean:
					return typeof (bool);
				case DType.Int16:
					return typeof (short);
				case DType.UInt16:
					return typeof (ushort);
				case DType.Int32:
					return typeof (int);
				case DType.UInt32:
					return typeof (uint);
				case DType.Int64:
					return typeof (long);
				case DType.UInt64:
					return typeof (ulong);
				case DType.Single: ////not supported by libdbus at time of writing
					return typeof (float);
				case DType.Double:
					return typeof (double);
				case DType.String:
					return typeof (string);
				case DType.ObjectPath:
					return typeof (ObjectPath);
				case DType.Signature:
					return typeof (Signature);
				case DType.Array:
					//peek to see if this is in fact a dictionary
					if ((DType)data[pos] == DType.DictEntryBegin) {
						//skip over the {
						pos++;
						Type keyType = ToType (ref pos);
						Type valueType = ToType (ref pos);
						//skip over the }
						pos++;
						//return typeof (IDictionary<,>).MakeGenericType (new Type[] {keyType, valueType});
						//workaround for Mono bug #81035 (memory leak)
						return Mapper.GetGenericType (typeof (IDictionary<,>), new Type[] {keyType, valueType});
					} else {
						return ToType (ref pos).MakeArrayType ();
					}
				case DType.Struct:
					return typeof (ValueType);
				case DType.DictEntry:
					return typeof (System.Collections.Generic.KeyValuePair<,>);
				case DType.Variant:
					return typeof (object);
				default:
					throw new NotSupportedException ("Parsing or converting this signature is not yet supported (signature was '" + this + "'), at DType." + dtype);
			}
		}

		public static Signature GetSig (object[] objs)
		{
			return GetSig (Type.GetTypeArray (objs));
		}

		public static Signature GetSig (Type[] types)
		{
			if (types == null)
				throw new ArgumentNullException ("types");

			Signature sig = Signature.Empty;

			foreach (Type type in types)
					sig += GetSig (type);

			return sig;
		}

		public static Signature GetSig (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");

			//this is inelegant, but works for now
			if (type == typeof (Signature))
				return new Signature (DType.Signature);

			if (type == typeof (ObjectPath))
				return new Signature (DType.ObjectPath);

			if (type == typeof (void))
				return Signature.Empty;

			if (type == typeof (string))
				return new Signature (DType.String);

			if (type == typeof (object))
				return new Signature (DType.Variant);

			if (type.IsArray)
				return GetSig (type.GetElementType ()).MakeArraySignature ();

			if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {

				Type[] genArgs = type.GetGenericArguments ();
				return Signature.MakeDict (GetSig (genArgs[0]), GetSig (genArgs[1]));
			}

			if (Mapper.IsPublic (type)) {
				return new Signature (DType.ObjectPath);
			}

			if (!type.IsPrimitive && !type.IsEnum) {
				Signature sig = Signature.Empty;

				foreach (FieldInfo fi in type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
					sig += GetSig (fi.FieldType);

				return Signature.MakeStruct (sig);
			}

			DType dtype = Signature.TypeToDType (type);
			return new Signature (dtype);
		}
	}

	enum ArgDirection
	{
		In,
		Out,
	}

	enum DType : byte
	{
		Invalid = (byte)'\0',

		Byte = (byte)'y',
		Boolean = (byte)'b',
		Int16 = (byte)'n',
		UInt16 = (byte)'q',
		Int32 = (byte)'i',
		UInt32 = (byte)'u',
		Int64 = (byte)'x',
		UInt64 = (byte)'t',
		Single = (byte)'f', //This is not yet supported!
		Double = (byte)'d',
		String = (byte)'s',
		ObjectPath = (byte)'o',
		Signature = (byte)'g',

		Array = (byte)'a',
		[Obsolete ("Not in protocol")]
		Struct = (byte)'r',
		[Obsolete ("Not in protocol")]
		DictEntry = (byte)'e',
		Variant = (byte)'v',

		StructBegin = (byte)'(',
		StructEnd = (byte)')',
		DictEntryBegin = (byte)'{',
		DictEntryEnd = (byte)'}',
	}
}

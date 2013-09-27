// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	// Allows conversion of complex variants via System.Convert
	class DValue : IConvertible
	{
		// TODO: Note that we currently drop the originating Connection/Message details
		// They may be useful later in conversion!

		internal EndianFlag endianness;
		internal Signature signature;
		internal byte[] data;

		public DValue (EndianFlag endianness, byte[] data)
		{
			this.endianness = endianness;
			this.data = data;
		}

		public bool CanConvertTo (Type conversionType)
		{
			Signature typeSig = Signature.GetSig (conversionType);
			return signature == typeSig;
		}

		public TypeCode GetTypeCode ()
		{
			return TypeCode.Object;
		}

		public object ToType (Type conversionType)
		{
			return ToType (conversionType, null);
		}

		public object ToType (Type conversionType, IFormatProvider provider)
		{
			Signature typeSig = Signature.GetSig (conversionType);
			if (typeSig != signature)
				throw new InvalidCastException ();

			MessageReader reader = new MessageReader (endianness, data);
			return reader.ReadValue (conversionType);
		}

		public override string ToString ()
		{
			// Seems a reasonable way of providing the signature to the API layer
			return signature.ToString ();
		}

		// IConvertible implementation:

		/*
		public TypeCode GetTypeCode ()
		{
			throw new NotSupportedException ();
		}
		*/

		public bool ToBoolean (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public byte ToByte (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public char ToChar (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public DateTime ToDateTime (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public decimal ToDecimal (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public double ToDouble (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public short ToInt16 (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public int ToInt32 (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public long ToInt64 (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public sbyte ToSByte (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public float ToSingle (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public string ToString (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public ushort ToUInt16 (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public uint ToUInt32 (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}

		public ulong ToUInt64 (IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}
	}
}
// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus
{
	class Message
	{
		public Message ()
		{
			Header.Endianness = Connection.NativeEndianness;
			Header.MessageType = MessageType.MethodCall;
			Header.Flags = HeaderFlag.NoReplyExpected; //TODO: is this the right place to do this?
			Header.MajorVersion = Protocol.Version;
			Header.Fields = new Dictionary<byte, object> ();
		}

		public Header Header = new Header ();

		public Connection Connection;

		public Signature Signature
		{
			get {
				object o = Header[FieldCode.Signature];
				if (o == null)
					return Signature.Empty;
				else
					return (Signature)o;
			} set {
				if (value == Signature.Empty)
					Header[FieldCode.Signature] = null;
				else
					Header[FieldCode.Signature] = value;
			}
		}

		public bool ReplyExpected
		{
			get {
				return (Header.Flags & HeaderFlag.NoReplyExpected) == HeaderFlag.None;
			} set {
				if (value)
					Header.Flags &= ~HeaderFlag.NoReplyExpected; //flag off
				else
					Header.Flags |= HeaderFlag.NoReplyExpected; //flag on
			}
		}

		//public HeaderField[] HeaderFields;
		//public Dictionary<FieldCode,object>;

		public byte[] Body;

		//TODO: make use of Locked
		/*
		protected bool locked = false;
		public bool Locked
		{
			get {
				return locked;
			}
		}
		*/

		public void HandleHeader (Header headerIn)
		{
			Header = headerIn;
		}

		static System.Reflection.MethodInfo hHandler = typeof (Message).GetMethod ("HandleHeader");
		public void SetHeaderData (byte[] data)
		{
			EndianFlag endianness = (EndianFlag)data[0];
			MessageReader reader = new MessageReader (endianness, data);

			MethodCaller2 mCaller = ExportObject.GetMCaller (hHandler);
			mCaller (this, reader, null, new MessageWriter ());
		}

		//public HeaderField[] Fields;

		/*
		public void SetHeaderData (byte[] data)
		{
			EndianFlag endianness = (EndianFlag)data[0];
			MessageReader reader = new MessageReader (endianness, data);

			Header = (Header)reader.ReadStruct (typeof (Header));
		}
		*/

		//TypeWriter<Header> headerWriter = TypeImplementer.GetTypeWriter<Header> ();
		public byte[] GetHeaderData ()
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);

			//writer.stream.Capacity = 512;
			//headerWriter (writer, Header);

			writer.Write ((byte)Header.Endianness);
			writer.Write ((byte)Header.MessageType);
			writer.Write ((byte)Header.Flags);
			writer.Write (Header.MajorVersion);
			writer.Write (Header.Length);
			writer.Write (Header.Serial);
			writer.WriteHeaderFields (Header.Fields);

			writer.CloseWrite ();

			return writer.ToArray ();
		}

		public void GetHeaderDataToStream (Stream stream)
		{
			if (Body != null)
				Header.Length = (uint)Body.Length;

			MessageWriter writer = new MessageWriter (Header.Endianness);

			//headerWriter (writer, Header);

			writer.Write ((byte)Header.Endianness);
			writer.Write ((byte)Header.MessageType);
			writer.Write ((byte)Header.Flags);
			writer.Write (Header.MajorVersion);
			writer.Write (Header.Length);
			writer.Write (Header.Serial);
			writer.WriteHeaderFields (Header.Fields);

			writer.CloseWrite ();

			writer.ToStream (stream);
		}
	}

	// Allows conversion of complex variants via System.Convert
	class DValue : IConvertible
	{
		// TODO: Note that we currently drop the originating Connection/Message details
		// They may be useful later in conversion!

		internal EndianFlag endianness;
		internal Signature signature;
		internal byte[] data;

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

		/*
		public object ToType (Type conversionType, IFormatProvider provider)
		{
			throw new NotSupportedException ();
		}
		*/

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

	partial class MessageReader
	{
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
				pos = Protocol.Padded (pos, elemSig.Alignment);
				pos += (int)ln;
				return true;
			}

			int endPos = pos;
			if (sig.GetFixedSize (ref endPos)) {
				pos = endPos;
				return true;
			}

			if (sig.IsDictEntry) {
				pos = Protocol.Padded (pos, sig.Alignment);
				Signature sigKey, sigValue;
				sig.GetDictEntrySignatures (out sigKey, out sigValue);
				if (!StepOver (sigKey))
					return false;
				if (!StepOver (sigValue))
					return false;
				return true;
			}

			if (sig.IsStruct) {
				pos = Protocol.Padded (pos, sig.Alignment);
				foreach (Signature fieldSig in sig.GetFieldSignatures ())
					if (!StepOver (fieldSig))
						return false;
				return true;
			}

			throw new Exception ("Can't step over '" + sig + "'");
			//return false;
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
				pos = Protocol.Padded (pos, sig.Alignment);
				Signature sigKey, sigValue;
				sig.GetDictEntrySignatures (out sigKey, out sigValue);
				yield return sigKey;
				yield return sigValue;
				yield break;
			}

			if (sig.IsStruct) {
				pos = Protocol.Padded (pos, sig.Alignment);
				foreach (Signature fieldSig in sig.GetFieldSignatures ())
					yield return fieldSig;
				yield break;
			}

			throw new Exception ("Can't step into '" + sig + "'");
			//yield break;
		}
	}

	class MessageDumper
	{
		public static byte[] ReadBlock (TextReader r)
		{
			//if (Body == null)
			//	return;

			MemoryStream ms = new MemoryStream ();

			while (true) {
				string ln = r.ReadLine ();
				if (ln == null)
					break;
				if (!ReadFromHex (ms, ln))
					break;
			}

			if (ms.Length == 0)
				return null;

			return ms.ToArray ();
		}

		public static void WriteComment (string comment, TextWriter w)
		{
			w.WriteLine ("# " + comment);
		}

		public static void WriteBlock (byte[] Body, TextWriter w)
		{
			//if (Body == null)
			//	return;
			if (Body != null)
			for (int i = 0 ; i != Body.Length ; i++) {
				if (i == 0) {}
				else if (i % 32 == 0)
					w.WriteLine ();
				else if (i % 4 == 0)
					w.Write (' ');

				w.Write (Body[i].ToString ("x2", System.Globalization.CultureInfo.InvariantCulture));
			}

			w.Write ('.');
			w.WriteLine ();
			w.Flush ();
		}

		public static void WriteMessage (Message msg, TextWriter w)
		{
			w.WriteLine ("# Message");
			w.WriteLine ("# Header");
			MessageDumper.WriteBlock (msg.GetHeaderData (), w);
			w.WriteLine ("# Body");
			MessageDumper.WriteBlock (msg.Body, w);
			w.WriteLine ();
			w.Flush ();
		}

		public static Message ReadMessage (TextReader r)
		{
			byte[] header = MessageDumper.ReadBlock (r);

			if (header == null)
				return null;

			byte[] body = MessageDumper.ReadBlock (r);

			Message msg = new Message ();
			msg.SetHeaderData (header);
			msg.Body = body;

			return msg;
		}

		static byte FromHexChar (char c)
		{
			if ((c >= 'a') && (c <= 'f'))
				return (byte) (c - 'a' + 10);
			if ((c >= 'A') && (c <= 'F'))
				return (byte) (c - 'A' + 10);
			if ((c >= '0') && (c <= '9'))
				return (byte) (c - '0');
			throw new ArgumentException ("Invalid hex char");
		}

		static bool ReadFromHex (Stream ms, string hex)
		{
			if (hex.StartsWith ("#"))
				return true;

			int i = 0;
			while (i < hex.Length) {
				if (char.IsWhiteSpace (hex [i])) {
					i++;
					continue;
				}

				if (hex [i] == '.') {
					ms.Flush ();
					return false;
				}

				byte res = (byte) (FromHexChar (hex [i++]) << 4);
				res += FromHexChar (hex [i++]);
				ms.WriteByte (res);
			}

			ms.Flush ();
			return true;
		}
	}
}

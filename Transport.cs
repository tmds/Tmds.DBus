// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DBus.Protocol;

namespace DBus.Transports
{
	abstract class Transport
	{
		readonly object writeLock = new object ();

		protected Connection connection;

		public Stream stream;
		public long socketHandle;
		internal Queue<Message> Inbound = new Queue<Message> ();
		byte[] mmbuf = null;

		int mmpos = 0;
		int mmneeded = 16;
		IEnumerator<MsgState> msgRdr;

		public event EventHandler WakeUp;

		public static Transport Create (AddressEntry entry)
		{
			switch (entry.Method) {
				case "tcp":
				{
					Transport transport = new SocketTransport ();
					transport.Open (entry);
					return transport;
				}
#if !PORTABLE
				case "unix":
				{
					Transport transport = new UnixNativeTransport ();
					transport.Open (entry);
					return transport;
				}
#endif
#if ENABLE_PIPES
				case "win": {
					Transport transport = new PipeTransport ();
					transport.Open (entry);
					return transport;
				}
#endif
				default:
					throw new NotSupportedException ("Transport method \"" + entry.Method + "\" not supported");
			}
		}

		public abstract void Open (AddressEntry entry);
		public abstract string AuthString ();
		public abstract void WriteCred ();

		public Connection Connection
		{
			get {
				return connection;
			} set {
				connection = value;
			}
		}

		public Stream Stream {
			get {
				return stream;
			}
			set {
				stream = value;
			}
		}

		public long SocketHandle {
			get {
				return socketHandle;
			}
			set {
				socketHandle = value;
			}
		}

		public virtual bool TryGetPeerPid (out uint pid)
		{
			pid = 0;
			return false;
		}


		public virtual void Disconnect ()
		{
			stream.Dispose ();
		}

		protected void FireWakeUp ()
		{
			if (WakeUp != null)
				WakeUp (this, EventArgs.Empty);
		}

		internal Message TryReadMessage ()
		{
			GetData ();
			if (Inbound.Count > 0)
				return Inbound.Dequeue ();
			return null;
		}

		public void Iterate ()
		{
			GetData ();
		}

		internal Message ReadMessage ()
		{
			// Hack to complete pending async reads in progress.
			while (msgRdr != null)
				GetData ();

			try {
				return ReadMessageReal ();
			} catch (IOException e) {
				if (ProtocolInformations.Verbose)
					Console.Error.WriteLine (e.Message);
				connection.IsConnected = false;
				return null;
			}
		}

		int Read (byte[] buffer, int offset, int count)
		{
			int read = 0;
			//System.Net.Sockets.Networkstream nns = ns as System.Net.Sockets.Networkstream;
			//SocketTransport st = this as SocketTransport;
			while (read < count) {
				// FIXME: Remove this hack to support non-blocking sockets on Windows
				//if (st != null && st.socket.Blocking == false && nns != null && !nns.DataAvailable) {
				/*
				if (nns != null && !nns.DataAvailable) {
					System.Threading.Thread.Sleep (10);
					continue;
				}
				*/
				int nread = stream.Read (buffer, offset + read, count - read);
				if (nread == 0)
					break;
				read += nread;
			}

			//if (read < count)
			//	throw new Exception ();

			if (read > count)
				throw new Exception ();

			return read;
		}

		public void GetData ()
		{
			if (msgRdr == null) {
				msgRdr = ReadMessageReal2 ();
			}

			SocketTransport st = this as SocketTransport;

			int avail = st.socket.Available;
			if (mmneeded == 0)
				throw new Exception ();

			if (avail == 0)
				return;

			avail = Math.Min (avail, mmneeded);
			int nread = st.socket.Receive (mmbuf, mmpos, avail, System.Net.Sockets.SocketFlags.None);
			mmpos += nread;
			mmneeded -= nread;
			if (!msgRdr.MoveNext ())
				throw new Exception ();

			MsgState state = msgRdr.Current;
			if (state != MsgState.Done)
				return;

			mmpos = 0;
			mmneeded = 16;

			msgRdr = null;
		}

		enum MsgState
		{
			Wait16,
			WaitHeader,
			WaitBody,
			Done,
		}

		IEnumerator<MsgState> ReadMessageReal2 ()
		{
			byte[] body = null;
			mmneeded = 16;
			while (mmpos < 16)
				yield return MsgState.Wait16;

			EndianFlag endianness = (EndianFlag)mmbuf[0];
			MessageReader reader = new MessageReader (endianness, mmbuf);

			//discard the endian byte as we've already read it
			reader.ReadByte ();

			//discard message type and flags, which we don't care about here
			reader.ReadByte ();
			reader.ReadByte ();

			byte version = reader.ReadByte ();

			if (version < ProtocolInformations.MinVersion || version > ProtocolInformations.MaxVersion)
				throw new NotSupportedException ("Protocol version '" + version.ToString () + "' is not supported");

			if (ProtocolInformations.Verbose)
				if (version != ProtocolInformations.Version)
					Console.Error.WriteLine ("Warning: Protocol version '" + version.ToString () + "' is not explicitly supported but may be compatible");

			uint bodyLength = reader.ReadUInt32 ();
			//discard serial
			reader.ReadUInt32 ();
			uint headerLength = reader.ReadUInt32 ();

			//this check may become relevant if a future version of the protocol allows larger messages
			/*
			if (bodyLength > Int32.MaxValue || headerLength > Int32.MaxValue)
				throw new NotImplementedException ("Long messages are not yet supported");
			*/

			int bodyLen = (int)bodyLength;
			int toRead = (int)headerLength;

			//we fixup to include the padding following the header
			toRead = ProtocolInformations.Padded (toRead, 8);

			long msgLength = toRead + bodyLen;
			if (msgLength > ProtocolInformations.MaxMessageLength)
				throw new Exception ("Message length " + msgLength + " exceeds maximum allowed " + ProtocolInformations.MaxMessageLength + " bytes");

			byte[] header = new byte[16 + toRead];
			Array.Copy (mmbuf, header, 16);

			mmneeded = toRead;
			while (mmpos < 16 + toRead)
				yield return MsgState.WaitHeader;

			Array.Copy (mmbuf, 16, header, 16, toRead);

			//if (read != toRead)
			//	throw new Exception ("Message header length mismatch: " + read + " of expected " + toRead);

			mmneeded = bodyLen;
			while (mmpos < 16 + toRead + bodyLen)
				yield return MsgState.WaitBody;

			//read the body
			if (bodyLen != 0) {
				body = new byte[bodyLen];

				Array.Copy (mmbuf, 16 + toRead, body, 0, bodyLen);

				//if (read != bodyLen)
				//	throw new Exception ("Message body length mismatch: " + read + " of expected " + bodyLen);
			}

			Message msg = Message.FromReceivedBytes (Connection, header, body);

			Inbound.Enqueue (msg);

			mmneeded = 16;

			yield return MsgState.Done;
		}

		Message ReadMessageReal ()
		{
			byte[] header;
			byte[] body = null;

			int read;

			//16 bytes is the size of the fixed part of the header
			byte[] hbuf = new byte[16];

			read = Read (hbuf, 0, 16);

			if (read == 0)
				return null;

			if (read != 16)
				throw new Exception ("Header read length mismatch: " + read + " of expected " + "16");

			EndianFlag endianness = (EndianFlag)hbuf[0];
			MessageReader reader = new MessageReader (endianness, hbuf);

			//discard endian byte, message type and flags, which we don't care about here
			reader.Seek (3);

			byte version = reader.ReadByte ();

			if (version < ProtocolInformations.MinVersion || version > ProtocolInformations.MaxVersion)
				throw new NotSupportedException ("Protocol version '" + version.ToString () + "' is not supported");

			if (ProtocolInformations.Verbose)
				if (version != ProtocolInformations.Version)
					Console.Error.WriteLine ("Warning: Protocol version '" + version.ToString () + "' is not explicitly supported but may be compatible");

			uint bodyLength = reader.ReadUInt32 ();
			//discard serial
			reader.ReadUInt32 ();
			uint headerLength = reader.ReadUInt32 ();

			//this check may become relevant if a future version of the protocol allows larger messages
			/*
			if (bodyLength > Int32.MaxValue || headerLength > Int32.MaxValue)
				throw new NotImplementedException ("Long messages are not yet supported");
			*/

			int bodyLen = (int)bodyLength;
			int toRead = (int)headerLength;

			//we fixup to include the padding following the header
			toRead = ProtocolInformations.Padded (toRead, 8);

			long msgLength = toRead + bodyLen;
			if (msgLength > ProtocolInformations.MaxMessageLength)
				throw new Exception ("Message length " + msgLength + " exceeds maximum allowed " + ProtocolInformations.MaxMessageLength + " bytes");

			header = new byte[16 + toRead];
			Array.Copy (hbuf, header, 16);

			read = Read (header, 16, toRead);

			if (read != toRead)
				throw new Exception ("Message header length mismatch: " + read + " of expected " + toRead);

			//read the body
			if (bodyLen != 0) {
				body = new byte[bodyLen];

				read = Read (body, 0, bodyLen);

				if (read != bodyLen)
					throw new Exception ("Message body length mismatch: " + read + " of expected " + bodyLen);
			}

			Message msg = Message.FromReceivedBytes (Connection, header, body);

			return msg;
		}

		internal virtual void WriteMessage (Message msg)
		{
			lock (writeLock) {
				msg.Header.GetHeaderDataToStream (stream);
				if (msg.Body != null && msg.Body.Length != 0)
					stream.Write (msg.Body, 0, msg.Body.Length);
				stream.Flush ();
			}
		}
	}
}

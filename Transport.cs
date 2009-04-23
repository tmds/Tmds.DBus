// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;

namespace NDesk.DBus.Transports
{
	abstract class Transport
	{
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
					//Transport transport = new UnixMonoTransport ();
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

		protected Connection connection;

		public Connection Connection
		{
			get {
				return connection;
			} set {
				connection = value;
			}
		}

		//TODO: design this properly

		//this is just a temporary solution
		public Stream Stream;
		public long SocketHandle;
		public abstract void Open (AddressEntry entry);
		public abstract string AuthString ();
		public abstract void WriteCred ();
		/*
	}

	abstract class StreamTransport : Transport
	{
	*/
		Stream ns {
			get {
				return this.Stream;
			}
		}

		public virtual void Disconnect ()
		{
			ns.Dispose ();
		}

		internal Message ReadMessage ()
		{
			try {
				return ReadMessageReal ();
			} catch (IOException e) {
				if (Protocol.Verbose)
					Console.Error.WriteLine (e.Message);
				return null;
			}
		}

		int Read (byte[] buffer, int offset, int count)
		{
			int read = 0;
			System.Net.Sockets.NetworkStream nns = ns as System.Net.Sockets.NetworkStream;
			SocketTransport st = this as SocketTransport;
			while (read < count) {
				// FIXME: Remove this hack to support non-blocking sockets on Windows
				/*
				if (st != null && st.socket.Blocking == false && nns != null && !nns.DataAvailable) {
					System.Threading.Thread.Sleep (10);
					continue;
				}
				*/
				int nread = ns.Read (buffer, offset + read, count - read);
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

			//discard the endian byte as we've already read it
			reader.ReadByte ();

			//discard message type and flags, which we don't care about here
			reader.ReadByte ();
			reader.ReadByte ();

			byte version = reader.ReadByte ();

			if (version < Protocol.MinVersion || version > Protocol.MaxVersion)
				throw new NotSupportedException ("Protocol version '" + version.ToString () + "' is not supported");

			if (Protocol.Verbose)
				if (version != Protocol.Version)
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
			toRead = Protocol.Padded (toRead, 8);

			long msgLength = toRead + bodyLen;
			if (msgLength > Protocol.MaxMessageLength)
				throw new Exception ("Message length " + msgLength + " exceeds maximum allowed " + Protocol.MaxMessageLength + " bytes");

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

			Message msg = new Message ();
			msg.Connection = this.Connection;
			msg.Body = body;
			msg.SetHeaderData (header);

			return msg;
		}

		object writeLock = new object ();
		internal virtual void WriteMessage (Message msg)
		{
			byte[] HeaderData = msg.GetHeaderData ();

			long msgLength = HeaderData.Length + (msg.Body != null ? msg.Body.Length : 0);
			if (msgLength > Protocol.MaxMessageLength)
				throw new Exception ("Message length " + msgLength + " exceeds maximum allowed " + Protocol.MaxMessageLength + " bytes");

			lock (writeLock) {
				ns.Write (HeaderData, 0, HeaderData.Length);
				if (msg.Body != null && msg.Body.Length != 0)
					ns.Write (msg.Body, 0, msg.Body.Length);
				ns.Flush ();
			}

		}
	}
}

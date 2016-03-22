// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DBus.Protocol;
using System.Diagnostics;

namespace DBus.Transports
{
	abstract class Transport
	{
		readonly object writeLock = new object ();

		[ThreadStatic]
		static byte[] readBuffer;

		protected Connection connection;

		public Stream stream;
		public long socketHandle;

		public event EventHandler WakeUp;

		const string DBUS_DAEMON_LAUNCH_COMMAND = "dbus-launch";

		public static Transport Create (AddressEntry entry)
		{
			switch (entry.Method) {
				case "tcp":
				{
					Transport transport = new SocketTransport ();
					transport.Open (entry);
					return transport;
				}

				case "unix":
				{
					if (OSHelpers.PlatformIsUnixoid) {
						Transport transport = new UnixNativeTransport ();
						transport.Open (entry);
						return transport;
					}
					break;
				}

#if ENABLE_PIPES
				case "win":
				{
					Transport transport = new PipeTransport ();
					transport.Open (entry);
					return transport;
				}
#endif

				// "autolaunch:" means: the first client user of the dbus library shall spawn the daemon on itself, see dbus 1.7.8 from http://dbus.freedesktop.org/releases/dbus/
				case "autolaunch":
				{
					if (OSHelpers.PlatformIsUnixoid)
						break;

					string addr = Address.GetSessionBusAddressFromSharedMemory ();

					if (string.IsNullOrEmpty (addr)) { // we have to launch the daemon ourselves
						string oldDir = Directory.GetCurrentDirectory ();
						// Without this, the "current" folder for the new process will be the one where the current
						// executable resides, and as a consequence,that folder cannot be relocated/deleted unless the daemon is stopped
						Directory.SetCurrentDirectory (Environment.GetFolderPath (Environment.SpecialFolder.System));

						Process process = Process.Start (DBUS_DAEMON_LAUNCH_COMMAND);
						if (process == null) {
							Directory.SetCurrentDirectory (oldDir);
							throw new NotSupportedException ("Transport method \"autolaunch:\" - cannot launch dbus daemon '" + DBUS_DAEMON_LAUNCH_COMMAND + "'");
						}

						// wait for daemon
						Stopwatch stopwatch = new Stopwatch ();
						stopwatch.Start ();
						do {
							addr = Address.GetSessionBusAddressFromSharedMemory ();
							if (String.IsNullOrEmpty (addr))
								Thread.Sleep (100);
						} while (String.IsNullOrEmpty (addr) && stopwatch.ElapsedMilliseconds <= 5000);

						Directory.SetCurrentDirectory (oldDir);
					}

					if (string.IsNullOrEmpty (addr))
						throw new NotSupportedException ("Transport method \"autolaunch:\" - timeout during access to freshly launched dbus daemon"); 
					return Create (AddressEntry.Parse (addr));
				}

			}

			throw new NotSupportedException ("Transport method \"" + entry.Method + "\" not supported");
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

		internal Message ReadMessage ()
		{
			Message msg;

			try {
				msg = ReadMessageReal ();
			} catch (IOException e) {
				if (ProtocolInformation.Verbose)
					Console.Error.WriteLine (e.Message);
				connection.IsConnected = false;
				msg = null;
			}
         
			if (connection != null && connection.Monitors != null)
				connection.Monitors (msg);

			return msg;
		}

		int Read (byte[] buffer, int offset, int count)
		{
			int read = 0;
			while (read < count) {
				int nread = stream.Read (buffer, offset + read, count - read);
				if (nread == 0)
					break;
				read += nread;
			}

			if (read > count)
				throw new Exception ();

			return read;
		}

		Message ReadMessageReal ()
		{
			byte[] header = null;
			byte[] body = null;

			int read;

			//16 bytes is the size of the fixed part of the header
			if (readBuffer == null)
				readBuffer = new byte[16];
			byte[] hbuf = readBuffer;

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

			if (version < ProtocolInformation.MinVersion || version > ProtocolInformation.MaxVersion)
				throw new NotSupportedException ("Protocol version '" + version.ToString () + "' is not supported");

			if (ProtocolInformation.Verbose)
				if (version != ProtocolInformation.Version)
					Console.Error.WriteLine ("Warning: Protocol version '" + version.ToString () + "' is not explicitly supported but may be compatible");

			uint bodyLength = reader.ReadUInt32 ();
			//discard serial
			reader.ReadUInt32 ();
			uint headerLength = reader.ReadUInt32 ();

			int bodyLen = (int)bodyLength;
			int toRead = (int)headerLength;

			//we fixup to include the padding following the header
			toRead = ProtocolInformation.Padded (toRead, 8);

			long msgLength = toRead + bodyLen;
			if (msgLength > ProtocolInformation.MaxMessageLength)
				throw new Exception ("Message length " + msgLength + " exceeds maximum allowed " + ProtocolInformation.MaxMessageLength + " bytes");

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

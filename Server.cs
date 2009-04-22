// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NDesk.DBus
{
	//TODO: complete this class
	abstract class Server
	{
		// Was Listen()
		public static Server ListenAt (string address)
		{
			AddressEntry[] entries = NDesk.DBus.Address.Parse (address);

			foreach (AddressEntry entry in entries) {
				try {
					switch (entry.Method) {
						case "tcp":
							return new TcpServer (entry.ToString ());
						case "unix":
							return new UnixServer (entry.ToString ());
#if ENABLE_PIPES
						case "win":
							return new WinServer (entry.ToString ());
#endif
					}
				} catch (Exception e) {
					if (Protocol.Verbose)
						Console.Error.WriteLine (e.Message);
				}
			}

			// TODO: Should call Listen on the Server?

			return null;

			/*
			server.address = address;

			server.Id = entry.GUID;
			if (server.Id == UUID.Zero)
				server.Id = UUID.Generate ();
			*/
		}

		public abstract void Listen ();

		public abstract void Disconnect ();

		public virtual bool IsConnected
		{
			get {
				return true;
			}
		}

		internal string address;
		public string Address
		{
			get {
				return address;
			}
		}

		public UUID Id = UUID.Zero;

		public abstract event Action<Connection> NewConnection;

		// FIXME: The follow fields do not belong here!
		// TODO: Make these a thread-specific CallContext prop
		[ThreadStatic]
		public Connection CurrentMessageConnection;
		[ThreadStatic]
		public Message CurrentMessage;
		public ServerBus SBus = null;
	}
}

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
		public static void Listen (string address)
		{
			AddressEntry[] entries = Address.Parse (address);
			AddressEntry entry = entries[0];

			Server server;
			if (entry.Method == "tcp")
				//server = new TcpServer ();
				server = new TcpServer (entry.ToString ());
			else
				throw new Exception ("");

			/*
			server.address = address;

			server.Id = entry.GUID;
			if (server.Id == UUID.Zero)
				server.Id = UUID.Generate ();
			*/
		}

		public abstract void Disconnect ();

		public virtual bool IsConnected
		{
			get {
				return true;
			}
		}

		internal string address;
		/*
		public string Address
		{
			get {
				return address;
			}
		}
		*/

		public UUID Id = UUID.Zero;

		public abstract event Action<Connection> NewConnection;

		//TODO: new connection event/virtual method
	}
}

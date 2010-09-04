// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#if ENABLE_PIPES
using System.IO.Pipes;
#endif

namespace DBus
{
	using Unix;
	using Transports;
	using Authentication;

	//TODO: complete this class
	abstract class Server
	{
		// Was Listen()
		public static Server ListenAt (string address)
		{
			AddressEntry[] entries = DBus.Address.Parse (address);

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

	class UnixServer : Server
	{
		string unixPath = null;
		bool isAbstract;

		public UnixServer (string address)
		{
			AddressEntry[] entries = DBus.Address.Parse (address);
			AddressEntry entry = entries[0];

			if (entry.Method != "unix")
				throw new Exception ();

			string val;
			if (entry.Properties.TryGetValue ("path", out val)) {
				unixPath = val;
				isAbstract = false;
			} else if (entry.Properties.TryGetValue ("abstract", out val)) {
				unixPath = val;
				isAbstract = true;
			}

			if (String.IsNullOrEmpty (unixPath))
				throw new Exception ("Address path is invalid");

			if (entry.GUID == UUID.Zero)
				entry.GUID = UUID.Generate ();
			Id = entry.GUID;

			/*
			Id = entry.GUID;
			if (Id == UUID.Zero)
				Id = UUID.Generate ();
			*/

			this.address = entry.ToString ();
			//Console.WriteLine ("Server address: " + Address);
		}

		public override void Disconnect ()
		{
		}

		bool AcceptClient (UnixSocket csock, out ServerConnection conn)
		{
			//TODO: use the right abstraction here, probably using the Server class
			UnixNativeTransport transport = new UnixNativeTransport ();
			//client.Client.Blocking = true;
			transport.socket = csock;
			transport.SocketHandle = (long)csock.Handle;
			transport.Stream = new UnixStream (csock);
			//Connection conn = new Connection (transport);
			//Connection conn = new ServerConnection (transport);
			//ServerConnection conn = new ServerConnection (transport);
			conn = new ServerConnection (transport);
			conn.Server = this;
			conn.Id = Id;

			if (conn.Transport.Stream.ReadByte () != 0)
				return false;

			conn.isConnected = true;

			SaslPeer remote = new SaslPeer ();
			remote.stream = transport.Stream;
			SaslServer local = new SaslServer ();
			local.stream = transport.Stream;
			local.Guid = Id;

			local.Peer = remote;
			remote.Peer = local;

			bool success = local.Authenticate ();

			Console.WriteLine ("Success? " + success);

			if (!success)
				return false;

			conn.UserId = ((SaslServer)local).uid;

			conn.isAuthenticated = true;

			return true;
		}

		public override void Listen ()
		{
			byte[] sa = isAbstract ? UnixNativeTransport.GetSockAddrAbstract (unixPath) : UnixNativeTransport.GetSockAddr (unixPath);
			UnixSocket usock = new UnixSocket ();
			usock.Bind (sa);
			usock.Listen (50);

			while (true) {
				Console.WriteLine ("Waiting for client on " + (isAbstract ? "abstract " : String.Empty) + "path " + unixPath);
				UnixSocket csock = usock.Accept ();
				Console.WriteLine ("Client connected");

				ServerConnection conn;
				if (!AcceptClient (csock, out conn)) {
					Console.WriteLine ("Client rejected");
					csock.Close ();
					continue;
				}

				//GLib.Idle.Add (delegate {

				if (NewConnection != null)
					NewConnection (conn);

				//BusG.Init (conn);
				/*
				conn.Iterate ();
				Console.WriteLine ("done iter");
				BusG.Init (conn);
				Console.WriteLine ("done init");
				*/

				//GLib.Idle.Add (delegate { BusG.Init (conn); return false; });
	#if USE_GLIB
				BusG.Init (conn);
	#else
				new Thread (new ThreadStart (delegate { while (conn.IsConnected) conn.Iterate (); })).Start ();
	#endif
				Console.WriteLine ("done init");


				//return false;
				//});
			}
		}

		/*
		public void ConnectionLost (Connection conn)
		{
		}
		*/

		public override event Action<Connection> NewConnection;
	}

	class TcpServer : Server
	{
		uint port = 0;
		public TcpServer (string address)
		{
			AddressEntry[] entries = DBus.Address.Parse (address);
			AddressEntry entry = entries[0];

			if (entry.Method != "tcp")
				throw new Exception ();

			string val;
			if (entry.Properties.TryGetValue ("port", out val))
				port = UInt32.Parse (val);

			if (entry.GUID == UUID.Zero)
				entry.GUID = UUID.Generate ();
			Id = entry.GUID;

			/*
			Id = entry.GUID;
			if (Id == UUID.Zero)
				Id = UUID.Generate ();
			*/

			this.address = entry.ToString ();
			//Console.WriteLine ("Server address: " + Address);
		}

		public override void Disconnect ()
		{
		}

		bool AcceptClient (TcpClient client, out ServerConnection conn)
		{
			//TODO: use the right abstraction here, probably using the Server class
			SocketTransport transport = new SocketTransport ();
			client.Client.Blocking = true;
			transport.SocketHandle = (long)client.Client.Handle;
			transport.Stream = client.GetStream ();
			//Connection conn = new Connection (transport);
			//Connection conn = new ServerConnection (transport);
			//ServerConnection conn = new ServerConnection (transport);
			conn = new ServerConnection (transport);
			conn.Server = this;
			conn.Id = Id;

			if (conn.Transport.Stream.ReadByte () != 0)
				return false;

			conn.isConnected = true;

			SaslPeer remote = new SaslPeer ();
			remote.stream = transport.Stream;
			SaslServer local = new SaslServer ();
			local.stream = transport.Stream;
			local.Guid = Id;

			local.Peer = remote;
			remote.Peer = local;

			bool success = local.Authenticate ();

			Console.WriteLine ("Success? " + success);

			if (!success)
				return false;

			conn.UserId = ((SaslServer)local).uid;

			conn.isAuthenticated = true;

			return true;
		}

		public override void Listen ()
		{
			TcpListener server = new TcpListener (IPAddress.Any, (int)port);
			server.Server.Blocking = true;

			server.Start ();

			while (true) {
				//Console.WriteLine ("Waiting for client on TCP port " + port);
				TcpClient client = server.AcceptTcpClient ();
				/*
				client.NoDelay = true;
				client.ReceiveBufferSize = (int)Protocol.MaxMessageLength;
				client.SendBufferSize = (int)Protocol.MaxMessageLength;
				*/
				//Console.WriteLine ("Client connected");

				ServerConnection conn;
				if (!AcceptClient (client, out conn)) {
					Console.WriteLine ("Client rejected");
					client.Close ();
					continue;
				}

				//client.Client.Blocking = false;


				//GLib.Idle.Add (delegate {

				if (NewConnection != null)
					NewConnection (conn);

				//BusG.Init (conn);
				/*
				conn.Iterate ();
				Console.WriteLine ("done iter");
				BusG.Init (conn);
				Console.WriteLine ("done init");
				*/

				//GLib.Idle.Add (delegate { BusG.Init (conn); return false; });
	#if USE_GLIB
				BusG.Init (conn);
	#else
				new Thread (new ThreadStart (delegate { while (conn.IsConnected) conn.Iterate (); })).Start ();
	#endif
				//Console.WriteLine ("done init");

				//return false;
				//});
			}
		}

		/*
		public void ConnectionLost (Connection conn)
		{
		}
		*/

		public override event Action<Connection> NewConnection;
	}

	#if ENABLE_PIPES
	class WinServer : Server
	{
		string pipePath;

		public WinServer (string address)
		{
			AddressEntry[] entries = DBus.Address.Parse (address);
			AddressEntry entry = entries[0];

			if (entry.Method != "win")
				throw new Exception ();

			string val;
			if (entry.Properties.TryGetValue ("path", out val)) {
				pipePath = val;
			}

			if (String.IsNullOrEmpty (pipePath))
				throw new Exception ("Address path is invalid");

			if (entry.GUID == UUID.Zero)
				entry.GUID = UUID.Generate ();
			Id = entry.GUID;

			/*
			Id = entry.GUID;
			if (Id == UUID.Zero)
				Id = UUID.Generate ();
			*/

			this.address = entry.ToString ();
			Console.WriteLine ("Server address: " + Address);
		}

		public override void Disconnect ()
		{
		}

		bool AcceptClient (PipeStream client, out ServerConnection conn)
		{
			PipeTransport transport = new PipeTransport ();
			//client.Client.Blocking = true;
			//transport.SocketHandle = (long)client.Client.Handle;
			transport.Stream = client;
			conn = new ServerConnection (transport);
			conn.Server = this;
			conn.Id = Id;

			if (conn.Transport.Stream.ReadByte () != 0)
				return false;

			conn.isConnected = true;

			SaslPeer remote = new SaslPeer ();
			remote.stream = transport.Stream;
			SaslServer local = new SaslServer ();
			local.stream = transport.Stream;
			local.Guid = Id;

			local.Peer = remote;
			remote.Peer = local;

			bool success = local.Authenticate ();
			//bool success = true;

			Console.WriteLine ("Success? " + success);

			if (!success)
				return false;

			conn.UserId = ((SaslServer)local).uid;

			conn.isAuthenticated = true;

			return true;
		}

		static int numPipeThreads = 16;

		public override void Listen ()
		{
			// TODO: Use a ThreadPool to have an adaptive number of reusable threads.
			for (int i = 0; i != numPipeThreads; i++) {
				Thread newThread = new Thread (new ThreadStart (DoListen));
				newThread.Name = "DBusPipeServer" + i;
				// Hack to allow shutdown without Joining threads for now.
				newThread.IsBackground = true;
				newThread.Start ();
			}

			Console.WriteLine ("Press enter to exit.");
			Console.ReadLine ();
		}

		void DoListen ()
		{
			while (true)
			using (NamedPipeServerStream pipeServer = new NamedPipeServerStream (pipePath, PipeDirection.InOut, numPipeThreads, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, (int)Protocol.MaxMessageLength, (int)Protocol.MaxMessageLength)) {
				Console.WriteLine ("Waiting for client on path " + pipePath);
				pipeServer.WaitForConnection ();

				Console.WriteLine ("Client connected");

				ServerConnection conn;
				if (!AcceptClient (pipeServer, out conn)) {
					Console.WriteLine ("Client rejected");
					pipeServer.Disconnect ();
					continue;
				}

				pipeServer.Flush ();
				pipeServer.WaitForPipeDrain ();

				if (NewConnection != null)
					NewConnection (conn);

				while (conn.IsConnected)
					conn.Iterate ();

				pipeServer.Disconnect ();
			}
		}

		/*
		public void ConnectionLost (Connection conn)
		{
		}
		*/

		public override event Action<Connection> NewConnection;
	}
#endif
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;
using org.freedesktop.DBus;

using System.IO;
using System.Net;
using System.Net.Sockets;
using Mono.Unix;

using System.Threading;

public class TestServer
{
	//TODO: complete this test daemon/server example, and a client
	//TODO: maybe generalise it and integrate it into the core
	public static void Main (string[] args)
	{
		bool isServer;

		if (args.Length == 1 && args[0] == "server")
			isServer = true;
		else if (args.Length == 1 && args[0] == "client")
			isServer = false;
		else {
			Console.Error.WriteLine ("Usage: test-server [server|client]");
			return;
		}

		string addr = "unix:abstract=/tmp/dbus-ABCDEFGHIJ";

		Connection conn;

		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");
		string myNameReq = "org.ndesk.test";

		if (!isServer) {
			conn = new Connection (false);
			conn.Open (addr);
			DemoObject demo = conn.GetObject<DemoObject> (myNameReq, myOpath);
			//float ret = demo.Hello ("hi from test client", 21);
			float ret = 200;
			while (ret > 5) {
				ret = demo.Hello ("hi from test client", (int)ret);
				Console.WriteLine ("Returned float: " + ret);
				System.Threading.Thread.Sleep (1000);
			}
		} else {
			string path;
			bool abstr;

			Address.Parse (addr, out path, out abstr);

			AbstractUnixEndPoint ep = new AbstractUnixEndPoint (path);
			Socket server = new Socket (AddressFamily.Unix, SocketType.Stream, 0);

			server.Bind (ep);
			//server.Listen (1);
			server.Listen (5);

			while (true) {
				Console.WriteLine ("Waiting for client on " + addr);
				Socket client = server.Accept ();
				Console.WriteLine ("Client accepted");

				//this might well be wrong, untested and doesn't yet work here onwards
				conn = new Connection (false);
				//conn.Open (path, @abstract);
				conn.sock = client;
				conn.sock.Blocking = true;

				PeerCred pc = new PeerCred (conn.sock);
				Console.WriteLine ("PeerCred: pid={0}, uid={1}, gid={2}", pc.ProcessID, pc.UserID, pc.GroupID);

				conn.ns = new NetworkStream (conn.sock);

				//ConnectionHandler.Handle (conn);

				//in reality a thread per connection is of course too expensive
				ConnectionHandler hnd = new ConnectionHandler (conn);
				new Thread (new ThreadStart (hnd.Handle)).Start ();

				Console.WriteLine ();
			}
		}
	}
}

public class ConnectionHandler
{
	protected Connection conn;

	public ConnectionHandler (Connection conn)
	{
		this.conn = conn;
	}

	public void Handle ()
	{
		ConnectionHandler.Handle (conn);
	}

	public static void Handle (Connection conn)
	{
		//Connection.tmpConn = conn;

		string myNameReq = "org.ndesk.test";
		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");

		DemoObject demo = new DemoObject ();
		conn.Marshal (demo, myNameReq, myOpath);

		//TODO: handle lost connections etc. properly instead of stupido try/catch
		try {
		while (true)
			conn.Iterate ();
		} catch (Exception e) {
			//Console.Error.WriteLine (e);
		}

		conn.Unmarshal (myNameReq, myOpath);
	}
}

[Interface ("org.ndesk.test")]
public class DemoObject : MarshalByRefObject
{
	public float Hello (string arg0, int arg1)
	{
		Console.WriteLine ("Got a Hello(" + arg0 + ", " + arg1 +")");

		return (float)arg1/2;
	}
}

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

		DemoObject demo;


		ObjectPath myOpath = new ObjectPath ("/test");
		string myNameReq = "org.ndesk.test";


		if (!isServer) {
			conn = new Connection (false);
			conn.Open (addr);
			demo = conn.GetObject<DemoObject> (myNameReq, myOpath);
			float ret = demo.Hello ("hi from test client", 21);
			Console.WriteLine ("Returned float: " + ret);
		} else {
			string path;
			bool abstr;

			Address.Parse (addr, out path, out abstr);

			AbstractUnixEndPoint ep = new AbstractUnixEndPoint (path);
			Socket server = new Socket (AddressFamily.Unix, SocketType.Stream, 0);

			server.Bind (ep);
			server.Listen (1);

			Console.WriteLine ("Waiting for client on " + addr);
			Socket client = server.Accept ();
			Console.WriteLine ("Client accepted");

			//this might well be wrong, untested and doesn't yet work here onwards
			conn = new Connection (false);
			//conn.Open (path, @abstract);
			conn.sock = client;
			conn.sock.Blocking = true;
			conn.ns = new NetworkStream (conn.sock);

			Connection.tmpConn = conn;

			demo = new DemoObject ();
			conn.Marshal (demo, "org.ndesk.test");

			//TODO: handle lost connections etc.
			while (true)
				conn.Iterate ();
		}
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

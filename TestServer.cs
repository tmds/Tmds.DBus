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
		string addr = "unix:abstract=/tmp/dbus-ABCDEFGHIJ";

		string path = "/tmp/dbus-ABCDEFGHIJ";
		bool @abstract = true;

		AbstractUnixEndPoint ep = new AbstractUnixEndPoint (path);
		Socket server = new Socket (AddressFamily.Unix, SocketType.Stream, 0);
		server.Bind (ep);

		server.Listen (1);

		Console.WriteLine ("Waiting for client on " + addr);
		Socket client = server.Accept ();
		Console.WriteLine ("Client accepted");

		//this might well be wrong, untested and doesn't yet work here onwards
		Connection conn = new Connection (false);
		//conn.Open (path, @abstract);
		conn.sock = client;
		conn.sock.Blocking = true;
		conn.ns = new NetworkStream (conn.sock);

		while (true)
			conn.Iterate ();
	}
}

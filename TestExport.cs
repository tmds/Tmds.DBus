// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestExport
{
	public static void Main ()
	{
		Connection conn = new Connection ();

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		DProxy prox = new DProxy (conn, opath, name, typeof (Bus));
		Bus bus = (Bus)prox.GetTransparentProxy ();

		/*
		bus.NameAcquired += delegate (string name) {
			Console.WriteLine ("NameAcquired: " + name);
		};
		*/

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);

		string myNameReq = "org.ndesk.test";

		//NameReply nameReply = bus.RequestName (myNameReq, NameFlag.None);
		NameReply nameReply = (NameReply)bus.RequestName (myNameReq, NameFlag.None);

		Console.WriteLine ("nameReply: " + nameReply);

		DemoObject demo = new DemoObject ();
		conn.RegisteredObjects["org.ndesk.test"] = demo;

		conn.WaitForReplyTo (0);
	}
}

public class DemoObject
{
	//dbus-send --type=method_call --dest=org.ndesk.test /demo org.ndesk.test.Say string:'foo'
	public void Say (string text)
	{
		Console.WriteLine (text);
	}

	//dbus-send --type=method_call --print-reply --dest=org.ndesk.test /demo org.ndesk.test.EchoCaps string:'foo'
	public string EchoCaps (string text)
	{
		return text.ToUpper ();
	}
}

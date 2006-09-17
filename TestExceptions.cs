// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestExceptions
{
	public static void Main ()
	{
		Connection conn = new Connection ();

		//begin ugly bits
		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetObject<Bus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);


		ObjectPath myOpath = new ObjectPath ("/org/ndesk/testexceptions");
		string myNameReq = "org.ndesk.testexceptions";

		DemoObject demo;

		if (bus.NameHasOwner (myNameReq)) {
			demo = conn.GetObject<DemoObject> (myNameReq, myOpath);
		} else {
			NameReply nameReply = bus.RequestName (myNameReq, NameFlag.None);

			Console.WriteLine ("nameReply: " + nameReply);

			demo = new DemoObject ();
			Connection.tmpConn = conn;
			conn.Marshal (demo, myNameReq, myOpath);

			while (true)
				conn.Iterate ();
		}
		//end ugly bits

		Console.WriteLine ();
		//org.freedesktop.DBus.Error.InvalidArgs: Requested bus name "" is not valid
		try {
			bus.RequestName ("", NameFlag.None);
		} catch (Exception e) {
			Console.WriteLine (e);
		}

		//TODO: make this work as expected (what is expected?)
		Console.WriteLine ();
		demo.ThrowSomeException ();
		//handle the thrown exception
		conn.Iterate ();
	}
}

[Interface ("org.ndesk.testexceptions")]
public class DemoObject : MarshalByRefObject
{
	public void ThrowSomeException ()
	{
		Console.WriteLine ("Asked to throw some Exception");

		throw new Exception ("Some Exception");
	}
}

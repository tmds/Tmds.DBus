// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;

using org.freedesktop;
using org.freedesktop.DBus;

public class ManagedDBusTestNotifications
{
	public static void Main ()
	{
		Connection conn = new Connection ();

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetInstance<Bus> (opath, name);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);

		//hack to process the NameAcquired signal synchronously
		conn.HandleSignal (conn.ReadMessage ());

		Notifications notifications = conn.GetInstance<Notifications> (new ObjectPath ("/org/freedesktop/Notifications"), "org.freedesktop.Notifications");

		Console.WriteLine ();

		Console.WriteLine ("Capabilities:");
		foreach (string cap in notifications.GetCapabilities ())
			Console.WriteLine ("\t" + cap);
	}
}

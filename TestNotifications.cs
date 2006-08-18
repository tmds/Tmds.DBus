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

		DProxy prox = new DProxy (conn, opath, name, typeof (Bus));
		Bus bus = (Bus)prox.GetTransparentProxy ();

		bus.NameAcquired += delegate (string name) {
			Console.WriteLine ("NameAcquired: " + name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);

		//hack to process the NameAcquired signal synchronously
		conn.HandleMessage (conn.ReadMessage ());

		DProxy notificationsProxy = new DProxy (conn, new ObjectPath ("/org/freedesktop/Notifications"), "org.freedesktop.Notifications", typeof (Notifications));
		Notifications notifications = (Notifications)notificationsProxy.GetTransparentProxy ();

		Console.WriteLine ();

		Console.WriteLine ("Capabilities:");
		foreach (string cap in notifications.GetCapabilities ())
			Console.WriteLine ("\t" + cap);
	}
}

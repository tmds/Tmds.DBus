// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop;
using org.freedesktop.DBus;

// Just for fun. A more complete implementation would cover the API at:
// http://www.galago-project.org/docs/api/libnotify/notification_8h.html
public class ManagedDBusTestNotifications
{
	public static void Main ()
	{
		Connection conn = new Connection ();

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetObject<Bus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);

		Notifications nf = conn.GetObject<Notifications> ("org.freedesktop.Notifications", new ObjectPath ("/org/freedesktop/Notifications"));

		Console.WriteLine ();
		Console.WriteLine ("Capabilities:");
		foreach (string cap in nf.Capabilities)
			Console.WriteLine ("\t" + cap);

		ServerInformation si = nf.ServerInformation;

		//TODO: ability to pass null
		Dictionary<string,object> hints = new Dictionary<string,object> ();

		string message = String.Format ("Brought to you using {0} {1} (implementing spec version {2}) from {3}.", si.Name, si.Version, si.SpecVersion, si.Vendor);

		uint handle = nf.Notify ("D-Bus# Notifications Demo", 0, "warning", "Managed D-Bus# says 'Hello'!", message, new string[0], hints, 0);

		Console.WriteLine ();
		Console.WriteLine ("Got handle " + handle);
	}
}

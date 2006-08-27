// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTest
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

		DProxy prox2 = new DProxy (conn, opath, "org.freedesktop.DBus.Introspectable", name, typeof (Introspectable));
		Introspectable bus2 = (Introspectable)prox2.GetTransparentProxy ();

		Console.WriteLine ();
		string xmlData = bus2.Introspect ();
		Console.WriteLine ("xmlData: " + xmlData);


		Console.WriteLine ();
		foreach (string n in bus.ListNames ())
			Console.WriteLine (n);

		Console.WriteLine ();
		foreach (string n in bus.ListNames ())
			Console.WriteLine ("Name " + n + " has owner: " + bus.NameHasOwner (n));

		Console.WriteLine ();
		//Console.WriteLine ("NameHasOwner: " + dbus.NameHasOwner (name));
		//Console.WriteLine ("NameHasOwner: " + dbus.NameHasOwner ("fiz"));
	}
}

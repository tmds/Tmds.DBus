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

		Bus bus = conn.GetInstance<Bus> (opath, name);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		bus.Hello ();

		//hack to process the NameAcquired signal synchronously
		conn.HandleSignal (conn.ReadMessage ());

		bus.AddMatch ("type='signal'");
		bus.AddMatch ("type='method_call'");
		bus.AddMatch ("type='method_return'");
		bus.AddMatch ("type='error'");

		while (true) {
			Message msg = conn.ReadMessage ();
			Console.WriteLine ("Message:");
			Console.WriteLine ("\t" + "Type: " + msg.MessageType);
			foreach (HeaderField hf in msg.HeaderFields)
				Console.WriteLine ("\t" + hf.Code + ": " + hf.Value);

			if (msg.Body != null) {
				Console.WriteLine ("\tBody:");
				//System.IO.MemoryStream ms = new System.IO.MemoryStream (msg.Body);
				//System.IO.MemoryStream ms = msg.Body;

				foreach (DType dtype in msg.Signature.Data) {
					if (dtype == DType.Invalid)
						continue;
					object arg;
					Message.GetValue (msg.Body, dtype, out arg);
					Console.WriteLine ("\t\t" + dtype + ": " + arg);
				}
			}

			Console.WriteLine ();
		}
	}
}

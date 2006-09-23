// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTest
{
	public static void Main (string[] args)
	{
		string addr = Address.SessionBus;

		if (args.Length == 1) {
			string arg = args[0];

			switch (arg)
			{
				case "--system":
					addr = Address.SystemBus;
					break;
				case "--session":
					addr = Address.SessionBus;
					break;
				default:
					Console.Error.WriteLine ("Usage: monitor.exe [--system | --session]");
					return;
			}
		}

		Connection conn = new Connection (false);
		conn.Open (addr);
		conn.Authenticate ();

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetObject<Bus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		bus.Hello ();

		//hack to process the NameAcquired signal synchronously
		conn.HandleSignal (conn.ReadMessage ());

		bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Signal));
		bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodCall));
		bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodReturn));
		bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Error));

		while (true) {
			Message msg = conn.ReadMessage ();
			Console.WriteLine ("Message:");
			Console.WriteLine ("\t" + "Type: " + msg.Header.MessageType);
			//foreach (HeaderField hf in msg.HeaderFields)
			//	Console.WriteLine ("\t" + hf.Code + ": " + hf.Value);
			foreach (KeyValuePair<FieldCode,object> field in msg.Header.Fields)
				Console.WriteLine ("\t" + field.Key + ": " + field.Value);

			if (msg.Body != null) {
				Console.WriteLine ("\tBody:");
				//System.IO.MemoryStream ms = new System.IO.MemoryStream (msg.Body);
				//System.IO.MemoryStream ms = msg.Body;

				//TODO: this needs to be done more intelligently
				try {
					foreach (DType dtype in msg.Signature.Data) {
						if (dtype == DType.Invalid)
							continue;
						object arg;
						MessageStream.GetValue (msg.Body, dtype, out arg);
						Console.WriteLine ("\t\t" + dtype + ": " + arg);
					}
				} catch {
						Console.WriteLine ("\t\tmonitor is too dumb to decode message body");
				}
			}

			Console.WriteLine ();
		}
	}
}

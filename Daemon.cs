// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//#define USE_GLIB

using System;
using System.IO;
using System.Text;

using DBus;
using DBus.Unix;

public class DBusDaemon
{
	public static void Main (string[] args)
	{
		string addr = "tcp:host=localhost,port=12345";
		//string addr = "win:path=dbus-session";

		bool shouldFork = false;

		for (int i = 0; i != args.Length; i++) {
			string arg = args[i];
			//Console.Error.WriteLine ("arg: " + arg);

			/*
			if (!arg.StartsWith ("--")) {
				addr = arg;
				continue;
			}
			*/

			if (arg.StartsWith ("--print-")) {
				string[] parts = arg.Split('=');
				int fd = 1;
				if (parts.Length > 1)
					fd = Int32.Parse (parts[1]);
				else if (args.Length > i + 1) {
					string poss = args[i + 1];
					if (Int32.TryParse (poss, out fd))
						i++;
				}

				TextWriter tw;
				if (fd == 1) {
					tw = Console.Out;
				} else if (fd == 2) {
					tw = Console.Error;
				}  else {
					Stream fs = new UnixStream (fd);
					tw = new StreamWriter (fs, Encoding.ASCII);
					tw.NewLine = "\n";
				}

				if (parts[0] == "--print-address")
					tw.WriteLine (addr);
				//else if (parts[0] == "--print-pid") {
				//	int pid = System.Diagnostics.Process.GetCurrentProcess ().Id; ;
				//	tw.WriteLine (pid);
				//}
				else
					continue;
					//throw new Exception ();

				tw.Flush ();
				continue;
			}

			switch (arg) {
				case "--version":
					//Console.WriteLine ("D-Bus Message Bus Daemon " + Introspector.GetProductDescription ());
					Console.WriteLine ("D-Bus Message Bus Daemon " + "0.1");
					return;
				case "--system":
					break;
				case "--session":
					break;
				case "--fork":
					shouldFork = true;
					break;
				case "--introspect":
					{
						Introspector intro = new Introspector ();
						intro.root_path = ObjectPath.Root;
						intro.WriteStart ();
						intro.WriteType (typeof (org.freedesktop.DBus.IBus));
						intro.WriteEnd ();

						Console.WriteLine (intro.xml);
					}
					return;
				default:
					break;
			}
		}

		/*
		if (args.Length >= 1) {
				addr = args[0];
		}
		*/

		int childPid;
		if (shouldFork) {
			childPid = (int)UnixSocket.fork ();
			//if (childPid != 0)
			//	return;
		} else
			childPid = System.Diagnostics.Process.GetCurrentProcess ().Id;

		if (childPid != 0) {
			for (int i = 0; i != args.Length; i++) {
				string arg = args[i];
				//Console.Error.WriteLine ("arg: " + arg);

				/*
				if (!arg.StartsWith ("--")) {
					addr = arg;
					continue;
				}
				*/

				if (arg.StartsWith ("--print-")) {
					string[] parts = arg.Split ('=');
					int fd = 1;
					if (parts.Length > 1)
						fd = Int32.Parse (parts[1]);
					else if (args.Length > i + 1) {
						string poss = args[i + 1];
						if (Int32.TryParse (poss, out fd))
							i++;
					}

					TextWriter tw;
					if (fd == 1) {
						tw = Console.Out;
					} else if (fd == 2) {
						tw = Console.Error;
					} else {
						Stream fs = new UnixStream (fd);
						tw = new StreamWriter (fs, Encoding.ASCII);
						tw.NewLine = "\n";
					}

					//if (parts[0] == "--print-address")
					//	tw.WriteLine (addr);
					if (parts[0] == "--print-pid") {
						int pid = childPid;
						tw.WriteLine (pid);
					}

					tw.Flush ();
					continue;
				}
			}

		}

		if (shouldFork && childPid != 0) {
			return;
			//Environment.Exit (1);
		}


		//if (shouldFork && childPid == 0) {
		if (shouldFork) {

			/*
			Console.In.Dispose ();
			Console.Out.Dispose ();
			Console.Error.Dispose ();
			*/


			int O_RDWR = 2;
			int devnull = UnixSocket.open ("/dev/null", O_RDWR);

			UnixSocket.dup2 (devnull, 0);
			UnixSocket.dup2 (devnull, 1);
			UnixSocket.dup2 (devnull, 2);

			//UnixSocket.close (0);
			//UnixSocket.close (1);
			//UnixSocket.close (2);

			if (UnixSocket.setsid () == (IntPtr) (-1))
				throw new Exception ();
		}

		RunServer (addr);


		//Console.Error.WriteLine ("Usage: dbus-daemon [address]");
	}

	static void RunServer (string addr)
	{
		Server serv = Server.ListenAt (addr);

		ServerBus sbus = new ServerBus ();

		string activationEnv = Environment.GetEnvironmentVariable ("DBUS_ACTIVATION");
		if (activationEnv == "1") {
			sbus.ScanServices ();
			sbus.allowActivation = true;
		}

		sbus.server = serv;
		serv.SBus = sbus;
		serv.NewConnection += sbus.AddConnection;

#if USE_GLIB
		new Thread (new ThreadStart (serv.Listen)).Start ();
		//GLib.Idle.Add (delegate { serv.Listen (); return false; });
		GLib.MainLoop main = new GLib.MainLoop ();
		main.Run ();
#else
		serv.Listen ();
#endif
	}
}
// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//#define USE_GLIB

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using NDesk.Unix;
using NDesk.DBus;
using NDesk.DBus.Authentication;
using NDesk.DBus.Transports;
using org.freedesktop.DBus;

using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Reflection;

using System.Threading;

#if ENABLE_PIPES
using System.IO.Pipes;
#endif

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
					Stream fs = new NDesk.Unix.UnixStream (fd);
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
					//Console.WriteLine ("NDesk D-Bus Message Bus Daemon " + Introspector.GetProductDescription ());
					Console.WriteLine ("NDesk D-Bus Message Bus Daemon " + "0.1");
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
						Stream fs = new NDesk.Unix.UnixStream (fd);
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

	public class ServerBus : org.freedesktop.DBus.IBus
	{
		static string ValidateBusName (string name)
		{
			if (name == String.Empty)
				return "cannot be empty";
			//if (name.StartsWith (":"))
			//	return "cannot be a unique name";
			return null;
		}

		static bool BusNameIsValid (string name, out string nameError)
		{
			nameError = ValidateBusName (name);
			return nameError == null;
		}

		readonly List<Connection> conns = new List<Connection> ();

		public static readonly ObjectPath Path = new ObjectPath ("/org/freedesktop/DBus");
		const string DBusBusName = "org.freedesktop.DBus";

		internal Server server;
		//Connection Caller
		ServerConnection Caller
		{
			get {
				//return server.CurrentMessageConnection;
				return server.CurrentMessageConnection as ServerConnection;
			}
		}

		// TODO: Should be the : name, or "(inactive)" / caller.UniqueName
		//string callerUniqueName = ":?";

		public void AddConnection (Connection conn)
		{
			//Console.Error.WriteLine ("AddConn");

			if (conns.Contains (conn))
				throw new Exception ("Cannot add connection");

			conns.Add (conn);
			conn.Register (Path, this);
		}

		public void RemoveConnection (Connection conn)
		{
			// FIXME: RemoveConnection is not always called when sessions end!

			Console.Error.WriteLine ("RemoveConn");

			if (!conns.Remove (conn))
				throw new Exception ("Cannot remove connection");

			//conn.Unregister (Path);

			List<string> namesToDisown = new List<string> ();
			foreach (KeyValuePair<string,Connection> pair in Names) {
				if (pair.Value == conn)
					namesToDisown.Add (pair.Key);
			}

			List<MatchRule> toRemove = new List<MatchRule> ();
			foreach (KeyValuePair<MatchRule,List<Connection>> pair in Rules) {
				//while (pair.Value.Remove (Caller)) { }
				while (pair.Value.Remove (conn)) { }
				//while (pair.Value.Remove (Caller)) { Console.WriteLine ("Remove!"); }
				//pair.Value.RemoveAll ( delegate (Connection conn) { conn == Caller; } )
				if (pair.Value.Count == 0)
					toRemove.Add (pair.Key);
					//Rules.Remove (pair);
					//Rules.Remove<KeyValuePair<MatchRule,List<Connection>>> (pair);
					//((ICollection<System.Collections.Generic.KeyValuePair<MatchRule,List<Connection>>>)Rules).Remove<KeyValuePair<MatchRule,List<Connection>>> (pair);
					//((ICollection<System.Collections.Generic.KeyValuePair<MatchRule,List<Connection>>>)Rules).Remove (pair);
			}

			foreach (MatchRule r in toRemove)
				Rules.Remove (r);

			// TODO: Check the order of signals
			// TODO: Atomicity

			foreach (string name in namesToDisown)
				Names.Remove (name);

			foreach (string name in namesToDisown)
				//NameOwnerChanged (name, Caller.UniqueName, String.Empty);
				NameOwnerChanged (name, ((ServerConnection)conn).UniqueName, String.Empty);

			//NameOwnerChanged (Caller.UniqueName, Caller.UniqueName, String.Empty);

			// FIXME: Unregister earlier?
			conn.Unregister (Path);
		}

		struct NameRequisition
		{
			public NameRequisition (Connection connection, bool allowReplacement)
			{
				this.Connection = connection;
				this.AllowReplacement = allowReplacement;
			}

			public readonly Connection Connection;
			public readonly bool AllowReplacement;
		}

		//SortedList<>
		readonly Dictionary<string,Connection> Names = new Dictionary<string,Connection> ();
		//readonly SortedList<string,Connection> Names = new SortedList<string,Connection> ();
		//readonly SortedDictionary<string,Connection> Names = new SortedDictionary<string,Connection> ();
		public RequestNameReply RequestName (string name, NameFlag flags)
		{
			Console.Error.WriteLine ("RequestName " + name);
			string nameError;
			if (!BusNameIsValid (name, out nameError))
				throw new ArgumentException (String.Format ("Requested name \"{0}\" is not valid: {1}", name, nameError), "name");

			if (name.StartsWith (":"))
				throw new ArgumentException (String.Format ("Cannot acquire a name starting with ':' such as \"{0}\"", name), "name");

			if (name == DBusBusName)
				throw new ArgumentException (String.Format ("Connection \"{0}\" is not allowed to own the name \"{1}\" because it is reserved for D-Bus' use only", Caller.UniqueName, name), "name");

			// TODO: Policy delegate support

			// TODO: NameFlag support

			if (flags != NameFlag.None)
				Console.Error.WriteLine ("Warning: Ignoring unimplemented NameFlags: " + flags);

			Connection c;
			if (!Names.TryGetValue (name, out c)) {
				Names[name] = Caller;
				RaiseNameSignal ("Acquired", name);
				NameOwnerChanged (name, String.Empty, Caller.UniqueName);

				Message activationMessage;
				if (activationMessages.TryGetValue (name, out activationMessage)) {
					activationMessages.Remove (name);
					Caller.SendReal (activationMessage);
				}

				return RequestNameReply.PrimaryOwner;
			} else if (c == Caller)
				return RequestNameReply.AlreadyOwner;
			else
				return RequestNameReply.Exists;
		}

		public ReleaseNameReply ReleaseName (string name)
		{
			// TODO: Check for : name here?

			Connection c;
			if (!Names.TryGetValue (name, out c))
				return ReleaseNameReply.NonExistent;

			if (c != Caller)
				return ReleaseNameReply.NotOwner;

			Names.Remove (name);
			// TODO: Does official daemon send NameLost signal here? Do the same.
			RaiseNameSignal ("Lost", name);
			NameOwnerChanged (name, Caller.UniqueName, String.Empty);
			return ReleaseNameReply.Released;
		}

		/*
		public string MyMeth (uint val1, string val2)
		{
			Console.WriteLine ("MyMeth " + val1 + " " + val2);
			return "WEE!";
		}
		*/

		/*
		public struct MethData
		{
			public int A;
			public int B;
			public int C;
		}
		*/

		public class MethDataBase
		{
			public int A;
		}

		public class MethData : MethDataBase
		{
			public int B;
			public int C;
			//public MethData[] Children;
			public long[] Children;
		}

		public void MyMeth0 ()
		{
		}

		public string MyMeth (MethData d, int[] ary, IDictionary<int,string> dict)
		{
			Console.WriteLine ("MyMeth struct " + d.A + " " + d.B + " " + d.C);
			foreach (int i in ary)
				Console.WriteLine (i);
			Console.WriteLine ("Children: " + d.Children.Length);
			Console.WriteLine ("Dict entries: " + dict.Count);
			Console.WriteLine ("321: " + dict[321]);
			return "WEE!";
		}

		readonly long uniqueBase = 1;
		long uniqueNames = 0;
		public string Hello ()
		{
			if (Caller.UniqueName != null)
				throw new BusException ("org.freedesktop.DBus.Error.Failed", "Already handled an Hello message");

			long uniqueNumber = Interlocked.Increment (ref uniqueNames);

			string uniqueName = String.Format (":{0}.{1}", uniqueBase, uniqueNumber);
			Console.Error.WriteLine ("Hello " + uniqueName + "!");
			Caller.UniqueName = uniqueName;
			Names[uniqueName] = Caller;

			// These signals ought to be queued up and send after the reply is sent?
			// Should have the Destination field set!
			//NameAcquired (uniqueName);
			RaiseNameSignal ("Acquired", uniqueName);

			NameOwnerChanged (uniqueName, String.Empty, uniqueName);

			return uniqueName;
		}

		void RaiseNameSignal (string memberSuffix, string name)
		{
			// Name* signals on org.freedesktop.DBus are connection-specific.
			// We handle them here as a special case.

			Signal nameSignal = new Signal (Path, "org.freedesktop.DBus", "Name" + memberSuffix);
			MessageWriter mw = new MessageWriter ();
			mw.Write (name);
			nameSignal.message.Body = mw.ToArray ();
			nameSignal.message.Signature = Signature.StringSig;
			Caller.Send (nameSignal.message);
		}

		public string[] ListNames ()
		{
			//return Names.Keys.ToArray ();
			//List<string> names = new List<string> (Names.Keys);
			List<string> names = new List<string> ();
			names.Add (DBusBusName);
			names.AddRange (Names.Keys);
			return names.ToArray ();
		}

		public string[] ListActivatableNames ()
		{
			List<string> names = new List<string> ();
			names.AddRange (services.Keys);
			return names.ToArray ();
		}

		public bool NameHasOwner (string name)
		{
			if (name == DBusBusName)
				return true;

			return Names.ContainsKey (name);
		}

		public event NameOwnerChangedHandler NameOwnerChanged;
		public event NameLostHandler NameLost;
		public event NameAcquiredHandler NameAcquired;

		public StartReply StartServiceByName (string name, uint flags)
		{
			if (name == DBusBusName)
				return StartReply.AlreadyRunning;

			if (Names.ContainsKey (name))
				return StartReply.AlreadyRunning;

			StartProcessNamed (name);
			return StartReply.Success;

			//return StartReply.Success;
			//throw new NotSupportedException ();
		}

		Dictionary<string, string> activationEnv = new Dictionary<string, string> ();
		public void UpdateActivationEnvironment (IDictionary<string, string> environment)
		{
			Console.Error.WriteLine ("UpdateActivationEnvironment");
			foreach (KeyValuePair<string, string> pair in environment) {
				if (pair.Value == String.Empty)
					activationEnv.Remove (pair.Key);
				else
					activationEnv[pair.Key] = pair.Value;
			}
		}

		public string GetNameOwner (string name)
		{
			if (name == DBusBusName)
				return DBusBusName;

			Connection c;
			if (!Names.TryGetValue (name, out c))
				throw new BusException ("org.freedesktop.DBus.Error.NameHasNoOwner", "Could not get owner of name '{0}': no such name", name);

			return ((ServerConnection)c).UniqueName;
		}

		//public uint GetConnectionUnixUser (string connection_name)
		public uint GetConnectionUnixUser (string name)
		{
			//if (name == DBusBusName)
			//	return 0;

			Connection c;
			if (!Names.TryGetValue (name, out c))
				throw new BusException ("org.freedesktop.DBus.Error.NameHasNoOwner", "Could not get UID of name '{0}': no such name", name);

			return (uint)((ServerConnection)c).UserId;
			//throw new BusException ("org.freedesktop.DBus.Error.Failed", "Could not determine UID for '{0}'", name);

		}

		Dictionary<string, Message> activationMessages = new Dictionary<string, Message> ();

		internal void HandleMessage (Message msg)
		{
			if (msg == null)
				return;

			//if (msg.Signature == new Signature ("u"))
			if (false) {
				System.Reflection.MethodInfo target = typeof (ServerBus).GetMethod ("MyMeth");
				//System.Reflection.MethodInfo target = typeof (ServerBus).GetMethod ("MyMeth0");
				Signature inSig, outSig;
				TypeImplementer.SigsForMethod (target, out inSig, out outSig);
				Console.WriteLine ("inSig: " + inSig);

				if (msg.Signature == inSig) {
					MethodCaller caller = TypeImplementer.GenCaller (target, this);
					//caller (new MessageReader (msg), msg);

					MessageWriter retWriter = new MessageWriter ();
					caller (new MessageReader (msg), msg, retWriter);

					if (msg.ReplyExpected) {
						MethodReturn method_return = new MethodReturn (msg.Header.Serial);
						Message replyMsg = method_return.message;
						replyMsg.Body = retWriter.ToArray ();
						Console.WriteLine ("replyMsg body: " + replyMsg.Body.Length);
						/*
						try {
						replyMsg.Header[FieldCode.Destination] = msg.Header[FieldCode.Sender];
						replyMsg.Header[FieldCode.Sender] = Caller.UniqueName;
						} catch (Exception e) {
							Console.Error.WriteLine (e);
						}
						*/
						replyMsg.Header[FieldCode.Destination] = Caller.UniqueName;
						replyMsg.Header[FieldCode.Sender] = "org.freedesktop.DBus";
						replyMsg.Signature = outSig;
						{
							Caller.Send (replyMsg);

							/*
							replyMsg.Header.Serial = Caller.GenerateSerial ();
							MessageDumper.WriteMessage (replyMsg, Console.Out);
							Caller.SendReal (replyMsg);
							*/
							return;
						}
					}
				}
			}

			//List<Connection> recipients = new List<Connection> ();
			HashSet<Connection> recipients = new HashSet<Connection> ();
			//HashSet<Connection> recipientsAll = new HashSet<Connection> (Connections);

			object fieldValue = msg.Header[FieldCode.Destination];
			if (fieldValue != null) {
				string destination = (string)fieldValue;
				Connection destConn;
				if (Names.TryGetValue (destination, out destConn))
					recipients.Add (destConn);
				else if (destination != "org.freedesktop.DBus" && !destination.StartsWith(":") && (msg.Header.Flags & HeaderFlag.NoAutoStart) != HeaderFlag.NoAutoStart) {
					// Attempt activation
					StartProcessNamed (destination);
					//Thread.Sleep (5000);
					// TODO: Route the message to the newly activated service!
					activationMessages[destination] = msg;
					//if (Names.TryGetValue (destination, out destConn))
					//	recipients.Add (destConn);
					//else
					//	Console.Error.WriteLine ("Couldn't route message to activated service");
				} else if (destination != "org.freedesktop.DBus") {
					// Send an error when there's no hope of getting the requested reply
					if (msg.ReplyExpected) {
						// Error org.freedesktop.DBus.Error.ServiceUnknown: The name {0} was not provided by any .service files
						Message rmsg = MessageHelper.CreateUnknownMethodError (new MethodCall (msg));
						if (rmsg != null) {
							//Caller.Send (rmsg);
							Caller.SendReal (rmsg);
							return;
						}
					}

				}
			}

			HashSet<Connection> recipientsMatchingHeader = new HashSet<Connection> ();

			HashSet<ArgMatchTest> a = new HashSet<ArgMatchTest> ();
			foreach (KeyValuePair<MatchRule,List<Connection>> pair in Rules) {
				if (recipients.IsSupersetOf (pair.Value))
					continue;
				if (pair.Key.MatchesHeader (msg)) {
					a.UnionWith (pair.Key.Args);
					recipientsMatchingHeader.UnionWith (pair.Value);
				}
			}

			MatchRule.Test (a, msg);

			foreach (KeyValuePair<MatchRule,List<Connection>> pair in Rules) {
				if (recipients.IsSupersetOf (pair.Value))
					continue;
				if (!recipientsMatchingHeader.IsSupersetOf (pair.Value))
					continue;
				if (a.IsSupersetOf (pair.Key.Args))
					recipients.UnionWith (pair.Value);
			}

			foreach (Connection conn in recipients) {
				// TODO: rewrite/don't header fields
				//conn.Send (msg);
				// TODO: Zero the Serial or not?
				//msg.Header.Serial = 0;
				((ServerConnection)conn).SendReal (msg);
			}
		}

		//SortedDictionary<MatchRule,int> Rules = new SortedDictionary<MatchRule,int> ();
		//Dictionary<MatchRule,int> Rules = new Dictionary<MatchRule,int> ();
		Dictionary<MatchRule,List<Connection>> Rules = new Dictionary<MatchRule,List<Connection>> ();
		public void AddMatch (string rule)
		{
			MatchRule r = MatchRule.Parse (rule);

			if (r == null)
				throw new Exception ("r == null");

			if (!Rules.ContainsKey (r))
				Rules[r] = new List<Connection> ();

			// Each occurrence of a Connection in the list represents one value-unique AddMatch call
			Rules[r].Add (Caller);

			Console.WriteLine ("Added. Rules count: " + Rules.Count);
		}

		public void RemoveMatch (string rule)
		{
			MatchRule r = MatchRule.Parse (rule);

			if (r == null)
				throw new Exception ("r == null");

			if (!Rules.ContainsKey (r))
				throw new Exception ();

			// We remove precisely one occurrence of the calling connection
			Rules[r].Remove (Caller);
			if (Rules[r].Count == 0)
				Rules.Remove (r);

			Console.WriteLine ("Removed. Rules count: " + Rules.Count);
		}

		public string GetId ()
		{
			return Caller.Id.ToString ();
		}

		// Undocumented in spec
		public string[] ListQueuedOwners (string name)
		{
			// ?
			if (name == DBusBusName)
				return new string[] { DBusBusName };

			Connection c;
			if (!Names.TryGetValue (name, out c))
				throw new BusException ("org.freedesktop.DBus.Error.NameHasNoOwner", "Could not get owners of name '{0}': no such name", name);

			return new string[] { ((ServerConnection)c).UniqueName };
			//throw new NotImplementedException ();
		}

		// Undocumented in spec
		public uint GetConnectionUnixProcessID (string connection_name)
		{
			Connection c;
			if (!Names.TryGetValue (connection_name, out c))
				throw new BusException ("org.freedesktop.DBus.Error.NameHasNoOwner", "Could not get PID of name '{0}': no such name", connection_name);

			uint pid;
			if (!c.Transport.TryGetPeerPid (out pid))
				throw new BusException ("org.freedesktop.DBus.Error.Failed", "Could not determine UID for '{0}'", connection_name);

			return pid;
		}

		// Undocumented in spec
		public byte[] GetConnectionSELinuxSecurityContext (string connection_name)
		{
			throw new BusException ("org.freedesktop.DBus.Error.SELinuxSecurityContextUnknown", "Could not determine security context for '{0}'", connection_name);
		}

		// Undocumented in spec
		public void ReloadConfig ()
		{
			ScanServices ();
		}

		Dictionary<string, string> services = new Dictionary<string, string> ();

		public void ScanServices ()
		{
			services.Clear ();

			string svcPath = "/usr/share/dbus-1/services";
			string[] svcs = Directory.GetFiles (svcPath, "*.service");
			foreach (string svc in svcs) {
				string fname = System.IO.Path.Combine (svcPath, svc);
				using (TextReader r = new StreamReader (fname)) {
					string ln;
					string cmd = null;
					string name = null;
					while ((ln = r.ReadLine ()) != null) {
						if (ln.StartsWith ("Exec="))
							cmd = ln.Remove (0, 5);
						else if (ln.StartsWith ("Name="))
							name = ln.Remove (0, 5);
					}

					if (name != null && cmd != null)
						services[name] = cmd;
				}
			}
		}

		public bool allowActivation = false;
		void StartProcessNamed (string name)
		{
			Console.WriteLine ("Start " + name);

			if (!allowActivation)
				return;

			string cmd;
			if (!services.TryGetValue (name, out cmd))
				return;

			try {
				StartProcess (cmd);
			} catch (Exception e) {
				Console.Error.WriteLine (e);
			}
		}

		void StartProcess (string fname)
		{
			if (!allowActivation)
				return;

			try {
				ProcessStartInfo startInfo = new ProcessStartInfo (fname);
				startInfo.UseShellExecute = false;

				foreach (KeyValuePair<string, string> pair in activationEnv) {
					startInfo.EnvironmentVariables[pair.Key] = pair.Value;
				}

				startInfo.EnvironmentVariables["DBUS_STARTER_BUS_TYPE"] = "session";
				startInfo.EnvironmentVariables["DBUS_SESSION_BUS_ADDRESS"] = server.address;
				startInfo.EnvironmentVariables["DBUS_STARTER_ADDRESS"] = server.address;
				startInfo.EnvironmentVariables["DBUS_STARTER_BUS_TYPE"] = "session";
				Process myProcess = Process.Start (startInfo);
			} catch (Exception e) {
				Console.Error.WriteLine (e);
			}
		}
	}

class ServerConnection : Connection
{
	public Server Server;

	public ServerConnection (Transport t) : base (t)
	{
	}

	bool shouldDump = false;
	//bool shouldDump = true;

	//bool isHelloed = false;
	//bool isConnected = true;
	override internal void HandleMessage (Message msg)
	{
		//Console.Error.WriteLine ("Message!");

		if (!isConnected)
			return;

		Server.CurrentMessageConnection = this;
		Server.CurrentMessage = msg;

		if (msg == null) {
			Console.Error.WriteLine ("Disconnected!");
			isConnected = false;
			//Server.Bus.RemoveConnection (this);
			//ServerBus sbus = Unregister (new ObjectPath ("/org/freedesktop/DBus")) as ServerBus;

			/*
			ServerBus sbus = Unregister (new ObjectPath ("/org/freedesktop/DBus")) as ServerBus;
			Register (new ObjectPath ("/org/freedesktop/DBus"), sbus);
			sbus.RemoveConnection (this);
			*/

			Server.SBus.RemoveConnection (this);

			//Server.ConnectionLost (this);
			return;
		}

		if (shouldDump) {
			MessageDumper.WriteComment ("Handling:", Console.Out);
			MessageDumper.WriteMessage (msg, Console.Out);
		}

		if (UniqueName != null)
			msg.Header[FieldCode.Sender] = UniqueName;

		object fieldValue = msg.Header[FieldCode.Destination];
		if (fieldValue != null) {
			if ((string)fieldValue == "org.freedesktop.DBus") {

				// Workaround for our daemon only listening on a single path
				if (msg.Header.MessageType == NDesk.DBus.MessageType.MethodCall)
					msg.Header[FieldCode.Path] = ServerBus.Path;

				base.HandleMessage (msg);
				//return;
			}
		}
		//base.HandleMessage (msg);

		Server.SBus.HandleMessage (msg);

		// TODO: we ought to make sure these are cleared in other cases above too!
		Server.CurrentMessageConnection = null;
		Server.CurrentMessage = null;
	}

	override internal uint Send (Message msg)
	{
		if (!isConnected)
			return 0;

		/*
		if (msg.Header.MessageType == NDesk.DBus.MessageType.Signal) {
			Signal signal = new Signal (msg);
			if (signal.Member == "NameAcquired" || signal.Member == "NameLost") {
				string dest = (string)msg.Header[FieldCode.Destination];
				if (dest != UniqueName)
					return 0;
			}
		}
		*/

		if (msg.Header.MessageType != NDesk.DBus.MessageType.MethodReturn) {
			msg.Header[FieldCode.Sender] = "org.freedesktop.DBus";
		}

		if (UniqueName != null)
			msg.Header[FieldCode.Destination] = UniqueName;

		if (shouldDump) {
			MessageDumper.WriteComment ("Sending:", Console.Out);
			MessageDumper.WriteMessage (msg, Console.Out);
		}

		//return base.Send (msg);
		return SendReal (msg);
	}

	internal uint SendReal (Message msg)
	{
		if (!isConnected)
			return 0;

		try {
			return base.Send (msg);
		} catch {
		//} catch (System.IO.IOException) {
			isConnected = false;
			Server.SBus.RemoveConnection (this);
		}
		return 0;
	}

	//ServerBus SBus;
	public string UniqueName = null;
	public long UserId = 0;

	~ServerConnection ()
	{
		Console.Error.WriteLine ("Good! ~ServerConnection () for {0}", UniqueName);
	}
}

internal class BusContext
{
	protected Connection connection = null;
	public Connection Connection
	{
		get {
			return connection;
		}
	}

	protected Message message = null;
	internal Message CurrentMessage
	{
		get {
			return message;
		}
	}

	public string SenderName = null;
}

class UnixServer : Server
{
	string unixPath = null;
	bool isAbstract;

	public UnixServer (string address)
	{
		AddressEntry[] entries = NDesk.DBus.Address.Parse (address);
		AddressEntry entry = entries[0];

		if (entry.Method != "unix")
			throw new Exception ();

		string val;
		if (entry.Properties.TryGetValue ("path", out val)) {
			unixPath = val;
			isAbstract = false;
		} else if (entry.Properties.TryGetValue ("abstract", out val)) {
			unixPath = val;
			isAbstract = true;
		}

		if (String.IsNullOrEmpty (unixPath))
			throw new Exception ("Address path is invalid");

		if (entry.GUID == UUID.Zero)
			entry.GUID = UUID.Generate ();
		Id = entry.GUID;

		/*
		Id = entry.GUID;
		if (Id == UUID.Zero)
			Id = UUID.Generate ();
		*/

		this.address = entry.ToString ();
		//Console.WriteLine ("Server address: " + Address);
	}

	public override void Disconnect ()
	{
	}

	bool AcceptClient (UnixSocket csock, out ServerConnection conn)
	{
		//TODO: use the right abstraction here, probably using the Server class
		UnixNativeTransport transport = new UnixNativeTransport ();
		//client.Client.Blocking = true;
		transport.socket = csock;
		transport.SocketHandle = (long)csock.Handle;
		transport.Stream = new UnixStream (csock);
		//Connection conn = new Connection (transport);
		//Connection conn = new ServerConnection (transport);
		//ServerConnection conn = new ServerConnection (transport);
		conn = new ServerConnection (transport);
		conn.Server = this;
		conn.Id = Id;

		if (conn.Transport.Stream.ReadByte () != 0)
			return false;

		conn.isConnected = true;

		SaslPeer remote = new SaslPeer ();
		remote.stream = transport.Stream;
		SaslServer local = new SaslServer ();
		local.stream = transport.Stream;
		local.Guid = Id;

		local.Peer = remote;
		remote.Peer = local;

		bool success = local.Authenticate ();

		Console.WriteLine ("Success? " + success);

		if (!success)
			return false;

		conn.UserId = ((SaslServer)local).uid;

		conn.isAuthenticated = true;

		return true;
	}

	public override void Listen ()
	{
		byte[] sa = isAbstract ? UnixNativeTransport.GetSockAddrAbstract (unixPath) : UnixNativeTransport.GetSockAddr (unixPath);
		UnixSocket usock = new UnixSocket ();
		usock.Bind (sa);
		usock.Listen (50);

		while (true) {
			Console.WriteLine ("Waiting for client on " + (isAbstract ? "abstract " : String.Empty) + "path " + unixPath);
			UnixSocket csock = usock.Accept ();
			Console.WriteLine ("Client connected");

			ServerConnection conn;
			if (!AcceptClient (csock, out conn)) {
				Console.WriteLine ("Client rejected");
				csock.Close ();
				continue;
			}

			//GLib.Idle.Add (delegate {

			if (NewConnection != null)
				NewConnection (conn);

			//BusG.Init (conn);
			/*
			conn.Iterate ();
			Console.WriteLine ("done iter");
			BusG.Init (conn);
			Console.WriteLine ("done init");
			*/

			//GLib.Idle.Add (delegate { BusG.Init (conn); return false; });
#if USE_GLIB
			BusG.Init (conn);
#else
			new Thread (new ThreadStart (delegate { while (conn.IsConnected) conn.Iterate (); })).Start ();
#endif
			Console.WriteLine ("done init");


			//return false;
			//});
		}
	}

	/*
	public void ConnectionLost (Connection conn)
	{
	}
	*/

	public override event Action<Connection> NewConnection;
}

class TcpServer : Server
{
	uint port = 0;
	public TcpServer (string address)
	{
		AddressEntry[] entries = NDesk.DBus.Address.Parse (address);
		AddressEntry entry = entries[0];

		if (entry.Method != "tcp")
			throw new Exception ();

		string val;
		if (entry.Properties.TryGetValue ("port", out val))
			port = UInt32.Parse (val);

		if (entry.GUID == UUID.Zero)
			entry.GUID = UUID.Generate ();
		Id = entry.GUID;

		/*
		Id = entry.GUID;
		if (Id == UUID.Zero)
			Id = UUID.Generate ();
		*/

		this.address = entry.ToString ();
		//Console.WriteLine ("Server address: " + Address);
	}

	public override void Disconnect ()
	{
	}

	bool AcceptClient (TcpClient client, out ServerConnection conn)
	{
		//TODO: use the right abstraction here, probably using the Server class
		SocketTransport transport = new SocketTransport ();
		client.Client.Blocking = true;
		transport.SocketHandle = (long)client.Client.Handle;
		transport.Stream = client.GetStream ();
		//Connection conn = new Connection (transport);
		//Connection conn = new ServerConnection (transport);
		//ServerConnection conn = new ServerConnection (transport);
		conn = new ServerConnection (transport);
		conn.Server = this;
		conn.Id = Id;

		if (conn.Transport.Stream.ReadByte () != 0)
			return false;

		conn.isConnected = true;

		SaslPeer remote = new SaslPeer ();
		remote.stream = transport.Stream;
		SaslServer local = new SaslServer ();
		local.stream = transport.Stream;
		local.Guid = Id;

		local.Peer = remote;
		remote.Peer = local;

		bool success = local.Authenticate ();

		Console.WriteLine ("Success? " + success);

		if (!success)
			return false;

		conn.UserId = ((SaslServer)local).uid;

		conn.isAuthenticated = true;

		return true;
	}

	public override void Listen ()
	{
		TcpListener server = new TcpListener (IPAddress.Any, (int)port);
		server.Server.Blocking = true;

		server.Start ();

		while (true) {
			//Console.WriteLine ("Waiting for client on TCP port " + port);
			TcpClient client = server.AcceptTcpClient ();
			/*
			client.NoDelay = true;
			client.ReceiveBufferSize = (int)Protocol.MaxMessageLength;
			client.SendBufferSize = (int)Protocol.MaxMessageLength;
			*/
			//Console.WriteLine ("Client connected");

			ServerConnection conn;
			if (!AcceptClient (client, out conn)) {
				Console.WriteLine ("Client rejected");
				client.Close ();
				continue;
			}

			//client.Client.Blocking = false;


			//GLib.Idle.Add (delegate {

			if (NewConnection != null)
				NewConnection (conn);

			//BusG.Init (conn);
			/*
			conn.Iterate ();
			Console.WriteLine ("done iter");
			BusG.Init (conn);
			Console.WriteLine ("done init");
			*/

			//GLib.Idle.Add (delegate { BusG.Init (conn); return false; });
#if USE_GLIB
			BusG.Init (conn);
#else
			new Thread (new ThreadStart (delegate { while (conn.IsConnected) conn.Iterate (); })).Start ();
#endif
			//Console.WriteLine ("done init");

			//return false;
			//});
		}
	}

	/*
	public void ConnectionLost (Connection conn)
	{
	}
	*/

	public override event Action<Connection> NewConnection;
}

#if ENABLE_PIPES
class WinServer : Server
{
	string pipePath;

	public WinServer (string address)
	{
		AddressEntry[] entries = NDesk.DBus.Address.Parse (address);
		AddressEntry entry = entries[0];

		if (entry.Method != "win")
			throw new Exception ();

		string val;
		if (entry.Properties.TryGetValue ("path", out val)) {
			pipePath = val;
		}

		if (String.IsNullOrEmpty (pipePath))
			throw new Exception ("Address path is invalid");

		if (entry.GUID == UUID.Zero)
			entry.GUID = UUID.Generate ();
		Id = entry.GUID;

		/*
		Id = entry.GUID;
		if (Id == UUID.Zero)
			Id = UUID.Generate ();
		*/

		this.address = entry.ToString ();
		Console.WriteLine ("Server address: " + Address);
	}

	public override void Disconnect ()
	{
	}

	bool AcceptClient (PipeStream client, out ServerConnection conn)
	{
		PipeTransport transport = new PipeTransport ();
		//client.Client.Blocking = true;
		//transport.SocketHandle = (long)client.Client.Handle;
		transport.Stream = client;
		conn = new ServerConnection (transport);
		conn.Server = this;
		conn.Id = Id;

		if (conn.Transport.Stream.ReadByte () != 0)
			return false;

		conn.isConnected = true;

		SaslPeer remote = new SaslPeer ();
		remote.stream = transport.Stream;
		SaslServer local = new SaslServer ();
		local.stream = transport.Stream;
		local.Guid = Id;

		local.Peer = remote;
		remote.Peer = local;

		bool success = local.Authenticate ();
		//bool success = true;

		Console.WriteLine ("Success? " + success);

		if (!success)
			return false;

		conn.UserId = ((SaslServer)local).uid;

		conn.isAuthenticated = true;

		return true;
	}

	static int numPipeThreads = 16;

	public override void Listen ()
	{
		// TODO: Use a ThreadPool to have an adaptive number of reusable threads.
		for (int i = 0; i != numPipeThreads; i++) {
			Thread newThread = new Thread (new ThreadStart (DoListen));
			newThread.Name = "DBusPipeServer" + i;
			// Hack to allow shutdown without Joining threads for now.
			newThread.IsBackground = true;
			newThread.Start ();
		}

		Console.WriteLine ("Press enter to exit.");
		Console.ReadLine ();
	}

	void DoListen ()
	{
		while (true)
		using (NamedPipeServerStream pipeServer = new NamedPipeServerStream (pipePath, PipeDirection.InOut, numPipeThreads, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, (int)Protocol.MaxMessageLength, (int)Protocol.MaxMessageLength)) {
			Console.WriteLine ("Waiting for client on path " + pipePath);
			pipeServer.WaitForConnection ();

			Console.WriteLine ("Client connected");

			ServerConnection conn;
			if (!AcceptClient (pipeServer, out conn)) {
				Console.WriteLine ("Client rejected");
				pipeServer.Disconnect ();
				continue;
			}

			pipeServer.Flush ();
			pipeServer.WaitForPipeDrain ();

			if (NewConnection != null)
				NewConnection (conn);

			while (conn.IsConnected)
				conn.Iterate ();

			pipeServer.Disconnect ();
		}
	}

	/*
	public void ConnectionLost (Connection conn)
	{
	}
	*/

	public override event Action<Connection> NewConnection;
}
#endif
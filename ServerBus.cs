// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

//#define USE_GLIB

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using DBus.Transports;
using org.freedesktop.DBus;

namespace DBus
{
	public class ServerBus : org.freedesktop.DBus.IBus
	{
		public static readonly ObjectPath Path = new ObjectPath ("/org/freedesktop/DBus");
		public const string DBusBusName = "org.freedesktop.DBus";
		public const string DBusInterface = "org.freedesktop.DBus";

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

		internal Server server;

		ServerConnection Caller
		{
			get
			{
				return server.CurrentMessageConnection as ServerConnection;
			}
		}

		public void AddConnection (Connection conn)
		{
			if (conns.Contains (conn))
				throw new Exception ("Cannot add connection");

			conns.Add (conn);
			conn.Register (Path, this);
			//((ExportObject)conn.RegisteredObjects[Path]).Registered = false;
		}

		public void RemoveConnection (Connection conn)
		{
			Console.Error.WriteLine ("RemoveConn");

			if (!conns.Remove (conn))
				throw new Exception ("Cannot remove connection");

			//conn.Unregister (Path);

			List<string> namesToDisown = new List<string> ();
			foreach (KeyValuePair<string, Connection> pair in Names) {
				if (pair.Value == conn)
					namesToDisown.Add (pair.Key);
			}

			List<MatchRule> toRemove = new List<MatchRule> ();
			foreach (KeyValuePair<MatchRule, List<Connection>> pair in Rules) {
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
				NameOwnerChanged (name, ((ServerConnection)conn).UniqueName, String.Empty);

			// TODO: Unregister earlier?
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
		readonly Dictionary<string, Connection> Names = new Dictionary<string, Connection> ();
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
				throw new ArgumentException (String.Format ("Connection \"{0}\" is not allowed to own the name \"{1}\" because it is reserved for D-Bus' use only", Caller.UniqueName ?? "(inactive)", name), "name");

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
			string nameError;
			if (!BusNameIsValid (name, out nameError))
				throw new ArgumentException (String.Format ("Given bus name \"{0}\" is not valid: {1}", name, nameError), "name");

			if (name.StartsWith (":"))
				throw new ArgumentException (String.Format ("Cannot release a name starting with ':' such as \"{0}\"", name), "name");

			if (name == DBusBusName)
				throw new ArgumentException (String.Format ("Cannot release the \"{0}\" name because it is owned by the bus", name), "name");

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

		int uniqueMajor = 1;
		int uniqueMinor = -1;

		string CreateUniqueName ()
		{
			int newMajor = uniqueMajor;
			int newMinor = Interlocked.Increment (ref uniqueMinor);

			// Major wrapping should probably be made atomic too.
			if (newMinor == Int32.MaxValue) {
				if (uniqueMajor == Int32.MaxValue)
					uniqueMajor = 1;
				else
					uniqueMajor++;
				uniqueMinor = -1;
			}

			string uniqueName = String.Format (":{0}.{1}", newMajor, newMinor);

			// We could check if the unique name already has an owner here for absolute correctness.
			Debug.Assert (!NameHasOwner (uniqueName));

			return uniqueName;
		}

		public string Hello ()
		{
			if (Caller.UniqueName != null)
				throw new DBusException ("Failed", "Already handled an Hello message");

			string uniqueName = CreateUniqueName ();

			Console.Error.WriteLine ("Hello " + uniqueName + "!");
			Caller.UniqueName = uniqueName;
			Names[uniqueName] = Caller;

			// TODO: Verify the order in which these messages are sent.
			//NameAcquired (uniqueName);
			RaiseNameSignal ("Acquired", uniqueName);
			NameOwnerChanged (uniqueName, String.Empty, uniqueName);

			return uniqueName;
		}

		void RaiseNameSignal (string memberSuffix, string name)
		{
			// Name* signals on org.freedesktop.DBus are connection-specific.
			// We handle them here as a special case.

			Signal nameSignal = new Signal (Path, DBusInterface, "Name" + memberSuffix);
			MessageWriter mw = new MessageWriter ();
			mw.Write (name);
			nameSignal.message.Body = mw.ToArray ();
			nameSignal.message.Signature = Signature.StringSig;
			Caller.Send (nameSignal.message);
		}

		public string[] ListNames ()
		{
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

			if (!StartProcessNamed (name))
				throw new DBusException ("Spawn.ServiceNotFound", "The name {0} was not provided by any .service files", name);

			return StartReply.Success;
		}

		Dictionary<string, string> activationEnv = new Dictionary<string, string> ();
		public void UpdateActivationEnvironment (IDictionary<string, string> environment)
		{
			foreach (KeyValuePair<string, string> pair in environment) {
				if (pair.Key == String.Empty)
					continue;
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
				throw new DBusException ("NameHasNoOwner", "Could not get owner of name '{0}': no such name", name);

			return ((ServerConnection)c).UniqueName;
		}

		public uint GetConnectionUnixUser (string name)
		{
			if (name == DBusBusName)
				return (uint)Process.GetCurrentProcess ().Id;

			Connection c;
			if (!Names.TryGetValue (name, out c))
				throw new DBusException ("NameHasNoOwner", "Could not get UID of name '{0}': no such name", name);

			if (((ServerConnection)c).UserId == 0)
				throw new DBusException ("Failed", "Could not determine UID for '{0}'", name);

			return (uint)((ServerConnection)c).UserId;
		}

		Dictionary<string, Message> activationMessages = new Dictionary<string, Message> ();

		internal void HandleMessage (Message msg)
		{
			if (msg == null)
				return;

			//List<Connection> recipients = new List<Connection> ();
			HashSet<Connection> recipients = new HashSet<Connection> ();
			//HashSet<Connection> recipientsAll = new HashSet<Connection> (Connections);

			object fieldValue = msg.Header[FieldCode.Destination];
			if (fieldValue != null) {
				string destination = (string)fieldValue;
				Connection destConn;
				if (Names.TryGetValue (destination, out destConn))
					recipients.Add (destConn);
				else if (destination != DBusBusName && !destination.StartsWith (":") && (msg.Header.Flags & HeaderFlag.NoAutoStart) != HeaderFlag.NoAutoStart) {
					// Attempt activation
					StartProcessNamed (destination);
					//Thread.Sleep (5000);
					// TODO: Route the message to the newly activated service!
					activationMessages[destination] = msg;
					//if (Names.TryGetValue (destination, out destConn))
					//	recipients.Add (destConn);
					//else
					//	Console.Error.WriteLine ("Couldn't route message to activated service");
				} else if (destination != DBusBusName) {
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
			foreach (KeyValuePair<MatchRule, List<Connection>> pair in Rules) {
				if (recipients.IsSupersetOf (pair.Value))
					continue;
				if (pair.Key.MatchesHeader (msg)) {
					a.UnionWith (pair.Key.Args);
					recipientsMatchingHeader.UnionWith (pair.Value);
				}
			}

			MatchRule.Test (a, msg);

			foreach (KeyValuePair<MatchRule, List<Connection>> pair in Rules) {
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
		Dictionary<MatchRule, List<Connection>> Rules = new Dictionary<MatchRule, List<Connection>> ();
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
				throw new DBusException ("NameHasNoOwner", "Could not get owners of name '{0}': no such name", name);

			return new string[] { ((ServerConnection)c).UniqueName };
		}

		// Undocumented in spec
		public uint GetConnectionUnixProcessID (string connection_name)
		{
			Connection c;
			if (!Names.TryGetValue (connection_name, out c))
				throw new DBusException ("NameHasNoOwner", "Could not get PID of name '{0}': no such name", connection_name);

			uint pid;
			if (!c.Transport.TryGetPeerPid (out pid))
				throw new DBusException ("UnixProcessIdUnknown", "Could not determine PID for '{0}'", connection_name);

			return pid;
		}

		// Undocumented in spec
		public byte[] GetConnectionSELinuxSecurityContext (string connection_name)
		{
			throw new DBusException ("SELinuxSecurityContextUnknown", "Could not determine security context for '{0}'", connection_name);
		}

		// Undocumented in spec
		public void ReloadConfig ()
		{
			ScanServices ();
		}

		Dictionary<string, string> services = new Dictionary<string, string> ();
		public string svcPath = "/usr/share/dbus-1/services";
		public void ScanServices ()
		{
			services.Clear ();

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

					// TODO: use XdgNet
					// TODO: Validate names and trim strings
					if (name != null && cmd != null)
						services[name] = cmd;
				}
			}
		}

		public bool allowActivation = false;
		bool StartProcessNamed (string name)
		{
			Console.Error.WriteLine ("Start " + name);

			if (!allowActivation)
				return false;

			string cmd;
			if (!services.TryGetValue (name, out cmd))
				return false;

			try {
				StartProcess (cmd);
			} catch (Exception e) {
				Console.Error.WriteLine (e);
				return false;
			}

			return true;
		}

		// Can be "session" or "system"
		public string busType = "session";

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

				startInfo.EnvironmentVariables["DBUS_STARTER_BUS_TYPE"] = busType;
				startInfo.EnvironmentVariables["DBUS_" + busType.ToUpper () + "_BUS_ADDRESS"] = server.address;
				startInfo.EnvironmentVariables["DBUS_STARTER_ADDRESS"] = server.address;
				startInfo.EnvironmentVariables["DBUS_STARTER_BUS_TYPE"] = busType;
				Process myProcess = Process.Start (startInfo);
			} catch (Exception e) {
				Console.Error.WriteLine (e);
			}
		}
	}

	class ServerConnection : Connection
	{
		public Server Server;

		public ServerConnection (Transport t)
			: base (t)
		{
		}

		bool shouldDump = false;

		override internal void HandleMessage (Message msg)
		{
			if (!isConnected)
				return;

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

			Server.CurrentMessageConnection = this;
			Server.CurrentMessage = msg;

			try {
				if (shouldDump) {
					MessageDumper.WriteComment ("Handling:", Console.Out);
					MessageDumper.WriteMessage (msg, Console.Out);
				}

				if (UniqueName != null)
					msg.Header[FieldCode.Sender] = UniqueName;

				object fieldValue = msg.Header[FieldCode.Destination];
				if (fieldValue != null) {
					if ((string)fieldValue == ServerBus.DBusBusName) {

						// Workaround for our daemon only listening on a single path
						if (msg.Header.MessageType == DBus.MessageType.MethodCall)
							msg.Header[FieldCode.Path] = ServerBus.Path;

						base.HandleMessage (msg);
						//return;
					}
				}
				//base.HandleMessage (msg);

				Server.SBus.HandleMessage (msg);
			} finally {
				Server.CurrentMessageConnection = null;
				Server.CurrentMessage = null;
			}
		}

		override internal uint Send (Message msg)
		{
			if (!isConnected)
				return 0;

			/*
			if (msg.Header.MessageType == DBus.MessageType.Signal) {
				Signal signal = new Signal (msg);
				if (signal.Member == "NameAcquired" || signal.Member == "NameLost") {
					string dest = (string)msg.Header[FieldCode.Destination];
					if (dest != UniqueName)
						return 0;
				}
			}
			*/

			if (msg.Header.MessageType != DBus.MessageType.MethodReturn) {
				msg.Header[FieldCode.Sender] = ServerBus.DBusBusName;
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
			Console.Error.WriteLine ("Good! ~ServerConnection () for {0}", UniqueName ?? "(inactive)");
		}
	}

	internal class BusContext
	{
		protected Connection connection = null;
		public Connection Connection
		{
			get
			{
				return connection;
			}
		}

		protected Message message = null;
		internal Message CurrentMessage
		{
			get
			{
				return message;
			}
		}

		public string SenderName = null;
	}

	class DBusException : BusException
	{
		public DBusException (string errorNameSuffix, string format, params object[] args)
			: base (ServerBus.DBusInterface + ".Error." + errorNameSuffix, format, args)
		{
			// Note: This won't log ArgumentExceptions which are used in some places.
			if (Protocol.Verbose)
				Console.Error.WriteLine (Message);
		}
	}
}
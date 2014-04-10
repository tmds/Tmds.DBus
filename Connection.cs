// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Reflection;

namespace DBus
{
	using Authentication;
	using Transports;
	using Protocol;

	public class Connection
	{
		// Maybe we should use XDG/basedir or check an env var for this?
		const string machineUuidPath = @"/var/lib/dbus/machine-id";

		internal static readonly EndianFlag NativeEndianness =
			BitConverter.IsLittleEndian ? EndianFlag.Little : EndianFlag.Big;
		internal static readonly UUID MachineId =
			File.Exists (machineUuidPath) ? ReadMachineId (machineUuidPath) : UUID.Zero;

		Transport transport;
		bool isConnected = false;
		bool isShared = false;
		UUID Id = UUID.Zero;
		bool isAuthenticated = false;
		int serial = 0;

		// STRONG TODO: GET RID OF THAT SHIT
		internal Thread mainThread = Thread.CurrentThread;

		Dictionary<uint,PendingCall> pendingCalls = new Dictionary<uint,PendingCall> ();
		Queue<Message> inbound = new Queue<Message> ();
		Dictionary<ObjectPath,BusObject> registeredObjects = new Dictionary<ObjectPath,BusObject> ();

		public delegate void MonitorEventHandler (Message msg);
		public MonitorEventHandler Monitors; // subscribe yourself to this list of observers if you want to get notified about each incoming message

		protected Connection ()
		{

		}

		internal Connection (Transport transport)
		{
			this.transport = transport;
			transport.Connection = this;
		}

		internal Connection (string address)
		{
			OpenPrivate (address);
			Authenticate ();
		}

		public bool IsConnected {
			get {
				return isConnected;
			}
			internal set {
				isConnected = value;
			}
		}

		internal bool IsAuthenticated {
			get {
				return isAuthenticated;
			}
		}

		internal Transport Transport {
			get {
				return transport;
			} set {
				transport = value;
				transport.Connection = this;
			}
		}

		// TODO: Complete disconnection support
		public void Close ()
		{
			if (isShared)
				throw new Exception ("Cannot disconnect a shared Connection");

			if (!IsConnected)
				return;

			CloseInternal ();

			transport.Disconnect ();
			isConnected = false;
		}

		protected virtual void CloseInternal ()
		{
		}

		//should we do connection sharing here?
		public static Connection Open (string address)
		{
			Connection conn = new Connection ();
			conn.OpenPrivate (address);
			conn.Authenticate ();

			return conn;
		}

		void OpenPrivate (string address)
		{
			if (address == null)
				throw new ArgumentNullException ("address");

			AddressEntry[] entries = Address.Parse (address);
			if (entries.Length == 0)
				throw new Exception ("No addresses were found");

			int index = 0;
			while (index < entries.Length) {
				AddressEntry entry = entries[index++];

				Id = entry.GUID;
				try {
					Transport = Transport.Create (entry);
				} catch {
					if (index < entries.Length)
						continue;
					throw;
				}

				break;
			}

			isConnected = true;
		}

		void Authenticate ()
		{
			if (transport != null)
				transport.WriteCred ();

			SaslClient auth = new SaslClient ();
			auth.Identity = transport.AuthString ();
			auth.stream = transport.Stream;
			auth.Peer = new SaslPeer ();
			auth.Peer.Peer = auth;
			auth.Peer.stream = transport.Stream;

			if (!auth.Authenticate ())
				throw new Exception ("Authentication failure");

			if (Id != UUID.Zero)
				if (auth.ActualId != Id)
					throw new Exception ("Authentication failure: Unexpected GUID");

			if (Id == UUID.Zero)
				Id = auth.ActualId;

			isAuthenticated = true;
		}

		// Interlocked.Increment() handles the overflow condition for uint correctly,
		// so it's ok to store the value as an int but cast it to uint
		internal uint GenerateSerial ()
		{
			return (uint)Interlocked.Increment (ref serial);
		}

		internal Message SendWithReplyAndBlock (Message msg)
		{
			PendingCall pending = SendWithReply (msg);
			return pending.Reply;
		}

		internal PendingCall SendWithReply (Message msg)
		{
			msg.ReplyExpected = true;

			if (msg.Header.Serial == 0)
				msg.Header.Serial = GenerateSerial ();

			// Should we throttle the maximum number of concurrent PendingCalls?
			// Should we support timeouts?
			PendingCall pending = new PendingCall (this);
			lock (pendingCalls)
				pendingCalls[msg.Header.Serial] = pending;

			Send (msg);

			return pending;
		}

		internal virtual uint Send (Message msg)
		{
			if (msg.Header.Serial == 0)
				msg.Header.Serial = GenerateSerial ();

			transport.WriteMessage (msg);

			return msg.Header.Serial;
		}

		//temporary hack
		internal void DispatchSignals ()
		{
			lock (inbound) {
				while (inbound.Count != 0) {
					Message msg = inbound.Dequeue ();
					HandleSignal (msg);
				}
			}
		}

		public void Iterate ()
		{
			Message msg = transport.ReadMessage ();

			HandleMessage (msg);
			DispatchSignals ();
		}

		internal virtual void HandleMessage (Message msg)
		{
			if (msg == null)
				return;

			//TODO: support disconnection situations properly and move this check elsewhere
			if (msg == null)
				throw new ArgumentNullException ("msg", "Cannot handle a null message; maybe the bus was disconnected");

			//TODO: Restrict messages to Local ObjectPath?

			{
				object field_value = msg.Header[FieldCode.ReplySerial];
				if (field_value != null) {
					uint reply_serial = (uint)field_value;
					PendingCall pending;

					lock (pendingCalls) {
						if (pendingCalls.TryGetValue (reply_serial, out pending)) {
							if (pendingCalls.Remove (reply_serial))
								pending.Reply = msg;

							return;
						}
					}

					//we discard reply messages with no corresponding PendingCall
					if (ProtocolInformation.Verbose)
						Console.Error.WriteLine ("Unexpected reply message received: MessageType='" + msg.Header.MessageType + "', ReplySerial=" + reply_serial);

					return;
				}
			}

			switch (msg.Header.MessageType) {
				case MessageType.MethodCall:
					MessageContainer method_call = MessageContainer.FromMessage (msg);
					HandleMethodCall (method_call);
					break;
				case MessageType.Signal:
					//HandleSignal (msg);
					lock (inbound)
						inbound.Enqueue (msg);
					break;
				case MessageType.Error:
					//TODO: better exception handling
					MessageContainer error = MessageContainer.FromMessage (msg);
					string errMsg = String.Empty;
					if (msg.Signature.Value.StartsWith ("s")) {
						MessageReader reader = new MessageReader (msg);
						errMsg = reader.ReadString ();
					}
					Console.Error.WriteLine ("Remote Error: Signature='" + msg.Signature.Value + "' " + error.ErrorName + ": " + errMsg);
					break;
				case MessageType.Invalid:
				default:
					throw new Exception ("Invalid message received: MessageType='" + msg.Header.MessageType + "'");
			}
		}

		//this might need reworking with MulticastDelegate
		internal void HandleSignal (Message msg)
		{
			var signal = MessageContainer.FromMessage (msg);

			//TODO: this is a hack, not necessary when MatchRule is complete
			MatchRule rule = new MatchRule ();
			rule.MessageType = MessageType.Signal;
			rule.Fields.Add (FieldCode.Interface, new MatchTest (signal.Interface));
			rule.Fields.Add (FieldCode.Member, new MatchTest (signal.Member));
			//rule.Fields.Add (FieldCode.Sender, new MatchTest (signal.Sender));
			rule.Fields.Add (FieldCode.Path, new MatchTest (signal.Path));

			Delegate dlg;
			if (Handlers.TryGetValue (rule, out dlg) && dlg != null) {
				MethodInfo mi = dlg.GetType ().GetMethod ("Invoke");

				bool compatible = false;
				Signature inSig, outSig;

				if (TypeImplementer.SigsForMethod(mi, out inSig, out outSig))
					if (outSig == Signature.Empty && inSig == msg.Signature)
						compatible = true;

				if (!compatible) {
					if (ProtocolInformation.Verbose)
						Console.Error.WriteLine ("Signal argument mismatch: " + signal.Interface + '.' + signal.Member);
					return;
				}

				//signals have no return value
				dlg.DynamicInvoke (MessageHelper.GetDynamicValues (msg, mi.GetParameters ()));
			} else {
				//TODO: how should we handle this condition? sending an Error may not be appropriate in this case
				if (ProtocolInformation.Verbose)
					Console.Error.WriteLine ("Warning: No signal handler for " + signal.Member);
			}
		}

		internal Dictionary<MatchRule,Delegate> Handlers = new Dictionary<MatchRule,Delegate> ();

		//very messy
		internal void MaybeSendUnknownMethodError (MessageContainer method_call)
		{
			Message msg = MessageHelper.CreateUnknownMethodError (method_call);
			if (msg != null)
				Send (msg);
		}

		//not particularly efficient and needs to be generalized
		internal void HandleMethodCall (MessageContainer method_call)
		{
			//TODO: Ping and Introspect need to be abstracted and moved somewhere more appropriate once message filter infrastructure is complete

			//FIXME: these special cases are slightly broken for the case where the member but not the interface is specified in the message
			if (method_call.Interface == "org.freedesktop.DBus.Peer") {
				switch (method_call.Member) {
					case "Ping":
						Send (MessageHelper.ConstructReply (method_call));
						return;
					case "GetMachineId":
						if (MachineId != UUID.Zero) {
							Send (MessageHelper.ConstructReply (method_call, MachineId.ToString ()));
							return;
						} else {
							// Might want to send back an error here?
						}
						break;
				}
			}

			if (method_call.Interface == "org.freedesktop.DBus.Introspectable" && method_call.Member == "Introspect") {
				Introspector intro = new Introspector ();
				intro.root_path = method_call.Path;
				intro.WriteStart ();

				//FIXME: do this properly
				//this is messy and inefficient
				List<string> linkNodes = new List<string> ();
				int depth = method_call.Path.Decomposed.Length;
				foreach (ObjectPath pth in registeredObjects.Keys) {
					if (pth.Value == (method_call.Path.Value)) {
						ExportObject exo = (ExportObject)registeredObjects[pth];
						exo.WriteIntrospect (intro);
					} else {
						for (ObjectPath cur = pth ; cur != null ; cur = cur.Parent) {
							if (cur.Value == method_call.Path.Value) {
								string linkNode = pth.Decomposed[depth];
								if (!linkNodes.Contains (linkNode)) {
									intro.WriteNode (linkNode);
									linkNodes.Add (linkNode);
								}
							}
						}
					}
				}

				intro.WriteEnd ();

				Message reply = MessageHelper.ConstructReply (method_call, intro.Xml);
				Send (reply);
				return;
			}

			BusObject bo;
			if (registeredObjects.TryGetValue (method_call.Path, out bo)) {
				ExportObject eo = (ExportObject)bo;
				eo.HandleMethodCall (method_call);
			} else {
				MaybeSendUnknownMethodError (method_call);
			}
		}

		public object GetObject (Type type, string bus_name, ObjectPath path)
		{
			if (!CheckBusNameExists (bus_name))
				return null;

			//if the requested type is an interface, we can implement it efficiently
			//otherwise we fall back to using a transparent proxy
			if (type.IsInterface || type.IsAbstract) {
				return BusObject.GetObject (this, bus_name, path, type);
			} else {
				if (ProtocolInformation.Verbose)
					Console.Error.WriteLine ("Warning: Note that MarshalByRefObject use is not recommended; for best performance, define interfaces");

				BusObject busObject = new BusObject (this, bus_name, path);
				DProxy prox = new DProxy (busObject, type);
				return prox.GetTransparentProxy ();
			}
		}

		public T GetObject<T> (string bus_name, ObjectPath path)
		{
			return (T)GetObject (typeof (T), bus_name, path);
		}

		protected virtual bool CheckBusNameExists (string busName)
		{
			return true;
		}

		public void Register (ObjectPath path, object obj)
		{
			ExportObject eo = ExportObject.CreateExportObject (this, path, obj);
			eo.Registered = true;

			//TODO: implement some kind of tree data structure or internal object hierarchy. right now we are ignoring the name and putting all object paths in one namespace, which is bad
			registeredObjects[path] = eo;
		}

		public object Unregister (ObjectPath path)
		{
			BusObject bo;

			if (!registeredObjects.TryGetValue (path, out bo))
				throw new Exception ("Cannot unregister " + path + " as it isn't registered");

			registeredObjects.Remove (path);

			ExportObject eo = (ExportObject)bo;
			eo.Registered = false;

			return eo.Object;
		}

		//these look out of place, but are useful
		internal protected virtual void AddMatch (string rule)
		{
		}

		internal protected virtual void RemoveMatch (string rule)
		{
		}

		static UUID ReadMachineId (string fname)
		{
			byte[] data = File.ReadAllBytes (fname);
			if (data.Length < 33)
				return UUID.Zero;

			return UUID.Parse (System.Text.Encoding.ASCII.GetString (data, 0, 32));
		}
	}
}

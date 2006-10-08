// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

using System.Threading;

using System.Reflection;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	using Authentication;
	using Transports;

	public class Connection
	{
		//TODO: reduce/correct visibility of these when appropriate
		public Stream ns = null;
		public long SocketHandle;

		protected Transport transport;
		public Transport Transport {
			get {
				return transport;
			} set {
				transport = value;
			}
		}

		//TODO: reduce visibility when test-server no longer needs this
		//protected Connection ()
		public Connection ()
		{
		}

		public Connection (string address)
		{
			OpenPrivate (address);
			Authenticate ();
		}

		protected bool isConnected = false;
		public bool IsConnected
		{
			get {
				return isConnected;
			}
		}

		public static Connection Open (string address)
		{
			Connection conn = new Connection ();
			conn.OpenPrivate (address);
			conn.Authenticate ();

			return conn;
		}

		//TODO: reduce visibility when test-server no longer needs this
		//protected void OpenPrivate (string address)
		public void OpenPrivate (string address)
		{
			string path;
			bool abstr;

			if (address == null)
				throw new ArgumentNullException ("address");

			if (!Address.Parse (address, out path, out abstr))
				throw new ArgumentException ("Invalid D-Bus address: '" + address + "'", "address");

			Open (path, abstr);

			isConnected = true;
		}

		void Open (string path, bool abstr)
		{
			//transport = new UnixMonoTransport (path, abstr);
			transport = new UnixNativeTransport (path, abstr);
			ns = transport.Stream;
			SocketHandle = transport.SocketHandle;
		}

		public void Authenticate ()
		{
			SaslClient auth = new SaslClient (this);
			auth.Run ();
			isAuthenticated = true;
		}

		protected bool isAuthenticated = false;
		public bool IsAuthenticated
		{
			get {
				return isAuthenticated;
			}
		}

		protected string unique_name = null;
		public virtual string UniqueName
		{
			get {
				return unique_name;
			} set {
				if (unique_name != null)
					throw new Exception ("Unique name of a Connection can only be set once");
				unique_name = value;
			}
		}

		//Interlocked.Increment() handles the overflow condition for uint correctly, so it's ok to store the value as an int but cast it to uint
		protected int serial = 0;
		protected uint GenerateSerial ()
		{
			//return ++serial;
			return (uint)Interlocked.Increment (ref serial);
		}




		public Message SendWithReplyAndBlock (Message msg)
		{
			uint id = SendWithReply (msg);

			Message retMsg = WaitForReplyTo (id);
			DispatchSignals ();

			return retMsg;
		}

		public uint SendWithReply (Message msg)
		{
			msg.ReplyExpected = true;
			return Send (msg);
		}

		public uint Send (Message msg)
		{
			msg.Header.Serial = GenerateSerial ();

			msg.WriteHeader ();

			WriteMessage (msg);

			//Outbound.Enqueue (msg);
			//temporary
			//Flush ();

			return msg.Header.Serial;
		}

		//could be cleaner
		protected void WriteMessage (Message msg)
		{
			//Monitor.Enter (ns);

			//ns.Write (msg.HeaderData, 0, msg.HeaderSize);
			//Console.WriteLine ("headerSize: " + msg.HeaderSize);
			//Console.WriteLine ("headerLength: " + msg.HeaderData.Length);
			//Console.WriteLine ();
			ns.Write (msg.HeaderData, 0, msg.HeaderData.Length);
			if (msg.Body != null) {
				ns.Write (msg.Body, 0, msg.Body.Length);
				//msg.Body.WriteTo (ns);
			}

			//Monitor.Exit (ns);
		}

		protected Queue<Message> Inbound = new Queue<Message> ();
		/*
		protected Queue<Message> Outbound = new Queue<Message> ();

		public void Flush ()
		{
			//should just iterate the enumerator here
			while (Outbound.Count != 0) {
				Message msg = Outbound.Dequeue ();
				WriteMessage (msg);
			}
		}

		public bool ReadWrite (int timeout_milliseconds)
		{
			//TODO

			return true;
		}

		public bool ReadWrite ()
		{
			return ReadWrite (-1);
		}

		public bool Dispatch ()
		{
			//TODO
			Message msg = Inbound.Dequeue ();
			//HandleMessage (msg);

			return true;
		}

		public bool ReadWriteDispatch (int timeout_milliseconds)
		{
			//TODO
			return Dispatch ();
		}

		public bool ReadWriteDispatch ()
		{
			return ReadWriteDispatch (-1);
		}
		*/

		public Message ReadMessage ()
		{
			//Monitor.Enter (ns);

			//FIXME: fix reading algorithm to work in one step
			//this code is a bit silly and inefficient
			//hopefully it's at least correct and avoids polls for now

			int read;

			byte[] buf = new byte[16];
			read = ns.Read (buf, 0, 16);

			if (read != 16)
				throw new Exception ("Header read length mismatch: " + read + " of expected " + "16");

			MemoryStream ms = new MemoryStream ();

			ms.Write (buf, 0, 16);

			int toRead;
			int bodyLen;

			//FIXME: use endianness instead of failing on non-native endianness
			EndianFlag endianness = (EndianFlag)buf[0];
			if (endianness != Connection.NativeEndianness)
				throw new NotImplementedException ("Only native-endian message reading is currently supported");

			byte version = buf[3];
			if (version < Protocol.MinVersion || version > Protocol.MaxVersion)
				throw new NotSupportedException ("Protocol version '" + version.ToString () + "' is not supported");

			if (Protocol.Verbose)
				if (version != Protocol.Version)
					Console.Error.WriteLine ("Warning: Protocol version '" + version.ToString () + "' is not explicitly supported but may be compatible");

			//TODO: remove this limitation
			if (BitConverter.ToUInt32 (buf, 4) > Int32.MaxValue || BitConverter.ToUInt32 (buf, 12) > Int32.MaxValue)
				throw new NotImplementedException ("Long messages are not yet supported");

			bodyLen = (int)BitConverter.ToUInt32 (buf, 4);
			toRead = (int)BitConverter.ToUInt32 (buf, 12);

			toRead = Protocol.Padded ((int)toRead, 8);

			buf = new byte[toRead];

			read = ns.Read (buf, 0, toRead);

			if (read != toRead)
				throw new Exception ("Read length mismatch: " + read + " of expected " + toRead);

			ms.Write (buf, 0, buf.Length);

			Message msg = new Message ();
			msg.HeaderData = ms.ToArray ();

			//read the body
			if (bodyLen != 0) {
				//FIXME
				//msg.Body = new byte[(int)msg.Header->Length];
				byte[] body = new byte[bodyLen];

				//int len = ns.Read (msg.Body, 0, msg.Body.Length);
				int len = ns.Read (body, 0, bodyLen);

				//if (len != msg.Body.Length)
				if (len != bodyLen)
					throw new Exception ("Message body size mismatch");

				//msg.Body = new MemoryStream (body);
				msg.Body = body;
			}

			//Monitor.Exit (ns);

			//this needn't be done here
			msg.ParseHeader ();

			return msg;
		}

		//needs to be done properly
		public Message WaitForReplyTo (uint id)
		{
			//Message msg = Inbound.Dequeue ();
			Message msg;

			while ((msg = ReadMessage ()) != null) {
				if (msg.Header.Fields.ContainsKey (FieldCode.ReplySerial))
					if ((uint)msg.Header.Fields[FieldCode.ReplySerial] == id)
						return msg;

				HandleMessage (msg);
			}

			return null;
		}


		//temporary hack
		protected void DispatchSignals ()
		{
			lock (Inbound) {
				while (Inbound.Count != 0) {
					Message msg = Inbound.Dequeue ();
					HandleSignal (msg);
				}
			}
		}

		//temporary hack
		public void Iterate ()
		{
			//Message msg = Inbound.Dequeue ();
			Message msg = ReadMessage ();
			HandleMessage (msg);
			DispatchSignals ();
		}

		protected void HandleMessage (Message msg)
		{
			switch (msg.Header.MessageType) {
				case MessageType.MethodCall:
					MethodCall method_call = new MethodCall (msg);
					HandleMethodCall (method_call);
					break;
				case MessageType.MethodReturn:
					MethodReturn method_return = new MethodReturn (msg);
					if (PendingCalls.ContainsKey (method_return.ReplySerial)) {
						//TODO: pending calls
						//return msg;
					}
					//if the signature is empty, it's just a token return message
					if (msg.Signature != Signature.Empty)
						Console.Error.WriteLine ("Warning: Couldn't handle async MethodReturn message for request id " + method_return.ReplySerial + " with signature '" + msg.Signature + "'");
					break;
				case MessageType.Error:
					//TODO: better exception handling
					Error error = new Error (msg);
					string errMsg = "";
					if (msg.Signature.Value == "s") {
						MessageReader reader = new MessageReader (msg);
						reader.GetValue (out errMsg);
					}
					//throw new Exception ("Remote Error: Signature='" + msg.Signature.Value + "' " + error.ErrorName + ": " + errMsg);
					//if (Protocol.Verbose)
					Console.Error.WriteLine ("Remote Error: Signature='" + msg.Signature.Value + "' " + error.ErrorName + ": " + errMsg);
					break;
				case MessageType.Signal:
					//HandleSignal (msg);
					lock (Inbound)
						Inbound.Enqueue (msg);
					break;
				case MessageType.Invalid:
				default:
					throw new Exception ("Invalid message received: MessageType='" + msg.Header.MessageType + "'");
			}
		}

		protected Dictionary<uint,Message> PendingCalls = new Dictionary<uint,Message> ();


		//this might need reworking with MulticastDelegate
		protected void HandleSignal (Message msg)
		{
			Signal signal = new Signal (msg);

			string matchRule = MessageFilter.CreateMatchRule (MessageType.Signal, signal.Path, signal.Interface, signal.Member);

			if (Handlers.ContainsKey (matchRule)) {
				Delegate dlg = Handlers[matchRule];
				//dlg.DynamicInvoke (GetDynamicValues (msg));

				MethodInfo mi = dlg.Method;
				//signals have no return value
				dlg.DynamicInvoke (MessageHelper.GetDynamicValues (msg, mi.GetParameters ()));

			} else {
				//TODO: how should we handle this condition? sending an Error may not be appropriate in this case
				if (Protocol.Verbose)
					Console.Error.WriteLine ("Warning: No signal handler for " + signal.Member);
			}
		}

		public Dictionary<string,Delegate> Handlers = new Dictionary<string,Delegate> ();

		//not particularly efficient and needs to be generalized
		protected void HandleMethodCall (MethodCall method_call)
		{
			//Console.Error.WriteLine ("method_call destination: " + method_call.Destination);
			//Console.Error.WriteLine ("method_call path: " + method_call.Path);

			//TODO: Ping and Introspect need to be abstracted and moved somewhere more appropriate once message filter infrastructure is complete

			if (method_call.Interface == "org.freedesktop.DBus.Peer" && method_call.Member == "Ping") {
				object[] pingRet = new object[0];
				Message reply = MessageHelper.ConstructReplyFor (method_call, pingRet);
				Send (reply);
				return;
			}

			if (method_call.Interface == "org.freedesktop.DBus.Introspectable" && method_call.Member == "Introspect") {
				Introspector intro = new Introspector ();
				intro.root_path = method_call.Path;
				//FIXME: do this properly
				foreach (ObjectPath pth in RegisteredObjects.Keys) {
					if (pth.Value.StartsWith (method_call.Path.Value)) {
						intro.target_path = pth;
						intro.target_type = RegisteredObjects[pth].GetType ();
					}
				}
				intro.HandleIntrospect ();
				//Console.Error.WriteLine (intro.xml);

				object[] introRet = new object[1];
				introRet[0] = intro.xml;
				Message reply = MessageHelper.ConstructReplyFor (method_call, introRet);
				Send (reply);
				return;
			}

			if (RegisteredObjects.ContainsKey (method_call.Path)) {
				object obj = RegisteredObjects[method_call.Path];
				Type type = obj.GetType ();
				//object retObj = type.InvokeMember (msg.Member, BindingFlags.InvokeMethod, null, obj, MessageHelper.GetDynamicValues (msg));

				string methodName = method_call.Member;

				//map property accessors
				//FIXME: this needs to be done properly, not with simple String.Replace
				//special case for Notifications left as a reminder that this is broken
				if (method_call.Interface == "org.freedesktop.Notifications") {
					methodName = methodName.Replace ("Get", "get_");
					methodName = methodName.Replace ("Set", "set_");
				}

				//FIXME: breaks for overloaded methods
				MethodInfo mi = type.GetMethod (methodName, BindingFlags.Public | BindingFlags.Instance);

				//TODO: send errors instead of passing up local exceptions for these

				if (mi == null)
					throw new Exception ("The requested method could not be resolved");

				//FIXME: such a simple approach won't work unfortunately
				//if (!Mapper.IsPublic (mi))
				//	throw new Exception ("The resolved method is not marked as being public on this bus");

				object retObj = null;
			 	try {
					object[] inArgs = MessageHelper.GetDynamicValues (method_call.message, mi.GetParameters ());
					retObj = mi.Invoke (obj, inArgs);
				} catch (TargetInvocationException e) {
					//TODO: consider whether it's correct to send an error for calls that don't expect a reply

					//TODO: complete exception sending support
					//TODO: method not found etc. exceptions
					Exception ie = e.InnerException;
					if (Protocol.Verbose) {
						Console.Error.WriteLine ();
						Console.Error.WriteLine (ie);
						Console.Error.WriteLine ();
					}

					if (!method_call.message.ReplyExpected) {
						Console.Error.WriteLine ();
						Console.Error.WriteLine ("Warning: Not sending Error message (" + ie.GetType ().Name + ") as reply because no reply was expected by call to '" + (method_call.Interface + "." + method_call.Member) + "'");
						Console.Error.WriteLine ();
						return;
					}

					Error error = new Error (Mapper.GetInterfaceName (ie.GetType ()), method_call.message.Header.Serial);
					error.message.Signature = new Signature (DType.String);

					MessageWriter writer = new MessageWriter ();
					writer.Write (ie.Message);
					error.message.Body = writer.ToArray ();

					//TODO: we should be more strict here, but this fallback was added as a quick fix for p2p
					if (method_call.Sender != null)
						error.message.Header.Fields[FieldCode.Destination] = method_call.Sender;

					error.message.Header.Fields[FieldCode.Interface] = method_call.Interface;
					error.message.Header.Fields[FieldCode.Member] = method_call.Member;

					Send (error.message);
					return;
				}

				if (method_call.message.ReplyExpected) {
					/*
					object[] retObjs;

					if (retObj == null) {
						retObjs = new object[0];
					} else {
						retObjs = new object[1];
						retObjs[0] = retObj;
					}

					Message reply = ConstructReplyFor (method_call, retObjs);
					*/
					Message reply = MessageHelper.ConstructReplyFor (method_call, mi.ReturnType, retObj);
					Send (reply);
				}
			} else {
				//FIXME: send the appropriate Error message
				Console.Error.WriteLine ("Warning: No method handler for " + method_call.Member);
			}
		}

		protected Dictionary<ObjectPath,object> RegisteredObjects = new Dictionary<ObjectPath,object> ();

		//FIXME: this shouldn't be part of the core API
		//that also applies to much of the other object mapping code
		//it should cache proxies and objects, really

		//inspired by System.Activator
		public object GetObject (Type type, string bus_name, ObjectPath path)
		{
			BusObject busObject = new BusObject (this, bus_name, path);
			DProxy prox = new DProxy (busObject, type);
			return prox.GetTransparentProxy ();
		}

		/*
		public object GetObject (Type type, string bus_name, ObjectPath path)
		{
			return BusObject.GetObject (this, bus_name, path, type);
		}
		*/

		public T GetObject<T> (string bus_name, ObjectPath path)
		{
			return (T)GetObject (typeof (T), bus_name, path);
		}

		public void Register (string bus_name, ObjectPath path, object obj)
		{
			Type type = obj.GetType ();

			BusObject busObject = new BusObject (this, bus_name, path);

			foreach (EventInfo ei in type.GetEvents (BindingFlags.Public | BindingFlags.Instance)) {
				//hook up only events that are public to the bus
				if (!Mapper.IsPublic (ei))
					continue;

				Delegate dlg = busObject.GetHookupDelegate (ei);
				ei.AddEventHandler (obj, dlg);
			}

			//FIXME: implement some kind of tree data structure or internal object hierarchy. right now we are ignoring the name and putting all object paths in one namespace, which is bad
			RegisteredObjects[path] = obj;
		}

		public object Unregister (string bus_name, ObjectPath path)
		{
			//TODO: make use of bus_name

			if (!RegisteredObjects.ContainsKey (path))
				throw new Exception ("Cannot unmarshal " + path + " as it isn't marshaled");
			object obj = RegisteredObjects[path];

			RegisteredObjects.Remove (path);

			//FIXME: complete unmarshaling including the handlers we added etc.

			return obj;
		}

		//these look out of place, but are useful
		public virtual void AddMatch (string rule)
		{
		}

		public virtual void RemoveMatch (string rule)
		{
		}

		static Connection ()
		{
			if (BitConverter.IsLittleEndian)
				NativeEndianness = EndianFlag.Little;
			else
				NativeEndianness = EndianFlag.Big;
		}

		public static readonly EndianFlag NativeEndianness;
	}
}

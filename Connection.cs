// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Runtime.InteropServices;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	public partial class Connection
	{
		const string SYSTEM_BUS_ADDRESS = "unix:path=/var/run/dbus/system_bus_socket";

		public Socket sock = null;
		Stream ns = null;
		Transport transport;

		public Connection () : this (true)
		{
		}

		public Connection (bool autoConnect)
		{
			if (!autoConnect)
				return;

			string path;
			bool abstr;

			string sessAddr = System.Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");
			Address.Parse (sessAddr, out path, out abstr);

			//not really correct
			if (String.IsNullOrEmpty (path)) {
				string sysAddr = System.Environment.GetEnvironmentVariable ("DBUS_SYSTEM_BUS_ADDRESS");

				if (String.IsNullOrEmpty (sysAddr))
					sysAddr = SYSTEM_BUS_ADDRESS;

				Address.Parse (sysAddr, out path, out abstr);
			}

			Open (path, abstr);
			Authenticate ();
		}

		public void Open (string address)
		{
			string path;
			bool abstr;

			Address.Parse (address, out path, out abstr);
			Open (path, abstr);
		}

		public void Open (string path, bool abstr)
		{
			transport = new UnixTransport (path, abstr);

			sock = transport.socket;

			sock.Blocking = true;
			//ns = new UnixStream ((int)sock.Handle);
			ns = new NetworkStream (sock);
		}

		uint serial = 0;
		public uint GenerateSerial ()
		{
			return ++serial;
		}




		public Message SendWithReplyAndBlock (Message msg)
		{
			uint id = SendWithReply (msg);

			Message retMsg = WaitForReplyTo (id);

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
			//ns.Write (msg.HeaderData, 0, msg.HeaderSize);
			//Console.WriteLine ("headerSize: " + msg.HeaderSize);
			//Console.WriteLine ("headerLength: " + msg.HeaderData.Length);
			//Console.WriteLine ();
			ns.Write (msg.HeaderData, 0, msg.HeaderData.Length);
			if (msg.Body != null) {
				//ns.Write (msg.Body, 0, msg.BodySize);
				msg.Body.WriteTo (ns);
			}
		}

		protected Queue<Message> Inbound = new Queue<Message> ();
		protected Queue<Message> Outbound = new Queue<Message> ();

		public void Flush ()
		{
			//should just iterate the enumerator here
			while (Outbound.Count != 0) {
				Message msg = Outbound.Dequeue ();
				WriteMessage (msg);
			}
		}

		public Message ReadMessage ()
		{
			//FIXME: fix reading algorithm to work in one step
			//this code is a bit silly and inefficient
			//hopefully it's at least correct and avoids polls for now

			byte[] buf = new byte[16];
			ns.Read (buf, 0, 16);

			MemoryStream ms = new MemoryStream ();

			ms.Write (buf, 0, 16);

			int toRead;
			int bodyLen;

			//TODO: remove this last remaining bit of unsafe
			unsafe {
				fixed (byte* pbuf = buf) {
					DHeader* hdr = (DHeader*)pbuf;
					bodyLen = (int)hdr->Length;
					toRead = Message.Padded ((int)hdr->HeaderLength, 8);
				}
			}

			buf = new byte[toRead];

			int read = ns.Read (buf, 0, toRead);

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

				msg.Body = new MemoryStream (body);
			}

			//this needn't be done here
			Message.IsReading = true;
			msg.ParseHeader ();
			Message.IsReading = false;

			return msg;
		}

		//this is just a start
		//needs to be done properly
		public Message WaitForReplyTo (uint id)
		{
			//Message msg = Inbound.Dequeue ();

			Message msg;

			while ((msg = ReadMessage ()) != null) {
				switch (msg.Header.MessageType) {
					case MessageType.Invalid:
						throw new Exception ("Invalid message received");
					case MessageType.MethodCall:
						MethodCall method_call = new MethodCall (msg);
						HandleMethodCall (method_call);
						break;
					case MessageType.MethodReturn:
						MethodReturn method_return = new MethodReturn (msg);
						if (method_return.ReplySerial == id)
							return msg;
						break;
					case MessageType.Error:
						Error error = new Error (msg);
						string errMsg = "";
						//if (error.Signature.Value == "s")
						if (msg.Signature.Value == "s")
							Message.GetValue (msg.Body, out errMsg);
						//throw new Exception ("Error: " + error.ErrorName + ": " + "signature='" + error.Signature.Value + "': " + errMsg);
						throw new Exception ("Error: " + error.ErrorName + ": " + "signature='" + msg.Signature.Value + "': " + errMsg);
					case MessageType.Signal:
						//Signal signal = new Signal (msg);
						HandleSignal (msg);
						break;
				}
			}

			return null;
		}


		//temporary hack
		public void Iterate ()
		{
			//Message msg = Inbound.Dequeue ();

			Message msg;

			msg = ReadMessage ();

			switch (msg.Header.MessageType) {
				case MessageType.Invalid:
					throw new Exception ("Invalid message received");
				case MessageType.MethodCall:
					MethodCall method_call = new MethodCall (msg);
					HandleMethodCall (method_call);
					break;
				case MessageType.MethodReturn:
					MethodReturn method_return = new MethodReturn (msg);
					if (PendingCalls.ContainsKey (method_return.ReplySerial)) {
						Console.Error.WriteLine ("Couldn't handle async method_return message");
						//return msg;
					}
					break;
				case MessageType.Error:
					//TODO: better exception handling
					Error error = new Error (msg);
					string errMsg = "";
					if (msg.Signature.Value == "s")
						Message.GetValue (msg.Body, out errMsg);
					throw new Exception ("Remote Error: type='" + msg.Signature.Value + "' " + error.ErrorName + ": " + errMsg);
				case MessageType.Signal:
					HandleSignal (msg);
					break;
			}
		}

		public Dictionary<uint,Message> PendingCalls = new Dictionary<uint,Message> ();


		//this might need reworking with MulticastDelegate
		public void HandleSignal (Message msg)
		{
			Signal signal = new Signal (msg);

			if (Handlers.ContainsKey (signal.Member)) {
				Delegate dlg = Handlers[signal.Member];
				//dlg.DynamicInvoke (GetDynamicValues (msg));

				System.Reflection.MethodInfo mi = dlg.Method;
				System.Reflection.ParameterInfo[]  parms = mi.GetParameters ();
				Type[] sig = new Type[parms.Length];
				for (int i = 0 ; i != parms.Length ; i++)
					sig[i] = parms[i].ParameterType;
				//object retObj = mi.Invoke (null, GetDynamicValues (msg, sig));
				//signals have no return value
				dlg.DynamicInvoke (GetDynamicValues (msg, sig));

			} else {
				Console.Error.WriteLine ("Warning: No signal handler for " + signal.Member);
			}
		}

		public Dictionary<string,Delegate> Handlers = new Dictionary<string,Delegate> ();

		//should generalize this method
		//it is duplicated in DProxy
		protected Message ConstructReplyFor (MethodCall method_call, object[] vals)
		{
			MethodReturn method_return = new MethodReturn (method_call.message.Header.Serial);
			//Message replyMsg = new Message ();
			Message replyMsg = method_return.message;

			//replyMsg.Header.MessageType = MessageType.MethodReturn;
			//replyMsg.ReplyExpected = false;

			Signature inSig = DProxy.GetSig (vals);

			if (vals != null && vals.Length != 0) {
				replyMsg.Body = new System.IO.MemoryStream ();

				foreach (object arg in vals)
					Message.Write (replyMsg.Body, arg.GetType (), arg);
			}

			//FIXME: this breaks the abstraction
			replyMsg.Header.Fields[FieldCode.ReplySerial] = method_call.message.Header.Serial;
			replyMsg.Header.Fields[FieldCode.Destination] = method_call.Sender;
			if (inSig.Data.Length != 0)
				replyMsg.Header.Fields[FieldCode.Signature] = inSig;

			//replyMsg.WriteHeader ();

			return replyMsg;
		}

		//not particularly efficient and needs to be generalized
		public void HandleMethodCall (MethodCall method_call)
		{
			if (RegisteredObjects.ContainsKey (method_call.Interface)) {
				object obj = RegisteredObjects[method_call.Interface];
				Type type = obj.GetType ();
				//object retObj = type.InvokeMember (msg.Member, System.Reflection.BindingFlags.InvokeMethod, null, obj, GetDynamicValues (msg));

				string methodName = method_call.Member;

				//map property accessors
				//TODO: this needs to be done properly, not with simple String.Replace
				//special case for Notifications left as a reminder that this is broken
				if (method_call.Interface == "org.freedesktop.Notifications") {
					methodName = methodName.Replace ("Get", "get_");
					methodName = methodName.Replace ("Set", "set_");
				}

				//FIXME: breaks for overloaded methods
				System.Reflection.MethodInfo mi = type.GetMethod (methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				System.Reflection.ParameterInfo[]  parms = mi.GetParameters ();
				Type[] sig = new Type[parms.Length];
				for (int i = 0 ; i != parms.Length ; i++)
					sig[i] = parms[i].ParameterType;
				object retObj = mi.Invoke (obj, GetDynamicValues (method_call.message, sig));

				if (method_call.message.ReplyExpected) {
					object[] retObjs;

					if (retObj == null) {
						retObjs = new object[0];
					} else {
						retObjs = new object[1];
						retObjs[0] = retObj;
					}

					Message reply = ConstructReplyFor (method_call, retObjs);
					Send (reply);
				}
			} else {
				Console.Error.WriteLine ("Warning: No method handler for " + method_call.Member);
			}
		}

		public Dictionary<Type,string> RegisteredTypes = new Dictionary<Type,string> ();

		protected Dictionary<string,object> RegisteredObjects = new Dictionary<string,object> ();

		public object[] GetDynamicValues (Message msg, Type[] types)
		{
			List<object> vals = new List<object> ();

			if (msg.Body != null) {
				foreach (Type type in types) {
					object arg;
					Message.GetValue (msg.Body, type, out arg);
					vals.Add (arg);
				}
			}

			return vals.ToArray ();
		}

		public object[] GetDynamicValues (Message msg)
		{
			List<object> vals = new List<object> ();

			if (msg.Body != null) {
				foreach (DType dtype in msg.Signature.Data) {
					object arg;
					Message.GetValue (msg.Body, dtype, out arg);
					vals.Add (arg);
				}
			}

			return vals.ToArray ();
		}

		//FIXME: this shouldn't be part of the core API
		//that also applies to much of the other object mapping code
		//it should cache proxies and objects, really

		//inspired by System.Activator
		public object GetObject (Type type, string bus_name, ObjectPath object_path)
		{
			DProxy prox = new DProxy (this, bus_name, object_path, type);
			return prox.GetTransparentProxy ();
		}

		public T GetObject<T> (string bus_name, ObjectPath object_path)
		{
			return (T)GetObject (typeof (T), bus_name, object_path);
		}

		//see also:
		//from System.Runtime.Remoting.RemotingServices:
		//public static object Connect (Type classToProxy, string url);

		//this may be silly API, but better than raw access to the dictionary:
		//inspired by System.Runtime.Remoting.RemotingServices
		//public ObjRef Marshal (MarshalByRefObject obj, string uri)
		public void Marshal (MarshalByRefObject obj, string bus_name)
		{
			RegisteredObjects[bus_name] = obj;
		}

		//just in case the MarshalByRefObject requirement is crack
		public void Marshal (object obj, string bus_name)
		{
			RegisteredObjects[bus_name] = obj;
		}
	}
}

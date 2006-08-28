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
		//unix:path=/var/run/dbus/system_bus_socket
		const string SYSTEM_BUS = "/var/run/dbus/system_bus_socket";

		public Socket sock = null;
		Stream ns = null;
		Transport transport;

		public Connection ()
		{
			string sessAddr = System.Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");
			//string sysAddr = System.Environment.GetEnvironmentVariable ("DBUS_SYSTEM_BUS_ADDRESS");
			bool abstr;
			string path;

			Address.Parse (sessAddr, out abstr, out path);

			//not really correct
			if (path == null)
				path = SYSTEM_BUS;

			transport = new UnixTransport (path, abstr);

			sock = transport.socket;

			sock.Blocking = true;
			//ns = new UnixStream ((int)sock.Handle);
			ns = new NetworkStream (sock);

			Authenticate ();
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
			msg.Serial = GenerateSerial ();

			WriteMessage (msg);

			//Outbound.Enqueue (msg);
			//temporary
			//Flush ();

			return msg.Serial;
		}

		//could be cleaner
		protected void WriteMessage (Message msg)
		{
			ns.Write (msg.HeaderData, 0, msg.HeaderSize);
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

		public unsafe Message ReadMessage ()
		{
			//FIXME: fix reading algorithm to work in one step

			byte[] buf = new byte[1024];

			//ns.Read (buf, 0, buf.Length);
			ns.Read (buf, 0, 16);

			//Console.WriteLine ("");
			//Console.WriteLine ("Header:");

			Message msg = new Message ();

			fixed (byte* pbuf = buf) {
				msg.Header = (DHeader*)pbuf;
				//Console.WriteLine (msg.MessageType);
				//Console.WriteLine ("Length: " + msg.Header->Length);
				//Console.WriteLine ("Header Length: " + msg.Header->HeaderLength);
			}

			int toRead = 0;
			toRead += Message.Padded ((int)msg.Header->HeaderLength, 8);

			//Console.WriteLine ("toRead: " + toRead);

			int read;

			read = ns.Read (buf, 16, toRead);

			if (read != toRead)
				Console.Error.WriteLine ("Read length mismatch: " + read + " of expected " + toRead);

			msg.HeaderData = buf;

			/*
			Console.WriteLine ("Len: " + msg.Header->Length);
			Console.WriteLine ("HLen: " + msg.Header->HeaderLength);
			Console.WriteLine ("toRead: " + toRead);
			*/
			//read the body
			if (msg.Header->Length != 0) {
				//FIXME
				//msg.Body = new byte[(int)msg.Header->Length];
				byte[] body = new byte[(int)msg.Header->Length];

				//int len = ns.Read (msg.Body, 0, msg.Body.Length);
				int len = ns.Read (body, 0, body.Length);

				//if (len != msg.Body.Length)
				if (len != body.Length)
					Console.Error.WriteLine ("Message body size mismatch");

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
				switch (msg.MessageType) {
					case MessageType.Invalid:
						break;
					case MessageType.MethodCall:
						HandleMethodCall (msg);
						break;
					case MessageType.MethodReturn:
					case MessageType.Error:
						if (msg.ReplySerial == id)
							return msg;
						break;
					case MessageType.Signal:
						HandleSignal (msg);
						break;
				}
			}

			return null;
		}


		//temporary convenience method
		public void HandleSignal (Message msg)
		{
			if (Handlers.ContainsKey (msg.Member)) {
				Delegate dlg = Handlers[msg.Member];
				dlg.DynamicInvoke (GetDynamicValues (msg));
			} else {
				Console.Error.WriteLine ("No signal handler for " + msg.Member);
			}
		}

		public Dictionary<string,Delegate> Handlers = new Dictionary<string,Delegate> ();

		//should generalize this method
		//it is duplicated in DProxy
		protected Message ConstructReplyFor (Message req, object[] vals)
		{
			Message replyMsg = new Message ();

			Signature inSig = new Signature ("");

			if (vals != null && vals.Length != 0) {
				replyMsg.Body = new System.IO.MemoryStream ();

				MemoryStream ms = new MemoryStream ();

				foreach (object arg in vals)
				{
					Type type = arg.GetType ();
					DType dtype = Signature.TypeToDType (type);

					ms.WriteByte ((byte)dtype);

					//hacky
					if (type.IsArray) {
						Type elem_type = type.GetElementType ();
						DType elem_dtype = Signature.TypeToDType (elem_type);

						ms.WriteByte ((byte)elem_dtype);
						Message.Write (replyMsg.Body, type, (Array)arg);
					} else {
						Message.Write (replyMsg.Body, dtype, arg);
					}
				}

				inSig.Data = ms.ToArray ();
			}

			if (inSig.Data.Length == 0)
				replyMsg.WriteHeader (new HeaderField (FieldCode.ReplySerial, req.Serial), new HeaderField (FieldCode.Destination, req.Sender));
			else
				replyMsg.WriteHeader (new HeaderField (FieldCode.ReplySerial, req.Serial), new HeaderField (FieldCode.Destination, req.Sender), new HeaderField (FieldCode.Signature, inSig));

			replyMsg.MessageType = MessageType.MethodReturn;
			replyMsg.ReplyExpected = false;

			return replyMsg;
		}

		//not particularly efficient and needs to be generalized
		public void HandleMethodCall (Message msg)
		{
			if (RegisteredObjects.ContainsKey (msg.Interface)) {
				object obj = RegisteredObjects[msg.Interface];
				Type type = obj.GetType ();
				//object retObj = type.InvokeMember (msg.Member, System.Reflection.BindingFlags.InvokeMethod, null, obj, GetDynamicValues (msg));

				//FIXME: breaks for overloaded methods
				System.Reflection.MethodInfo mi = type.GetMethod (msg.Member, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				System.Reflection.ParameterInfo[]  parms = mi.GetParameters ();
				Type[] sig = new Type[parms.Length];
				for (int i = 0 ; i != parms.Length ; i++)
					sig[i] = parms[i].ParameterType;
				object retObj = mi.Invoke (obj, GetDynamicValues (msg, sig));

				if (msg.ReplyExpected) {
					object[] retObjs;

					if (retObj == null) {
						retObjs = new object[0];
					} else {
						retObjs = new object[1];
						retObjs[0] = retObj;
					}

					Message reply = ConstructReplyFor (msg, retObjs);
					Send (reply);
				}
			} else {
				Console.Error.WriteLine ("No method handler for " + msg.Member);
			}
		}

		public Dictionary<string,object> RegisteredObjects = new Dictionary<string,object> ();

		public object[] GetDynamicValues (Message msg, Type[] types)
		{
			List<object> vals = new List<object> ();

			if (msg.Body != null) {
				foreach (Type type in types) {
					object arg;

					DType dtype = Signature.TypeToDType (type);

					//special case arrays for now
					if (type.IsArray) {
						Array argArr;
						Message.GetValue (msg.Body, type, out argArr);
						arg = argArr;
					} else {
						Message.GetValue (msg.Body, dtype, out arg);
					}

					if (type.IsEnum)
						arg = Enum.ToObject (type, arg);

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
					//Console.WriteLine (arg);
					vals.Add (arg);
				}
			}

			return vals.ToArray ();
		}
	}
}

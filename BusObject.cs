// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace DBus
{
	using Protocol;

	public class BusObject
	{
		static Dictionary<object,BusObject> boCache = new Dictionary<object,BusObject>();

		protected Connection conn;
		string bus_name;
		ObjectPath object_path;

		public BusObject ()
		{
		}

		public BusObject (Connection conn, string bus_name, ObjectPath object_path)
		{
			this.conn = conn;
			this.bus_name = bus_name;
			this.object_path = object_path;
		}

		public Connection Connection
		{
			get {
				return conn;
			}
		}

		public string BusName
		{
			get {
				return bus_name;
			}
		}

		public ObjectPath Path
		{
			get {
				return object_path;
			}
		}

		public void ToggleSignal (string iface, string member, Delegate dlg, bool adding)
		{
			MatchRule rule = new MatchRule ();
			rule.MessageType = MessageType.Signal;
			rule.Fields.Add (FieldCode.Interface, new MatchTest (iface));
			rule.Fields.Add (FieldCode.Member, new MatchTest (member));
			rule.Fields.Add (FieldCode.Path, new MatchTest (object_path));
			// FIXME: Cause a regression compared to 0.6 as name wasn't matched before
			// the problem arises because busname is not used by DBus daemon and
			// instead it uses the canonical name of the sender (i.e. similar to ':1.13')
			// rule.Fields.Add (FieldCode.Sender, new MatchTest (bus_name));

			if (adding) {
				if (conn.Handlers.ContainsKey (rule))
					conn.Handlers[rule] = Delegate.Combine (conn.Handlers[rule], dlg);
				else {
					conn.Handlers[rule] = dlg;
					conn.AddMatch (rule.ToString ());
				}
			} else if (conn.Handlers.ContainsKey (rule)) {
				conn.Handlers[rule] = Delegate.Remove (conn.Handlers[rule], dlg);
				if (conn.Handlers[rule] == null) {
					conn.RemoveMatch (rule.ToString ());
					conn.Handlers.Remove (rule);
				}
			}
		}

		public void SendSignal (string iface, string member, string inSigStr, MessageWriter writer, Type retType, out Exception exception)
		{
			exception = null;

			Signature outSig = String.IsNullOrEmpty (inSigStr) ? Signature.Empty : new Signature (inSigStr);

			MessageContainer signal = new MessageContainer {
				Type = MessageType.Signal,
				Path = object_path,
				Interface = iface,
				Member = member,
				Signature = outSig,
			};

			Message signalMsg = signal.Message;
			signalMsg.AttachBodyTo (writer);

			conn.Send (signalMsg);
		}

		public MessageReader SendMethodCall (string iface, string member, string inSigStr, MessageWriter writer, Type retType, out Exception exception)
		{
			if (string.IsNullOrEmpty (bus_name))
				throw new ArgumentNullException ("bus_name");
			if (object_path == null)
				throw new ArgumentNullException ("object_path");

			exception = null;
			Signature inSig = String.IsNullOrEmpty (inSigStr) ? Signature.Empty : new Signature (inSigStr);

			MessageContainer method_call = new MessageContainer {
				Path = object_path,
				Interface = iface,
				Member = member,
				Destination = bus_name,
				Signature = inSig
			};

			Message callMsg = method_call.Message;
			callMsg.AttachBodyTo (writer);

			bool needsReply = true;

			callMsg.ReplyExpected = needsReply;
			callMsg.Signature = inSig;

			if (!needsReply) {
				conn.Send (callMsg);
				return null;
			}

#if PROTO_REPLY_SIGNATURE
			if (needsReply) {
				Signature outSig = Signature.GetSig (retType);
				callMsg.Header[FieldCode.ReplySignature] = outSig;
			}
#endif

			Message retMsg = conn.SendWithReplyAndBlock (callMsg);

			MessageReader retVal = null;

			//handle the reply message
			switch (retMsg.Header.MessageType) {
			case MessageType.MethodReturn:
				retVal = new MessageReader (retMsg);
				break;
			case MessageType.Error:
				MessageContainer error = MessageContainer.FromMessage (retMsg);
				string errMsg = String.Empty;
				if (retMsg.Signature.Value.StartsWith ("s")) {
					MessageReader reader = new MessageReader (retMsg);
					errMsg = reader.ReadString ();
				}
				exception = new Exception (error.ErrorName + ": " + errMsg);
				break;
			default:
				throw new Exception ("Got unexpected message of type " + retMsg.Header.MessageType + " while waiting for a MethodReturn or Error");
			}

			return retVal;
		}

		public void Invoke (MethodBase methodBase, string methodName, object[] inArgs, out object[] outArgs, out object retVal, out Exception exception)
		{
			outArgs = new object[0];
			retVal = null;
			exception = null;

			MethodInfo mi = methodBase as MethodInfo;

			if (mi != null && mi.IsSpecialName && (methodName.StartsWith ("add_") || methodName.StartsWith ("remove_"))) {
				string[] parts = methodName.Split (new char[]{'_'}, 2);
				string ename = parts[1];
				Delegate dlg = (Delegate)inArgs[0];

				ToggleSignal (Mapper.GetInterfaceName (mi), ename, dlg, parts[0] == "add");

				return;
			}

			Type[] inTypes = Mapper.GetTypes (ArgDirection.In, mi.GetParameters ());
			Signature inSig = Signature.GetSig (inTypes);

			string iface = null;
			if (mi != null)
				iface = Mapper.GetInterfaceName (mi);

			if (mi != null && mi.IsSpecialName) {
				methodName = methodName.Replace ("get_", "Get");
				methodName = methodName.Replace ("set_", "Set");
			}

			MessageWriter writer = new MessageWriter (conn);

			if (inArgs != null && inArgs.Length != 0) {
				for (int i = 0 ; i != inTypes.Length ; i++)
					writer.Write (inTypes[i], inArgs[i]);
			}

			MessageReader reader = SendMethodCall (iface, methodName, inSig.Value, writer, mi.ReturnType, out exception);
			if (reader == null)
				return;

			retVal = reader.ReadValue (mi.ReturnType);
		}

		public static object GetObject (Connection conn, string bus_name, ObjectPath object_path, Type declType)
		{
			Type proxyType = TypeImplementer.Root.GetImplementation (declType);

			object instObj = Activator.CreateInstance (proxyType);
			BusObject inst = GetBusObject (instObj);
			inst.conn = conn;
			inst.bus_name = bus_name;
			inst.object_path = object_path;

			return instObj;
		}

		public static BusObject GetBusObject (object instObj)
		{
			if (instObj is BusObject)
				return (BusObject)instObj;

			BusObject inst;
			if (boCache.TryGetValue (instObj, out inst))
				return inst;

			inst = new BusObject ();
			boCache[instObj] = inst;

			return inst;
		}

		public Delegate GetHookupDelegate (EventInfo ei)
		{
			DynamicMethod hookupMethod = TypeImplementer.GetHookupMethod (ei);
			Delegate d = hookupMethod.CreateDelegate (ei.EventHandlerType, this);
			return d;
		}
	}
}

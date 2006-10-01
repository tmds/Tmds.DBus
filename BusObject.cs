// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;

namespace NDesk.DBus
{
	//marked internal because this isn't ready for public use yet
	internal class BusObject
	{
		Connection conn;
		string bus_name;
		ObjectPath object_path;

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
				string matchRule = MessageFilter.CreateMatchRule (MessageType.Signal, object_path, Mapper.GetInterfaceName (mi), ename);

				if (parts[0] == "add") {
					if (conn.Handlers.ContainsKey (matchRule))
						conn.Handlers[matchRule] = Delegate.Combine (conn.Handlers[matchRule], dlg);
					else {
						conn.Handlers[matchRule] = dlg;
						conn.AddMatch (matchRule);
					}
				} else if (parts[0] == "remove") {
					conn.Handlers[matchRule] = Delegate.Remove (conn.Handlers[matchRule], dlg);
					if (conn.Handlers[matchRule] == null) {
						conn.RemoveMatch (matchRule);
						conn.Handlers.Remove (matchRule);
					}
				}
				return;
			}

			Type[] inTypes = Mapper.GetTypes (ArgDirection.In, mi.GetParameters ());
			Signature inSig = Signature.GetSig (inTypes);

			MethodCall method_call;
			Message callMsg;

			//build the outbound method call message
			{
				//this bit is error-prone (no null checking) and will need rewriting when DProxy is replaced
				string iface = null;
				if (mi != null)
					iface = Mapper.GetInterfaceName (mi);

				//map property accessors
				//TODO: this needs to be done properly, not with simple String.Replace
				//note that IsSpecialName is also for event accessors, but we already handled those and returned
				if (mi != null && mi.IsSpecialName) {
					methodName = methodName.Replace ("get_", "Get");
					methodName = methodName.Replace ("set_", "Set");
				}

				//TODO: no need for this conditional and overload
				if (inSig != Signature.Empty)
					method_call = new MethodCall (object_path, iface, methodName, bus_name, inSig);
				else
					method_call = new MethodCall (object_path, iface, methodName, bus_name);

				callMsg = method_call.message;

				if (inArgs != null && inArgs.Length != 0) {
					MessageWriter writer = new MessageWriter ();

					for (int i = 0 ; i != inTypes.Length ; i++)
						writer.Write (inTypes[i], inArgs[i]);

					callMsg.Body = writer.ToArray ();
				}
			}

			//TODO: complete out parameter support
			Type[] outParmTypes = Mapper.GetTypes (ArgDirection.Out, mi.GetParameters ());
			Signature outParmSig = Signature.GetSig (outParmTypes);

			if (outParmSig != Signature.Empty)
				throw new Exception ("Out parameters not yet supported: out_signature='" + outParmSig.Value + "'");

			Type[] outTypes = new Type[1];
			outTypes[0] = mi.ReturnType;

			//we default to always requiring replies for now, even though unnecessary
			//this is to make sure errors are handled synchronously
			//TODO: don't hard code this
			bool needsReply = true;

			//if (mi.ReturnType == typeof (void))
			//	needsReply = false;

			callMsg.ReplyExpected = needsReply;
			callMsg.Signature = inSig;

			if (!needsReply) {
				conn.Send (callMsg);
				return;
			}

#if PROTO_REPLY_SIGNATURE
			if (needsReply) {
				Signature outSig = Signature.GetSig (outTypes);
				callMsg.Header.Fields[FieldCode.ReplySignature] = outSig;
			}
#endif

			Message retMsg = conn.SendWithReplyAndBlock (callMsg);

			//handle the reply message
			switch (retMsg.Header.MessageType) {
				case MessageType.MethodReturn:
				object[] retVals = MessageHelper.GetDynamicValues (retMsg, outTypes);
				if (retVals.Length != 0)
					retVal = retVals[retVals.Length - 1];
				break;
				case MessageType.Error:
				//TODO: typed exceptions
				Error error = new Error (retMsg);
				string errMsg = "";
				if (retMsg.Signature.Value.StartsWith ("s")) {
					MessageReader reader = new MessageReader (retMsg);
					reader.GetValue (out errMsg);
				}
				exception = new Exception (error.ErrorName + ": " + errMsg);
				break;
				default:
				throw new Exception ("Got unexpected message of type " + retMsg.Header.MessageType + " while waiting for a MethodReturn or Error");
			}

			return;
		}
	}
}

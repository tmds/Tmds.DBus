// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;

namespace NDesk.DBus
{
	//marked internal because this is really an implementation detail and needs to be replaced
	internal class DProxy : RealProxy
	{
		Connection conn;

		string bus_name;
		ObjectPath object_path;

		//Dictionary<string,string> methods = new Dictionary<string,string> ();

		public DProxy (Connection conn, string bus_name, ObjectPath object_path, Type type) : base(type)
		{
			this.conn = conn;
			this.bus_name = bus_name;
			this.object_path = object_path;
		}

		public override IMessage Invoke (IMessage msg)
		{
			IMethodCallMessage mcm = (IMethodCallMessage) msg;

			MethodReturnMessageWrapper newRet = new MethodReturnMessageWrapper ((IMethodReturnMessage) msg);

			MethodInfo imi = mcm.MethodBase as MethodInfo;
			string methodName = mcm.MethodName;

			if (imi != null && imi.IsSpecialName) {
				if (methodName.StartsWith ("add_")) {
					string[] parts = mcm.MethodName.Split ('_');
					string ename = parts[1];
					Delegate dlg = (Delegate)mcm.InArgs[0];

					string matchRule = MessageFilter.CreateMatchRule (MessageType.Signal, object_path, Mapper.GetInterfaceName (imi), ename);

					if (conn.Handlers.ContainsKey (matchRule))
						conn.Handlers[matchRule] = Delegate.Combine (conn.Handlers[matchRule], dlg);
					else {
						conn.Handlers[matchRule] = dlg;
						conn.AddMatch (matchRule);
					}

					return (IMethodReturnMessage) newRet;
				}

				if (mcm.MethodName.StartsWith ("remove_")) {
					string[] parts = mcm.MethodName.Split ('_');

					string ename = parts[1];
					string matchRule = MessageFilter.CreateMatchRule (MessageType.Signal, object_path, Mapper.GetInterfaceName (imi), ename);

					Delegate dlg = (Delegate)mcm.InArgs[0];

					conn.Handlers[matchRule] = Delegate.Remove (conn.Handlers[matchRule], dlg);

					if (conn.Handlers[matchRule] == null)
						conn.RemoveMatch (matchRule);

					return (IMethodReturnMessage) newRet;
				}
			}

			Type[] inTypes = Mapper.GetTypes (ArgDirection.In, imi.GetParameters ());
			object[] inValues = mcm.InArgs;
			Signature inSig = Signature.GetSig (inTypes);

			MethodCall method_call;
			Message callMsg;

			//build the outbound method call message
			{
				//this bit is error-prone (no null checking) and will need rewriting when DProxy is replaced
				string iface = null;
				if (imi != null)
					iface = Mapper.GetInterfaceName (imi);

				//map property accessors
				//TODO: this needs to be done properly, not with simple String.Replace
				//note that IsSpecialName is also for event accessors, but we already handled those and returned
				if (imi != null && imi.IsSpecialName) {
					methodName = methodName.Replace ("get_", "Get");
					methodName = methodName.Replace ("set_", "Set");
				}

				//TODO: no need for this conditional and overload
				if (inSig != Signature.Empty)
					method_call = new MethodCall (object_path, iface, methodName, bus_name, inSig);
				else
					method_call = new MethodCall (object_path, iface, methodName, bus_name);

				callMsg = method_call.message;

				if (mcm.InArgs != null && mcm.InArgs.Length != 0) {
					MessageWriter writer = new MessageWriter ();

					for (int i = 0 ; i != inTypes.Length ; i++)
						writer.Write (inTypes[i], inValues[i]);

					callMsg.Body = writer.ToArray ();
				}
			}

			MethodInfo mi = newRet.MethodBase as MethodInfo;

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
				return (IMethodReturnMessage) newRet;
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
					newRet.ReturnValue = retVals[retVals.Length - 1];
				break;
				case MessageType.Error:
				//TODO: typed exceptions
				Error error = new Error (retMsg);
				string errMsg = "";
				if (retMsg.Signature.Value.StartsWith ("s")) {
					MessageReader reader = new MessageReader (retMsg);
					reader.GetValue (out errMsg);
				}
				Exception e = new Exception (error.ErrorName + ": " + errMsg);
				newRet.Exception = e;
				break;
				default:
				throw new Exception ("Got unexpected message of type " + retMsg.Header.MessageType + " while waiting for a MethodReturn or Error");
			}

			return (IMethodReturnMessage) newRet;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotImplementedException ();
		}
		*/

		~DProxy ()
		{
			//FIXME: remove handlers/match rules here
			if (Protocol.Verbose)
				Console.Error.WriteLine ("Warning: Finalization of " + object_path + " not yet supported");
		}
	}
}

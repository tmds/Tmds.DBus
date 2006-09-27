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

					conn.Handlers[ename] = dlg;

					//inelegant
					if (bus_name != "org.freedesktop.DBus" || object_path.Value != "/org/freedesktop/DBus" || ename != "NameAcquired") {
						org.freedesktop.DBus.Bus bus = conn.GetObject<org.freedesktop.DBus.Bus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
						bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Signal, object_path, Connection.GetInterfaceName (imi), ename));
						conn.Iterate ();
					}

					return (IMethodReturnMessage) newRet;
				}

				if (mcm.MethodName.StartsWith ("remove_")) {
					string[] parts = mcm.MethodName.Split ('_');

					string ename = parts[1];
					//Delegate dlg = (Delegate)mcm.InArgs[0];

					//FIXME: this removes the match rule even when we still have delegates connected to the event!
					//inelegant
					if (bus_name != "org.freedesktop.DBus" || object_path.Value != "/org/freedesktop/DBus" || ename != "NameAcquired") {
						org.freedesktop.DBus.Bus bus = conn.GetObject<org.freedesktop.DBus.Bus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
						bus.RemoveMatch (MessageFilter.CreateMatchRule (MessageType.Signal, object_path, Connection.GetInterfaceName (imi), ename));
						conn.Iterate ();
					}

					conn.Handlers.Remove (ename);

					return (IMethodReturnMessage) newRet;
				}
			}

			Type[] inTypes = GetTypes (ArgDirection.In, imi.GetParameters ());
			object[] inValues = mcm.InArgs;
			Signature inSig = Signature.GetSig (inTypes);

			MethodCall method_call;
			Message callMsg;

			//build the outbound method call message
			{
				//this bit is error-prone (no null checking) and will need rewriting when DProxy is replaced
				string iface = null;
				if (imi != null)
					iface = Connection.GetInterfaceName (imi);

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
					MessageWriter writer = new MessageWriter (EndianFlag.Little);

					for (int i = 0 ; i != inTypes.Length ; i++)
						writer.Write (inTypes[i], inValues[i]);

					callMsg.Body = writer.ToArray ();
				}
			}

			bool needsReply = true;

			MethodInfo mi = newRet.MethodBase as MethodInfo;

			//TODO: complete out parameter support
			Type[] outParmTypes = GetTypes (ArgDirection.Out, mi.GetParameters ());
			Signature outParmSig = Signature.GetSig (outParmTypes);

			if (outParmSig != Signature.Empty)
				throw new Exception ("Out parameters not yet supported: out_signature='" + outParmSig.Value + "'");

			if (mi.ReturnType == typeof (void))
				needsReply = false;

			callMsg.ReplyExpected = needsReply;
			callMsg.Signature = inSig;

			if (!needsReply) {
				conn.Send (callMsg);
				return (IMethodReturnMessage) newRet;
			}

			Type[] outTypes = new Type[1];
			outTypes[0] = mi.ReturnType;

#if PROTO_REPLY_SIGNATURE
			if (needsReply) {
				Signature outSig = Signature.GetSig (outTypes);
				callMsg.Header.Fields[FieldCode.ReplySignature] = outSig;
			}
#endif

			Message retMsg = conn.SendWithReplyAndBlock (callMsg);

			//handle the reply message
			if (retMsg.Header.MessageType == MessageType.Error) {
				//TODO: typed exceptions
				Error error = new Error (retMsg);
				string errMsg = "";
				if (retMsg.Signature.Value.StartsWith ("s")) {
					MessageReader reader = new MessageReader (retMsg);
					reader.GetValue (out errMsg);
				}
				Exception e = new Exception (error.ErrorName + ": " + errMsg);
				newRet.Exception = e;
			} else if (retMsg.Header.MessageType == MessageType.MethodReturn) {
				newRet.ReturnValue = conn.GetDynamicValues (retMsg, outTypes)[0];
			} else {
				//should not be possible to reach this at present
				throw new Exception ("Got unexpected message of type " + retMsg.Header.MessageType + " while waiting for a MethodReturn");
			}

			return (IMethodReturnMessage) newRet;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotImplementedException ();
		}
		*/

		public static Type[] GetTypes (ArgDirection dir, ParameterInfo[] parms)
		{
			List<Type> types = new List<Type> ();

			//TODO: consider InOut/Ref

			for (int i = 0 ; i != parms.Length ; i++) {
				switch (dir) {
					case ArgDirection.In:
						//docs say IsIn isn't reliable, and this is indeed true
						//if (parms[i].IsIn)
						if (!parms[i].IsOut)
							types.Add (parms[i].ParameterType);
						break;
					case ArgDirection.Out:
						if (parms[i].IsOut) {
							//TODO: note that IsOut is optional to the compiler, we may want to use IsByRef instead
						//eg: if (parms[i].ParameterType.IsByRef)
							types.Add (parms[i].ParameterType.GetElementType ());
						}
						break;
				}
			}

			return types.ToArray ();
		}
	}

	[AttributeUsage (AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
	public class InterfaceAttribute : Attribute
	{
		public string Name;

		public InterfaceAttribute (string name)
		{
			this.Name = name;
		}
	}

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple=false, Inherited=true)]
	public class ArgumentAttribute : Attribute
	{
		public string Name;

		public ArgumentAttribute (string name)
		{
			this.Name = name;
		}
	}
}

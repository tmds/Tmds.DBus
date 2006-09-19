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

					//TODO: make the match rule more specific, and cache the DBus object somewhere sensible
					if (bus_name != "org.freedesktop.DBus") {
						org.freedesktop.DBus.Bus bus = conn.GetObject<org.freedesktop.DBus.Bus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
						bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Signal, bus_name, ename));
						conn.Iterate ();
					}

					return (IMethodReturnMessage) newRet;
				}

				if (mcm.MethodName.StartsWith ("remove_")) {
					string[] parts = mcm.MethodName.Split ('_');

					string ename = parts[1];
					//Delegate dlg = (Delegate)mcm.InArgs[0];

					//TODO: make the match rule more specific, and cache the DBus object somewhere sensible
					if (bus_name != "org.freedesktop.DBus") {
						org.freedesktop.DBus.Bus bus = conn.GetObject<org.freedesktop.DBus.Bus> ("org.freedesktop.DBus", new ObjectPath ("/org/freedesktop/DBus"));
						bus.RemoveMatch (MessageFilter.CreateMatchRule (MessageType.Signal, bus_name, ename));
						conn.Iterate ();
					}

					conn.Handlers.Remove (ename);

					return (IMethodReturnMessage) newRet;
				}
			}

			Signature inSig = Signature.GetSig (mcm.InArgs);

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

				/*
				callMsg.Header.Fields[FieldCode.Path] = object_path;
				callMsg.Header.Fields[FieldCode.Interface] = iface;
				callMsg.Header.Fields[FieldCode.Member] = methodName;
				callMsg.Header.Fields[FieldCode.Destination] = bus_name;
				if (inSig.Data.Length != 0)
					callMsg.Header.Fields[FieldCode.Signature] = inSig;
				*/

				//callMsg.WriteHeader ();

				/*
				if (inSig.Data.Length == 0)
				{
					callMsg.WriteHeader (HeaderField.Create (FieldCode.Path, object_path), HeaderField.Create (FieldCode.Interface, iface), HeaderField.Create (FieldCode.Member, methodName), HeaderField.Create (FieldCode.Destination, bus_name));
				} else {
					callMsg.WriteHeader (HeaderField.Create (FieldCode.Path, object_path), HeaderField.Create (FieldCode.Interface, iface), HeaderField.Create (FieldCode.Member, methodName), HeaderField.Create (FieldCode.Destination, bus_name), HeaderField.Create (FieldCode.Signature, inSig));
				}
				*/

				if (inSig.Data.Length != 0)
					method_call = new MethodCall (object_path, iface, methodName, bus_name, inSig);
				else
					method_call = new MethodCall (object_path, iface, methodName, bus_name);

				callMsg = method_call.message;

				if (mcm.InArgs != null && mcm.InArgs.Length != 0) {
						callMsg.Body = new System.IO.MemoryStream ();

						foreach (object arg in mcm.InArgs)
							Message.Write (callMsg.Body, arg.GetType (), arg);
					}
			}

			bool needsReply = true;

			MethodInfo mi = newRet.MethodBase as MethodInfo;

			//TODO: complete out parameter support
			Signature oSig = Signature.GetSig (ArgDirection.Out, mi.GetParameters ());
			if (oSig.Data.Length != 0)
				throw new Exception ("Out parameters not yet supported: out_signature='" + oSig.Value + "'");

			if (mi.ReturnType == typeof (void))
				needsReply = false;

			callMsg.ReplyExpected = needsReply;

			//signature helper is broken, so write it by hand
			callMsg.Header.Fields[FieldCode.Path] = object_path;
			//callMsg.Header.Fields[FieldCode.Interface] = iface;
			callMsg.Header.Fields[FieldCode.Member] = methodName;
			callMsg.Header.Fields[FieldCode.Destination] = bus_name;
			if (inSig.Data.Length != 0)
				callMsg.Header.Fields[FieldCode.Signature] = inSig;

			if (!needsReply) {
				conn.Send (callMsg);
				return (IMethodReturnMessage) newRet;
			}

			Type[] retTypeArr = new Type[1];
			retTypeArr[0] = mi.ReturnType;

#if PROTO_REPLY_SIGNATURE
			Signature outSig = Signature.GetSig (retTypeArr);
			callMsg.Header.Fields[FieldCode.ReplySignature] = outSig;
#endif

			Message retMsg = conn.SendWithReplyAndBlock (callMsg);

			//handle the reply message
			if (retMsg.Header.MessageType == MessageType.Error) {
				//TODO: typed exceptions
				Error error = new Error (retMsg);
				string errMsg = "";
				if (retMsg.Signature.Value.StartsWith ("s"))
					Message.GetValue (retMsg.Body, out errMsg);
				Exception e = new Exception (error.ErrorName + ": " + errMsg);
				newRet.Exception = e;
			} else if (retMsg.Header.MessageType == MessageType.MethodReturn) {
				newRet.ReturnValue = conn.GetDynamicValues (retMsg, retTypeArr)[0];
			} else {
				//should not be possible to reach this at present
				throw new Exception ("Got unexpected message of type " + retMsg.Header.MessageType + " while waiting for a MethodReturn");
			}

			return (IMethodReturnMessage) newRet;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotSupportedException ();
		}
		*/
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

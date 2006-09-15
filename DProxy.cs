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
	public class DProxy : RealProxy
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

			//messy and only relevant to imported objects, but works
			//note that the foreach is useless since there can be only key
			//probably does not deal with class inheritance etc.

			foreach (InterfaceAttribute ia in type.GetCustomAttributes (typeof (InterfaceAttribute), false))
				conn.RegisteredTypes[type] = ia.Name;

			foreach (Type t in type.GetInterfaces ())
				foreach (InterfaceAttribute ia in t.GetCustomAttributes (typeof (InterfaceAttribute), false))
					conn.RegisteredTypes[t] = ia.Name;

			/*
			methods["Hello"] = "";
			methods["ListNames"] = "";
			methods["NameHasOwner"] = "s";
			*/
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

			Signature inSig = GetSig (mcm.InArgs);

			MethodCall method_call;
			Message callMsg;

			//build the outbound method call message
			{
				string iface = null;
				//if the type is registered, use that, otherwise use legacy iface string
				if (imi != null && conn.RegisteredTypes.ContainsKey (imi.DeclaringType))
					iface = conn.RegisteredTypes[imi.DeclaringType];

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
			Signature oSig = GetSig (ArgDirection.Out, mi.GetParameters ());
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
			Signature outSig = GetSig (retTypeArr);
			callMsg.Header.Fields[FieldCode.ReplySignature] = outSig;
#endif

			Message retMsg = conn.SendWithReplyAndBlock (callMsg);

			//handle the reply message
			newRet.ReturnValue = conn.GetDynamicValues (retMsg, retTypeArr)[0];

			return (IMethodReturnMessage) newRet;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotSupportedException ();
		}
		*/

		public static Signature GetSig (ArgDirection dir, ParameterInfo[] parms)
		{
			List<Type> types = new List<Type> ();

			//TODO: consider InOut/Ref

			for (int i = 0 ; i != parms.Length ; i++) {
				switch (dir) {
					case ArgDirection.In:
						if (parms[i].IsIn)
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

			return GetSig (types.ToArray ());
		}

		public static Signature GetSig (object[] objs)
		{
			return GetSig (Type.GetTypeArray (objs));
		}

		public static Signature GetSig (Type[] types)
		{
			MemoryStream ms = new MemoryStream ();

			foreach (Type type in types) {
				{
					byte[] data = GetSig (type).Data;
					ms.Write (data, 0, data.Length);
				}
			}

			Signature sig;
			sig.Data = ms.ToArray ();
			return sig;
		}

		public static Signature GetSig (Type type)
		{
			MemoryStream ms = new MemoryStream ();

			if (type.IsArray) {
				ms.WriteByte ((byte)DType.Array);

				Type elem_type = type.GetElementType ();
				//TODO: recurse
				//DType elem_dtype = Signature.TypeToDType (elem_type);
				//ms.WriteByte ((byte)elem_dtype);
				{
					byte[] data = GetSig (elem_type).Data;
					ms.Write (data, 0, data.Length);
				}
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();

				ms.WriteByte ((byte)'a');
				ms.WriteByte ((byte)'{');

				{
					byte[] data = GetSig (genArgs[0]).Data;
					ms.Write (data, 0, data.Length);
				}

				{
					byte[] data = GetSig (genArgs[1]).Data;
					ms.Write (data, 0, data.Length);
				}

				ms.WriteByte ((byte)'}');
			} else if (!type.IsPrimitive && type.IsValueType && !type.IsEnum) {
				//if (type.IsGenericParameter && type.GetGenericTypeDefinition () == typeof (KeyValuePair<,>))
				if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (KeyValuePair<,>))
					ms.WriteByte ((byte)'{');
				else
					ms.WriteByte ((byte)'(');

				ConstructorInfo[] cis = type.GetConstructors ();
				if (cis.Length != 0) {
					System.Reflection.ConstructorInfo ci = cis[0];
					System.Reflection.ParameterInfo[]  parms = ci.GetParameters ();

					foreach (ParameterInfo parm in parms) {
						{
							byte[] data = GetSig (parm.ParameterType).Data;
							ms.Write (data, 0, data.Length);
						}
					}

				} else {
					foreach (FieldInfo fi in type.GetFields ()) {
						{
							byte[] data = GetSig (fi.FieldType).Data;
							ms.Write (data, 0, data.Length);
						}
					}
				}
				if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (KeyValuePair<,>))
					ms.WriteByte ((byte)'}');
				else
					ms.WriteByte ((byte)')');
			} else {
				DType dtype = Signature.TypeToDType (type);
				ms.WriteByte ((byte)dtype);
			}

			Signature sig;
			sig.Data = ms.ToArray ();
			return sig;
		}
	}

	public enum ArgDirection
	{
		In,
		Out,
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
}

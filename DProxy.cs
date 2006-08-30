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
		ObjectPath opath;
		string iface;
		string dest;

		//Dictionary<string,string> methods = new Dictionary<string,string> ();

		public DProxy (Connection conn, ObjectPath opath, string dest, Type type) : this(conn, opath, dest, dest, type)
		{
		}

		public DProxy (Connection conn, ObjectPath opath, string iface, string dest, Type type) : base(type)
		{
			this.conn = conn;
			this.opath = opath;
			this.iface = iface;
			this.dest = dest;

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

			//Console.WriteLine (mcm.MethodName);

			//if (!methods.ContainsKey (mcm.MethodName))
			//	return null;

			//foreach (object myObj in mcm.InArgs)
			//	Console.WriteLine("arg value: " + myObj.ToString());

			if (mcm.MethodName.StartsWith ("add_")) {
				string[] parts = mcm.MethodName.Split ('_');
				string ename = parts[1];
				Delegate dlg = (Delegate)mcm.InArgs[0];

				conn.Handlers[ename] = dlg;

				return (IMethodReturnMessage) newRet;
			}

			if (mcm.MethodName.StartsWith ("remove_")) {
				string[] parts = mcm.MethodName.Split ('_');
				string ename = parts[1];
				Delegate dlg = (Delegate)mcm.InArgs[0];

				conn.Handlers.Remove (ename);

				return (IMethodReturnMessage) newRet;
			}

			Message callMsg = new Message ();

			//build the outbound method call message
			{

				Signature inSig = new Signature ("");

				if (mcm.InArgs != null && mcm.InArgs.Length != 0) {
					callMsg.Body = new System.IO.MemoryStream ();

					foreach (object arg in mcm.InArgs)
						Message.Write (callMsg.Body, arg.GetType (), arg);

					inSig = GetSig (mcm.InArgs);
				}

				//if the type is registered, use that, otherwise use legacy iface string
				MethodInfo imi = mcm.MethodBase as MethodInfo;
				if (imi != null && conn.RegisteredTypes.ContainsKey (imi.DeclaringType))
					iface = conn.RegisteredTypes[imi.DeclaringType];

				if (inSig.Data.Length == 0)
					callMsg.WriteHeader (new HeaderField (FieldCode.Path, opath), new HeaderField (FieldCode.Interface, iface), new HeaderField (FieldCode.Member, mcm.MethodName), new HeaderField (FieldCode.Destination, dest));
				else
					callMsg.WriteHeader (new HeaderField (FieldCode.Path, opath), new HeaderField (FieldCode.Interface, iface), new HeaderField (FieldCode.Member, mcm.MethodName), new HeaderField (FieldCode.Destination, dest), new HeaderField (FieldCode.Signature, inSig));
			}

			bool needsReply = true;

			MethodInfo mi = newRet.MethodBase as MethodInfo;
			if (mi.ReturnType == typeof (void))
				needsReply = false;

			if (!needsReply) {
				conn.Send (callMsg);
				return (IMethodReturnMessage) newRet;
			}

			Message retMsg = conn.SendWithReplyAndBlock (callMsg);

			//handle the reply message
			{
				Type[] retTypeArr = new Type[1];
				retTypeArr[0] = mi.ReturnType;
				newRet.ReturnValue = conn.GetDynamicValues (retMsg, retTypeArr)[0];
			}

			return (IMethodReturnMessage) newRet;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotSupportedException ();
		}
		*/

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
}

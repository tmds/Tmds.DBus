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

					MemoryStream ms = new MemoryStream ();

					//for (int i = 0 ; i != mcm.InArgs.Length ; i++)
					foreach (object arg in mcm.InArgs)
					{
						//Console.Error.WriteLine ("INarg: ." + arg + ".");
						Type type = arg.GetType ();
						DType dtype = Signature.TypeToDType (type);

						ms.WriteByte ((byte)dtype);

						//hacky
						if (type.IsArray) {
							Type elem_type = type.GetElementType ();
							DType elem_dtype = Signature.TypeToDType (elem_type);

							ms.WriteByte ((byte)elem_dtype);
							Message.Write (callMsg.Body, type, (Array)arg);
						} else {
							Message.Write (callMsg.Body, dtype, arg);
						}
					}

					inSig.Data = ms.ToArray ();
				}

				//Signature outSig = new Signature ("");
				//Console.Error.WriteLine ("INSIG: ." + inSig.Value + ".");

				if (inSig.Data.Length == 0)
					callMsg.WriteHeader (opath, iface, mcm.MethodName, dest);
				else
					callMsg.WriteHeader (opath, iface, mcm.MethodName, dest, inSig);
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

			/*
			{
				Signature outSig = retMsg.Signature;

				//Console.Error.WriteLine ("out: " + mcm.MethodName);
				//Console.Error.WriteLine ("outSig: " + outSig.Value);

				if (outSig.Data.Length == 0)
					return (IMethodReturnMessage) newRet;

				//special case array of string for now
				//if (outSig.Value == "as")
				if (outSig.Data[0] == (byte)'a' && outSig.Data[1] == (byte)'s')
				{
					string[] arg;

					Message.GetValue (retMsg.Body, out arg);

					newRet.ReturnValue = arg;
				}
				else
				{
					object arg;

					DType dtype = (DType)outSig.Data[0];
					Message.GetValue (retMsg.Body, dtype, out arg);

					newRet.ReturnValue = arg;
				}
			}
			*/

			//Console.Error.WriteLine ("INVOKE: " + mcm.MethodName);

			return (IMethodReturnMessage) newRet;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotSupportedException ();
		}
		*/
	}
}

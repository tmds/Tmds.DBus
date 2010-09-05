// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

#if DLR

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

//using System.Dynamic;
//using Microsoft.Scripting.Utils;
//using Microsoft.Scripting.Runtime;

using org.freedesktop.DBus;

namespace DBus
{
	internal class DynamicExportObject : ExportObject
	{
		//public readonly object obj;

		ObjectOperations ops;
		public DynamicExportObject (Connection conn, ObjectPath object_path, object obj) : base (conn, object_path, obj)
		{
			//this.obj = obj;
			ScriptRuntime runtime = ScriptRuntime.CreateFromConfiguration ();
			ops = runtime.CreateOperations ();
		}

		bool isRegistered = false;
		public override bool Registered
		{
			get {
				return isRegistered;
			}
			set {
				isRegistered = value;
			}
		}

		internal override void WriteIntrospect (Introspector intro)
		{
		}

		public override void HandleMethodCall (MethodCall method_call)
		{
			//object retVal = obj.GetType ().InvokeMember (method_call.Member, BindingFlags.InvokeMethod, null, obj, new object[0]);
			//IDynamicMetaObjectProvider idyn = obj as IDynamicMetaObjectProvider;

			object retVal = null;

			Exception raisedException = null;
			try {
				object[] args = MessageHelper.GetDynamicValues (method_call.message);
				retVal = ops.InvokeMember (obj, method_call.Member, args);
				//retVal = ops.Call (ops.GetMember (obj, method_call.Member), args);
			} catch (Exception e) {
				raisedException = e;
			}

			if (!method_call.message.ReplyExpected)
				return;

			Message msg = method_call.message;
			Message replyMsg = null;

			if (raisedException == null) {
				MethodReturn method_return = new MethodReturn (msg.Header.Serial);
				replyMsg = method_return.message;
				if (retVal != null) {
					if (retVal.GetType ().FullName == "IronRuby.Builtins.MutableString")
						retVal = retVal.ToString ();
					// TODO: Invalid sig handling
					Signature outSig = Signature.GetSig (retVal.GetType ());
					MessageWriter retWriter = new MessageWriter ();
					retWriter.Write (retVal.GetType (), retVal);
					//retWriter.WriteValueType (retVal, retVal.GetType ());
					replyMsg.Body = retWriter.ToArray ();
					replyMsg.Signature = outSig;
				}
			} else {
				Error error = method_call.CreateError (Mapper.GetInterfaceName (raisedException.GetType ()), raisedException.Message);
				replyMsg = error.message;
			}

			if (method_call.Sender != null)
				replyMsg.Header[FieldCode.Destination] = method_call.Sender;

			conn.Send (replyMsg);
		}
	}
}

#endif
// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NDesk.DBus
{
	//marked internal because this isn't ready for public use yet
	//it probably needs to be made into a base and import/export subclasses
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

		public Delegate GetHookupDelegate (EventInfo ei)
		{
			if (ei.EventHandlerType.IsAssignableFrom (typeof (System.EventHandler)))
				Console.Error.WriteLine ("Warning: Cannot yet fully marshal EventHandler and its subclasses: " + ei.EventHandlerType);

			MethodInfo invokeMethod = ei.EventHandlerType.GetMethod ("Invoke");

			DynamicMethod hookupMethod = GetHookupMethod (invokeMethod, Mapper.GetInterfaceName (ei), ei.Name);

			Delegate d = hookupMethod.CreateDelegate (ei.EventHandlerType, this);

			return d;
		}

		public DynamicMethod GetHookupMethod (MethodInfo invokeMethod, string @interface, string member)
		{
			ParameterInfo[] delegateParms = invokeMethod.GetParameters ();
			Type[] hookupParms = new Type[delegateParms.Length+1];
			hookupParms[0] = typeof (Connection);
			for (int i = 0; i < delegateParms.Length ; i++)
				hookupParms[i+1] = delegateParms[i].ParameterType;

			DynamicMethod hookupMethod = new DynamicMethod ("Handle" + member, typeof (void), hookupParms, typeof (object));
			ILGenerator ilg = hookupMethod.GetILGenerator ();

			//the BusObject instance
			ilg.Emit (OpCodes.Ldarg_0);

			//MethodInfo
			ilg.Emit (OpCodes.Ldtoken, invokeMethod);
			ilg.Emit (OpCodes.Call, typeof (MethodBase).GetMethod ("GetMethodFromHandle"));

			//interface
			ilg.Emit (OpCodes.Ldstr, @interface);

			//member
			ilg.Emit (OpCodes.Ldstr, member);

			LocalBuilder local = ilg.DeclareLocal (typeof (object[]));
			ilg.Emit (OpCodes.Ldc_I4, hookupParms.Length - 1);
			ilg.Emit (OpCodes.Newarr, typeof (object));
			ilg.Emit (OpCodes.Stloc, local);

			//offset by one because arg0 is the instance of the delegate
			for (int i = 1 ; i < hookupParms.Length ; i++)
			{
				Type t = hookupParms[i];

				ilg.Emit (OpCodes.Ldloc, local);
				//the delegate instance offset requires a -1 here
				ilg.Emit (OpCodes.Ldc_I4, i-1);
				ilg.Emit (OpCodes.Ldarg, i);
				if (t.IsValueType)
					ilg.Emit (OpCodes.Box, t);
				ilg.Emit (OpCodes.Stelem_Ref);
			}

			ilg.Emit (OpCodes.Ldloc, local);
			ilg.Emit (OpCodes.Call, typeof (BusObject).GetMethod ("InvokeSignal"));

			//void return
			ilg.Emit (OpCodes.Ret);

			return hookupMethod;
		}

		public void InvokeSignal (MethodInfo mi, string @interface, string member, object[] outValues)
		{
			//TODO: make use of bus_name?

			Type[] outTypes = Mapper.GetTypes (ArgDirection.In, mi.GetParameters ());
			Signature outSig = Signature.GetSig (outTypes);

			Signal signal = new Signal (object_path, @interface, member);
			signal.message.Signature = outSig;

			if (outValues != null && outValues.Length != 0) {
				MessageWriter writer = new MessageWriter ();

				for (int i = 0 ; i != outTypes.Length ; i++)
					writer.Write (outTypes[i], outValues[i]);

				signal.message.Body = writer.ToArray ();
			}

			conn.Send (signal.message);
		}
	}
}

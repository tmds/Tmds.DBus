// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace NDesk.DBus
{
	using Introspection;

	//FIXME: debug hack
	public delegate void VoidHandler ();

	class BusObject
	{
		protected Connection conn;
		string bus_name;
		ObjectPath object_path;

		//protected BusObject ()
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

		//convenience method
		public object InvokeMethod (MethodInfo methodInfo, params object[] inArgs)
		{
			//TODO: this ignores outArgs, doesn't wrap the exception etc.

			object[] outArgs;
			object retVal;
			Exception exception;

			Invoke (methodInfo, methodInfo.Name, inArgs, out outArgs, out retVal, out exception);

			if (exception != null)
				throw exception;

			return retVal;
		}

		//this method is kept simple while IL generation support is worked on
		public object SendMethodCall (MethodInfo methodInfo, string @interface, string member, object[] inArgs, out Exception exception)
		{
			//TODO: don't ignore retVal, exception etc.

			object[] outArgs;
			object retVal;
			Invoke (methodInfo, methodInfo.Name, inArgs, out outArgs, out retVal, out exception);

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

				MatchRule rule = new MatchRule ();
				rule.MessageType = MessageType.Signal;
				rule.Interface = Mapper.GetInterfaceName (mi);
				rule.Member = ename;
				rule.Path = object_path;

				if (parts[0] == "add") {
					if (conn.Handlers.ContainsKey (rule))
						conn.Handlers[rule] = Delegate.Combine (conn.Handlers[rule], dlg);
					else {
						conn.Handlers[rule] = dlg;
						conn.AddMatch (rule.ToString ());
					}
				} else if (parts[0] == "remove") {
					conn.Handlers[rule] = Delegate.Remove (conn.Handlers[rule], dlg);
					if (conn.Handlers[rule] == null) {
						conn.RemoveMatch (rule.ToString ());
						conn.Handlers.Remove (rule);
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

				method_call = new MethodCall (object_path, iface, methodName, bus_name, inSig);

				callMsg = method_call.message;

				if (inArgs != null && inArgs.Length != 0) {
					MessageWriter writer = new MessageWriter (Connection.NativeEndianness);
					writer.connection = conn;

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
				string errMsg = String.Empty;
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

		static AssemblyBuilder asmB;
		static ModuleBuilder modB;

		static AssemblyBuilder asmBdef;
		static ModuleBuilder modBdef;

		static void InitHack ()
		{
			if (asmB != null)
				return;

			asmB = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("ProxyAssembly"), AssemblyBuilderAccess.Run);
			modB = asmB.DefineDynamicModule ("ProxyModule");

			asmBdef = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("Defs"), AssemblyBuilderAccess.RunAndSave);
			//asmBdef = System.Threading.Thread.GetDomain ().DefineDynamicAssembly (new AssemblyName ("DefAssembly"), AssemblyBuilderAccess.RunAndSave);
			modBdef = asmBdef.DefineDynamicModule ("Defs.dll", "Defs.dll");
		}

		static System.Collections.Generic.Dictionary<Type,Type> map = new System.Collections.Generic.Dictionary<Type,Type> ();

		public static Type DefineType (Type declType)
		{
			if (map.ContainsKey (declType))
				return map[declType];

			InitHack ();

			TypeBuilder typeB = modB.DefineType (declType.Name + "Proxy", TypeAttributes.Class | TypeAttributes.Public, typeof (BusObject));

			Implement (typeB, declType);

			foreach (Type iface in declType.GetInterfaces ())
				Implement (typeB, iface);

			Type retT = typeB.CreateType ();

			map[declType] = retT;
			//return typeB.CreateType ();
			return retT;
		}

		static uint ifaceId = 0;
		public static Type Define (Interface[] ifaces)
		{
			InitHack ();

			//Provide a unique interface name
			//This is a bit ugly
			string ifaceName = "Aggregate" + (ifaceId++);

			TypeBuilder typeB = modB.DefineType (ifaceName, TypeAttributes.Public | TypeAttributes.Interface);
			foreach (Interface iface in ifaces)
				typeB.AddInterfaceImplementation (Define (iface));

			return typeB.CreateType ();
		}

		public static Type Define (Interface iface)
		{
			InitHack ();

			int lastDotPos = iface.Name.LastIndexOf ('.');
			string nsName = iface.Name.Substring (0, lastDotPos);
			string ifaceName = iface.Name.Substring (lastDotPos+1);

			nsName = nsName.Replace ('.', Type.Delimiter);

			//using the full interface name is ok, but makes consuming the type from C# difficult since namespaces/Type names may overlap
			TypeBuilder typeB = modBdef.DefineType (nsName + Type.Delimiter + "I" + ifaceName, TypeAttributes.Public | TypeAttributes.Interface);
			Define (typeB, iface);

			return typeB.CreateType ();
		}

		public static void Save ()
		{
			asmBdef.Save ("Defs.dll");
		}

		public static void Define (TypeBuilder typeB, Interface iface)
		{
			foreach (Method declMethod in iface.Methods) {

				//MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, declMethod.ReturnType, Mapper.GetTypes (ArgDirection.In, declMethod.GetParameters ()));

				List<Type> parms = new List<Type> ();

				if (declMethod.Arguments != null)
					foreach (Argument arg in declMethod.Arguments) {
						if (arg.Direction == Introspection.ArgDirection.@in)
							parms.Add (new Signature (arg.Type).ToType ());
						//if (arg.Direction == Introspection.ArgDirection.@out)
						//	parms.Add (new Signature (arg.Type).ToType ().MakeByRefType ());
					}

				Signature outSig = Signature.Empty;
				//this just takes the last out arg and uses is as the return type
				if (declMethod.Arguments != null)
					foreach (Argument arg in declMethod.Arguments)
						if (arg.Direction == Introspection.ArgDirection.@out)
							outSig = new Signature (arg.Type);

				Type retType = outSig == Signature.Empty ? null : outSig.ToType ();

				MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, retType, parms.ToArray ());

				//define the parameter attributes and names
				if (declMethod.Arguments != null) {
					int argNum = 0;

					foreach (Argument arg in declMethod.Arguments) {
						if (arg.Direction == Introspection.ArgDirection.@in)
							method_builder.DefineParameter (++argNum, ParameterAttributes.In, arg.Name);
						//if (arg.Direction == Introspection.ArgDirection.@out)
						//	method_builder.DefineParameter (++argNum, ParameterAttributes.Out, arg.Name);
					}
				}
			}

			if (iface.Signals != null)
			foreach (NDesk.DBus.Introspection.Signal signal in iface.Signals) {
				//Type eventType = typeof (EventHandler);
				Type eventType = typeof (VoidHandler);

				EventBuilder event_builder = typeB.DefineEvent (signal.Name, EventAttributes.None, eventType);

				event_builder.SetAddOnMethod (typeB.DefineMethod ("add_" + signal.Name, MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, null, new Type[] {eventType}));

				event_builder.SetRemoveOnMethod (typeB.DefineMethod ("remove_" + signal.Name, MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, null, new Type[] {eventType}));
			}

			//apply InterfaceAttribute
			ConstructorInfo interfaceAttributeCtor = typeof (InterfaceAttribute).GetConstructor(new Type[] {typeof (string)});

			CustomAttributeBuilder cab = new CustomAttributeBuilder (interfaceAttributeCtor, new object[] {iface.Name});

			typeB.SetCustomAttribute (cab);
		}

		public static void Implement (TypeBuilder typeB, Type iface)
		{
			typeB.AddInterfaceImplementation (iface);

			foreach (MethodInfo declMethod in iface.GetMethods ()) {

				MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, declMethod.ReturnType, Mapper.GetTypes (ArgDirection.In, declMethod.GetParameters ()));
				ILGenerator ilg = method_builder.GetILGenerator ();

				//Mapper.GetTypes (ArgDirection.In, declMethod.GetParameters ())

				ParameterInfo[] delegateParms = declMethod.GetParameters ();
				Type[] hookupParms = new Type[delegateParms.Length+1];
				hookupParms[0] = typeof (BusObject);
				for (int i = 0; i < delegateParms.Length ; i++)
					hookupParms[i+1] = delegateParms[i].ParameterType;

				GenHookupMethod (ilg, declMethod, sendMethodCallMethod, Mapper.GetInterfaceName (iface), declMethod.Name, hookupParms);

				typeB.DefineMethodOverride (method_builder, declMethod);
			}
		}

		public static object GetObject (Connection conn, string bus_name, ObjectPath object_path, Type declType)
		{
			Type proxyType = DefineType (declType);

			BusObject inst = (BusObject)Activator.CreateInstance (proxyType);
			inst.conn = conn;
			inst.bus_name = bus_name;
			inst.object_path = object_path;

			return inst;
		}

		static MethodInfo sendMethodCallMethod = typeof (BusObject).GetMethod ("SendMethodCall");
		static MethodInfo sendSignalMethod = typeof (BusObject).GetMethod ("SendSignal");

		public Delegate GetHookupDelegate (EventInfo ei)
		{
			DynamicMethod hookupMethod = GetHookupMethod (ei);
			Delegate d = hookupMethod.CreateDelegate (ei.EventHandlerType, this);
			return d;
		}

		static Dictionary<EventInfo,DynamicMethod> hookup_methods = new Dictionary<EventInfo,DynamicMethod> ();
		public static DynamicMethod GetHookupMethod (EventInfo ei)
		{
			DynamicMethod hookupMethod;
			if (hookup_methods.TryGetValue (ei, out hookupMethod))
				return hookupMethod;

			if (ei.EventHandlerType.IsAssignableFrom (typeof (System.EventHandler)))
				Console.Error.WriteLine ("Warning: Cannot yet fully expose EventHandler and its subclasses: " + ei.EventHandlerType);

			MethodInfo declMethod = ei.EventHandlerType.GetMethod ("Invoke");

			hookupMethod = GetHookupMethod (declMethod, sendSignalMethod, Mapper.GetInterfaceName (ei), ei.Name);

			hookup_methods[ei] = hookupMethod;

			return hookupMethod;
		}

		public static DynamicMethod GetHookupMethod (MethodInfo declMethod, MethodInfo invokeMethod, string @interface, string member)
		{
			ParameterInfo[] delegateParms = declMethod.GetParameters ();
			Type[] hookupParms = new Type[delegateParms.Length+1];
			hookupParms[0] = typeof (BusObject);
			for (int i = 0; i < delegateParms.Length ; i++)
				hookupParms[i+1] = delegateParms[i].ParameterType;

			DynamicMethod hookupMethod = new DynamicMethod ("Handle" + member, declMethod.ReturnType, hookupParms, typeof (object));

			ILGenerator ilg = hookupMethod.GetILGenerator ();

			GenHookupMethod (ilg, declMethod, invokeMethod, @interface, member, hookupParms);

			return hookupMethod;
		}

		static MethodInfo getMethodFromHandleMethod = typeof (MethodBase).GetMethod ("GetMethodFromHandle");
		static ConstructorInfo argumentNullExceptionConstructor = typeof (ArgumentNullException).GetConstructor (new Type[] {typeof (string)});

		public static void GenHookupMethod (ILGenerator ilg, MethodInfo declMethod, MethodInfo invokeMethod, string @interface, string member, Type[] hookupParms)
		{
			Type retType = declMethod.ReturnType;

			//the BusObject instance
			ilg.Emit (OpCodes.Ldarg_0);

			//MethodInfo
			ilg.Emit (OpCodes.Ldtoken, declMethod);
			ilg.Emit (OpCodes.Call, getMethodFromHandleMethod);

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

				//null checking of parameters (but not their recursive contents)
				if (!t.IsValueType) {
					Label notNull = ilg.DefineLabel ();

					//if the value is null...
					ilg.Emit (OpCodes.Ldarg, i);
					ilg.Emit (OpCodes.Brtrue_S, notNull);

					//...throw Exception
					//TODO: use proper parameter names
					string paramName = "arg" + (i-1);
					ilg.Emit (OpCodes.Ldstr, paramName);
					ilg.Emit (OpCodes.Newobj, argumentNullExceptionConstructor);
					ilg.Emit (OpCodes.Throw);

					//was not null, so all is well
					ilg.MarkLabel (notNull);
				}

				ilg.Emit (OpCodes.Ldloc, local);
				//the instance parameter requires the -1 offset here
				ilg.Emit (OpCodes.Ldc_I4, i-1);
				ilg.Emit (OpCodes.Ldarg, i);

				if (t.IsValueType)
					ilg.Emit (OpCodes.Box, t);

				ilg.Emit (OpCodes.Stelem_Ref);
			}

			ilg.Emit (OpCodes.Ldloc, local);

			LocalBuilder exc = ilg.DeclareLocal (typeof (Exception));
			ilg.Emit (OpCodes.Ldloca_S, exc);

			//make the call
			ilg.Emit (invokeMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, invokeMethod);

			//define a label we'll use to deal with a non-null Exception
			Label noErr = ilg.DefineLabel ();

			//if the out Exception is not null...
			ilg.Emit (OpCodes.Ldloc, exc);
			ilg.Emit (OpCodes.Brfalse_S, noErr);

			//...throw it.
			ilg.Emit (OpCodes.Ldloc, exc);
			ilg.Emit (OpCodes.Throw);

			//Exception was null, so all is well
			ilg.MarkLabel (noErr);

			if (retType == typeof (void)) {
				//we aren't expecting a return value, so throw away the (hopefully) null return
				if (invokeMethod.ReturnType != typeof (void))
					ilg.Emit (OpCodes.Pop);
			} else {
				if (retType.IsValueType)
					ilg.Emit (OpCodes.Unbox_Any, retType);
				else
					ilg.Emit (OpCodes.Castclass, retType);
			}

			ilg.Emit (OpCodes.Ret);
		}

		public void SendSignal (MethodInfo mi, string @interface, string member, object[] outValues, out Exception exception)
		{
			exception = null;

			//TODO: make use of bus_name?

			Type[] outTypes = Mapper.GetTypes (ArgDirection.In, mi.GetParameters ());
			Signature outSig = Signature.GetSig (outTypes);

			Signal signal = new Signal (object_path, @interface, member);
			signal.message.Signature = outSig;

			if (outValues != null && outValues.Length != 0) {
				MessageWriter writer = new MessageWriter (Connection.NativeEndianness);
				writer.connection = conn;

				for (int i = 0 ; i != outTypes.Length ; i++)
					writer.Write (outTypes[i], outValues[i]);

				signal.message.Body = writer.ToArray ();
			}

			conn.Send (signal.message);
		}
	}
}

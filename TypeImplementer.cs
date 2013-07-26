// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace DBus
{
	using Protocol;

	class TypeImplementer
	{
		public static readonly TypeImplementer Root = new TypeImplementer ("DBus.Proxies", false);
		AssemblyBuilder asmB;
		ModuleBuilder modB;
		static readonly object getImplLock = new Object ();

		Dictionary<Type,Type> map = new Dictionary<Type,Type> ();

		static MethodInfo getTypeFromHandleMethod = typeof (Type).GetMethod ("GetTypeFromHandle", new Type[] {typeof (RuntimeTypeHandle)});
		static ConstructorInfo argumentNullExceptionConstructor = typeof (ArgumentNullException).GetConstructor (new Type[] {typeof (string)});
		static ConstructorInfo messageWriterConstructor = typeof (MessageWriter).GetConstructor (Type.EmptyTypes);
		static MethodInfo messageWriterWriteArray = typeof (MessageWriter).GetMethod ("WriteArray");
		static MethodInfo messageWriterWriteDict = typeof (MessageWriter).GetMethod ("WriteFromDict");
		static MethodInfo messageWriterWriteStruct = typeof (MessageWriter).GetMethod ("WriteStructure");
		static MethodInfo messageReaderReadValue = typeof (MessageReader).GetMethod ("ReadValue", new Type[] { typeof (System.Type) });
		static MethodInfo messageReaderReadArray = typeof (MessageReader).GetMethod ("ReadArray", Type.EmptyTypes);
		static MethodInfo messageReaderReadDictionary = typeof (MessageReader).GetMethod ("ReadDictionary", Type.EmptyTypes);
		static MethodInfo messageReaderReadStruct = typeof (MessageReader).GetMethod ("ReadStruct", Type.EmptyTypes);

		static Dictionary<Type,MethodInfo> writeMethods = new Dictionary<Type,MethodInfo> ();
		static Dictionary<Type,object> typeWriters = new Dictionary<Type,object> ();

		static MethodInfo sendMethodCallMethod = typeof (BusObject).GetMethod ("SendMethodCall");
		static MethodInfo sendSignalMethod = typeof (BusObject).GetMethod ("SendSignal");
		static MethodInfo toggleSignalMethod = typeof (BusObject).GetMethod ("ToggleSignal");

		static Dictionary<EventInfo,DynamicMethod> hookup_methods = new Dictionary<EventInfo,DynamicMethod> ();
		static Dictionary<Type,MethodInfo> readMethods = new Dictionary<Type,MethodInfo> ();

		public TypeImplementer (string name, bool canSave)
		{
			asmB = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName (name),
			                                                      canSave ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);
			modB = asmB.DefineDynamicModule (name);
		}

		public Type GetImplementation (Type declType)
		{
			Type retT;

			lock (getImplLock)
				if (map.TryGetValue (declType, out retT))
					return retT;

			string proxyName = declType.FullName + "Proxy";

			Type parentType;

			if (declType.IsInterface)
				parentType = typeof (BusObject);
			else
				parentType = declType;

			TypeBuilder typeB = modB.DefineType (proxyName, TypeAttributes.Class | TypeAttributes.Public, parentType);

			string interfaceName = null;
			if (declType.IsInterface)
				Implement (typeB, declType, interfaceName = Mapper.GetInterfaceName (declType));

			foreach (Type iface in declType.GetInterfaces ())
				Implement (typeB, iface, interfaceName == null ? Mapper.GetInterfaceName (iface) : interfaceName);

			retT = typeB.CreateType ();

			lock (getImplLock)
				map[declType] = retT;

			return retT;
		}

		static void Implement (TypeBuilder typeB, Type iface, string interfaceName)
		{
			typeB.AddInterfaceImplementation (iface);

			Dictionary<string,MethodBuilder> builders = new Dictionary<string,MethodBuilder> ();

			foreach (MethodInfo declMethod in iface.GetMethods ()) {
				ParameterInfo[] parms = declMethod.GetParameters ();

				Type[] parmTypes = new Type[parms.Length];
				for (int i = 0 ; i < parms.Length ; i++)
					parmTypes[i] = parms[i].ParameterType;

				MethodAttributes attrs = declMethod.Attributes ^ MethodAttributes.Abstract;
				attrs ^= MethodAttributes.NewSlot;
				attrs |= MethodAttributes.Final;
				MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, attrs, declMethod.ReturnType, parmTypes);
				typeB.DefineMethodOverride (method_builder, declMethod);

				//define in/out/ref/name for each of the parameters
				for (int i = 0; i < parms.Length ; i++)
					method_builder.DefineParameter (i + 1, parms[i].Attributes, parms[i].Name);

				ILGenerator ilg = method_builder.GetILGenerator ();
				GenHookupMethod (ilg, declMethod, sendMethodCallMethod, interfaceName, declMethod.Name);

				if (declMethod.IsSpecialName)
					builders[declMethod.Name] = method_builder;
			}

			foreach (EventInfo declEvent in iface.GetEvents ())
			{
				EventBuilder event_builder = typeB.DefineEvent (declEvent.Name, declEvent.Attributes, declEvent.EventHandlerType);
				event_builder.SetAddOnMethod (builders["add_" + declEvent.Name]);
				event_builder.SetRemoveOnMethod (builders["remove_" + declEvent.Name]);
			}

			foreach (PropertyInfo declProp in iface.GetProperties ())
			{
				List<Type> indexers = new List<Type> ();
				foreach (ParameterInfo pi in declProp.GetIndexParameters ())
					indexers.Add (pi.ParameterType);

				PropertyBuilder prop_builder = typeB.DefineProperty (declProp.Name, declProp.Attributes, declProp.PropertyType, indexers.ToArray ());
				MethodBuilder mb;
				if (builders.TryGetValue ("get_" + declProp.Name, out mb))
					prop_builder.SetGetMethod (mb);
				if (builders.TryGetValue ("set_" + declProp.Name, out mb))
					prop_builder.SetSetMethod (mb);
			}
		}

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
			for (int i = 0; i < delegateParms.Length; ++i)
				hookupParms[i + 1] = delegateParms[i].ParameterType;

			DynamicMethod hookupMethod = new DynamicMethod ("Handle" + member, declMethod.ReturnType, hookupParms, typeof (MessageWriter));

			ILGenerator ilg = hookupMethod.GetILGenerator ();

			GenHookupMethod (ilg, declMethod, invokeMethod, @interface, member);

			return hookupMethod;
		}

		public static MethodInfo GetWriteMethod (Type t)
		{
			MethodInfo meth;

			if (writeMethods.TryGetValue (t, out meth))
				return meth;

			DynamicMethod method_builder = new DynamicMethod ("Write" + t.Name, typeof (void), new Type[] {typeof (MessageWriter), t}, typeof (MessageWriter), true);

			ILGenerator ilg = method_builder.GetILGenerator ();

			ilg.Emit (OpCodes.Ldarg_0);
			ilg.Emit (OpCodes.Ldarg_1);

			GenWriter (ilg, t);

			ilg.Emit (OpCodes.Ret);

			meth = method_builder;

			writeMethods[t] = meth;
			return meth;
		}

		public static TypeWriter<T> GetTypeWriter<T> ()
		{
			Type t = typeof (T);

			object value;
			if (typeWriters.TryGetValue (t, out value))
				return (TypeWriter<T>)value;

			MethodInfo mi = GetWriteMethod (t);
			DynamicMethod dm = mi as DynamicMethod;
			if (dm == null)
				return null;

			TypeWriter<T> tWriter = dm.CreateDelegate (typeof (TypeWriter<T>)) as TypeWriter<T>;
			typeWriters[t] = tWriter;
			return tWriter;
		}

		//takes the Writer instance and the value of Type t off the stack, writes it
		public static void GenWriter (ILGenerator ilg, Type t)
		{
			Type tUnder = t;

			if (t.IsEnum)
				tUnder = Enum.GetUnderlyingType (t);

			Type type = t;

			MethodInfo exactWriteMethod = typeof (MessageWriter).GetMethod ("Write", BindingFlags.ExactBinding | BindingFlags.Instance | BindingFlags.Public, null, new Type[] {tUnder}, null);

			if (exactWriteMethod != null) {
				ilg.Emit (OpCodes.Call, exactWriteMethod);
			} else if (t.IsArray) {
				exactWriteMethod = messageWriterWriteArray.MakeGenericMethod (type.GetElementType ());
				ilg.Emit (OpCodes.Call, exactWriteMethod);
			} else if (type.IsGenericType && (type.GetGenericTypeDefinition () == typeof (IDictionary<,>) || type.GetGenericTypeDefinition () == typeof (Dictionary<,>))) {
				Type[] genArgs = type.GetGenericArguments ();
				exactWriteMethod = messageWriterWriteDict.MakeGenericMethod (genArgs);
				ilg.Emit (OpCodes.Call, exactWriteMethod);
			} else {
				MethodInfo mi = messageWriterWriteStruct.MakeGenericMethod (t);
				ilg.Emit (OpCodes.Call, mi);
			}
		}

		public static IEnumerable<FieldInfo> GetMarshalFields (Type type)
		{
			// FIXME: Field order!
			return type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static void GenHookupMethod (ILGenerator ilg, MethodInfo declMethod, MethodInfo invokeMethod, string @interface, string member)
		{
			ParameterInfo[] parms = declMethod.GetParameters ();
			Type retType = declMethod.ReturnType;

			//the BusObject instance
			ilg.Emit (OpCodes.Ldarg_0);

			ilg.Emit (OpCodes.Castclass, typeof (BusObject));

			//interface
			ilg.Emit (OpCodes.Ldstr, @interface);

			//special case event add/remove methods
			if (declMethod.IsSpecialName && (declMethod.Name.StartsWith ("add_") || declMethod.Name.StartsWith ("remove_"))) {
				string[] parts = declMethod.Name.Split (new char[]{'_'}, 2);
				string ename = parts[1];

				bool adding = parts[0] == "add";

				ilg.Emit (OpCodes.Ldstr, ename);

				ilg.Emit (OpCodes.Ldarg_1);

				ilg.Emit (OpCodes.Ldc_I4, adding ? 1 : 0);

				ilg.Emit (OpCodes.Tailcall);
				ilg.Emit (toggleSignalMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, toggleSignalMethod);
				ilg.Emit (OpCodes.Ret);
				return;
			}

			//property accessor mapping
			if (declMethod.IsSpecialName) {
				if (member.StartsWith ("get_"))
					member = "Get" + member.Substring (4);
				else if (member.StartsWith ("set_"))
					member = "Set" + member.Substring (4);
			}

			//member
			ilg.Emit (OpCodes.Ldstr, member);

			//signature
			Signature inSig;
			Signature outSig;
			SigsForMethod (declMethod, out inSig, out outSig);

			ilg.Emit (OpCodes.Ldstr, inSig.Value);

			LocalBuilder writer = ilg.DeclareLocal (typeof (MessageWriter));
			ilg.Emit (OpCodes.Newobj, messageWriterConstructor);
			ilg.Emit (OpCodes.Stloc, writer);

			foreach (ParameterInfo parm in parms)
			{
				if (parm.IsOut)
					continue;

				Type t = parm.ParameterType;
				//offset by one to account for "this"
				int i = parm.Position + 1;

				//null checking of parameters (but not their recursive contents)
				if (!t.IsValueType) {
					Label notNull = ilg.DefineLabel ();

					//if the value is null...
					ilg.Emit (OpCodes.Ldarg, i);
					ilg.Emit (OpCodes.Brtrue_S, notNull);

					//...throw Exception
					string paramName = parm.Name;
					ilg.Emit (OpCodes.Ldstr, paramName);
					ilg.Emit (OpCodes.Newobj, argumentNullExceptionConstructor);
					ilg.Emit (OpCodes.Throw);

					//was not null, so all is well
					ilg.MarkLabel (notNull);
				}

				ilg.Emit (OpCodes.Ldloc, writer);

				//the parameter
				ilg.Emit (OpCodes.Ldarg, i);

				GenWriter (ilg, t);
			}

			ilg.Emit (OpCodes.Ldloc, writer);

			//the expected return Type
			GenTypeOf (ilg, retType);

			LocalBuilder exc = ilg.DeclareLocal (typeof (Exception));
			ilg.Emit (OpCodes.Ldloca_S, exc);

			//make the call
			ilg.Emit (OpCodes.Callvirt, invokeMethod);

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

			if (invokeMethod.ReturnType == typeof (MessageReader)) {
				LocalBuilder reader = ilg.DeclareLocal (typeof (MessageReader));
				ilg.Emit (OpCodes.Stloc, reader);

				foreach (ParameterInfo parm in parms)
				{
					//t.IsByRef
					if (!parm.IsOut)
						continue;

					Type t = parm.ParameterType.GetElementType ();
					//offset by one to account for "this"
					int i = parm.Position + 1;

					ilg.Emit (OpCodes.Ldarg, i);
					ilg.Emit (OpCodes.Ldloc, reader);
					GenReader (ilg, t);
					ilg.Emit (OpCodes.Stobj, t);
				}

				if (retType != typeof (void)) {
					ilg.Emit (OpCodes.Ldloc, reader);
					GenReader (ilg, retType);
				}

				ilg.Emit (OpCodes.Ret);
				return;
			}

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


		public static bool SigsForMethod (MethodInfo mi, out Signature inSig, out Signature outSig)
		{
			inSig = Signature.Empty;
			outSig = Signature.Empty;

			foreach (ParameterInfo parm in mi.GetParameters ()) {
				if (parm.IsOut)
					outSig += Signature.GetSig (parm.ParameterType.GetElementType ());
				else
					inSig += Signature.GetSig (parm.ParameterType);
			}

			outSig += Signature.GetSig (mi.ReturnType);

			return true;
		}

		static void InitReaders ()
		{
			foreach (MethodInfo mi in typeof (MessageReader).GetMethods (BindingFlags.Instance | BindingFlags.Public)) {
				if (!mi.Name.StartsWith ("Read"))
					continue;
				if (mi.ReturnType == typeof (void))
					continue;
				if (mi.GetParameters ().Length != 0)
					continue;

				readMethods[mi.ReturnType] = mi;
			}
		}

		internal static MethodInfo GetReadMethod (Type t)
		{
			if (readMethods.Count == 0)
				InitReaders ();

			MethodInfo mi;
			if (readMethods.TryGetValue (t, out mi))
				return mi;

			return null;
		}

		internal static MethodCaller GenCaller (MethodInfo target)
		{
			DynamicMethod hookupMethod = GenReadMethod (target);
			MethodCaller caller = hookupMethod.CreateDelegate (typeof (MethodCaller)) as MethodCaller;
			return caller;
		}

		internal static DynamicMethod GenReadMethod (MethodInfo target)
		{
			Type[] parms = new Type[] { typeof (object), typeof (MessageReader), typeof (Message), typeof (MessageWriter) };
			DynamicMethod hookupMethod = new DynamicMethod ("Caller", typeof (void), parms, typeof (MessageReader));
			Gen (hookupMethod, target);
			return hookupMethod;
		}

		static void Gen (DynamicMethod hookupMethod, MethodInfo declMethod)
		{
			ILGenerator ilg = hookupMethod.GetILGenerator ();

			ParameterInfo[] parms = declMethod.GetParameters ();
			Type retType = declMethod.ReturnType;

			// The target instance
			ilg.Emit (OpCodes.Ldarg_0);

			Dictionary<ParameterInfo,LocalBuilder> locals = new Dictionary<ParameterInfo,LocalBuilder> ();

			foreach (ParameterInfo parm in parms) {

				Type parmType = parm.ParameterType;

				if (parm.IsOut) {
					LocalBuilder parmLocal = ilg.DeclareLocal (parmType.GetElementType ());
					locals[parm] = parmLocal;
					ilg.Emit (OpCodes.Ldloca, parmLocal);
					continue;
				}

				ilg.Emit (OpCodes.Ldarg_1);
				GenReader (ilg, parmType);
			}

			ilg.Emit (declMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, declMethod);

			foreach (ParameterInfo parm in parms) {
				if (!parm.IsOut)
					continue;

				Type parmType = parm.ParameterType.GetElementType ();

				LocalBuilder parmLocal = locals[parm];
				ilg.Emit (OpCodes.Ldarg_3); // writer
				ilg.Emit (OpCodes.Ldloc, parmLocal);
				GenWriter (ilg, parmType);
			}

			if (retType != typeof (void)) {
				// Skip reply message construction if MessageWriter is null

				LocalBuilder retLocal = ilg.DeclareLocal (retType);
				ilg.Emit (OpCodes.Stloc, retLocal);

				ilg.Emit (OpCodes.Ldarg_3); // writer
				ilg.Emit (OpCodes.Ldloc, retLocal);
				GenWriter (ilg, retType);
			}

			ilg.Emit (OpCodes.Ret);
		}

		public static void GenReader (ILGenerator ilg, Type t)
		{
			Type tUnder = t;

			if (t.IsEnum)
				tUnder = Enum.GetUnderlyingType (t);

			Type gDef = t.IsGenericType ? t.GetGenericTypeDefinition () : null;

			MethodInfo exactMethod = GetReadMethod (tUnder);
			if (exactMethod != null) {
				ilg.Emit (OpCodes.Callvirt, exactMethod);
			} else if (t.IsArray) {
				var tarray = t.GetElementType ();
				ilg.Emit (OpCodes.Call, messageReaderReadArray.MakeGenericMethod (new[] { tarray }));
			} else if (gDef != null && (gDef == typeof (IDictionary<,>) || gDef == typeof (Dictionary<,>))) {
				var tmpTypes = t.GetGenericArguments ();
				MethodInfo mi = messageReaderReadDictionary.MakeGenericMethod (new[] { tmpTypes[0], tmpTypes[1] });
				ilg.Emit (OpCodes.Callvirt, mi);
			} else if (t.IsInterface)
				GenFallbackReader (ilg, tUnder);
			else if (!tUnder.IsValueType) {
				ilg.Emit (OpCodes.Callvirt, messageReaderReadStruct.MakeGenericMethod (tUnder));
			} else
				GenFallbackReader (ilg, tUnder);
		}

		public static void GenFallbackReader (ILGenerator ilg, Type t)
		{
			// TODO: do we want non-tUnder here for Castclass use?
			if (ProtocolInformation.Verbose)
				Console.Error.WriteLine ("Bad! Generating fallback reader for " + t);

			// The Type parameter
			GenTypeOf (ilg, t);
			ilg.Emit (OpCodes.Callvirt, messageReaderReadValue);

			if (t.IsValueType)
				ilg.Emit (OpCodes.Unbox_Any, t);
			else
				ilg.Emit (OpCodes.Castclass, t);
		}

		static void GenTypeOf (ILGenerator ilg, Type t)
		{
			ilg.Emit (OpCodes.Ldtoken, t);
			ilg.Emit (OpCodes.Call, getTypeFromHandleMethod);
		}
	}

	internal delegate void TypeWriter<T> (MessageWriter writer, T value);

	internal delegate void MethodCaller (object instance, MessageReader rdr, Message msg, MessageWriter ret);
}

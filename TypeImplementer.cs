// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace NDesk.DBus
{
	static class TypeImplementer
	{
		static AssemblyBuilder asmB;
		static ModuleBuilder modB;

		static void InitHack ()
		{
			if (asmB != null)
				return;

			asmB = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("NDesk.DBus.Proxies"), AssemblyBuilderAccess.Run);
			modB = asmB.DefineDynamicModule ("ProxyModule");
		}

		static Dictionary<Type,Type> map = new Dictionary<Type,Type> ();

		public static Type GetImplementation (Type declType)
		{
			Type retT;

			if (map.TryGetValue (declType, out retT))
				return retT;

			InitHack ();

			TypeBuilder typeB = modB.DefineType (declType.Name + "Proxy", TypeAttributes.Class | TypeAttributes.Public, typeof (BusObject));

			Implement (typeB, declType);

			foreach (Type iface in declType.GetInterfaces ())
				Implement (typeB, iface);

			retT = typeB.CreateType ();
			map[declType] = retT;

			return retT;
		}

		public static void Implement (TypeBuilder typeB, Type iface)
		{
			typeB.AddInterfaceImplementation (iface);

			foreach (MethodInfo declMethod in iface.GetMethods ()) {
				ParameterInfo[] parms = declMethod.GetParameters ();

				Type[] parmTypes = new Type[parms.Length];
				for (int i = 0 ; i < parms.Length ; i++)
					parmTypes[i] = parms[i].ParameterType;

				MethodAttributes attrs = declMethod.Attributes ^ MethodAttributes.Abstract;
				MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, attrs, declMethod.ReturnType, parmTypes);
				typeB.DefineMethodOverride (method_builder, declMethod);

				//define in/out/ref/name for each of the parameters
				for (int i = 0; i < parms.Length ; i++)
					method_builder.DefineParameter (i, parms[i].Attributes, parms[i].Name);

				//Console.WriteLine ("retType: " + declMethod.ReturnType);
				ILGenerator ilg = method_builder.GetILGenerator ();
				//GenHookupMethod (ilg, declMethod, sendMethodCallMethod, Mapper.GetInterfaceName (iface), declMethod.Name);
				if (declMethod.ReturnType != typeof (void) && !declMethod.ReturnType.IsPrimitive && declMethod.ReturnType.IsValueType)
					GenHookupMethod (ilg, declMethod, sendMethodCallOldMethod, Mapper.GetInterfaceName (iface), declMethod.Name);
				else
					GenHookupMethod (ilg, declMethod, sendMethodCallMethod, Mapper.GetInterfaceName (iface), declMethod.Name);
			}
		}

		static MethodInfo sendMethodCallMethod = typeof (BusObject).GetMethod ("SendMethodCall");
		static MethodInfo sendMethodCallOldMethod = typeof (BusObject).GetMethod ("SendMethodCallOld");
		static MethodInfo sendSignalMethod = typeof (BusObject).GetMethod ("SendSignal");
		static MethodInfo toggleSignalMethod = typeof (BusObject).GetMethod ("ToggleSignal");

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

			DynamicMethod hookupMethod = new DynamicMethod ("Handle" + member, declMethod.ReturnType, hookupParms, typeof (MessageWriter));

			ILGenerator ilg = hookupMethod.GetILGenerator ();

			GenHookupMethod (ilg, declMethod, invokeMethod, @interface, member);

			return hookupMethod;
		}

		//static MethodInfo getMethodFromHandleMethod = typeof (MethodBase).GetMethod ("GetMethodFromHandle", new Type[] {typeof (RuntimeMethodHandle)});
		static MethodInfo getTypeFromHandleMethod = typeof (Type).GetMethod ("GetTypeFromHandle", new Type[] {typeof (RuntimeTypeHandle)});
		static ConstructorInfo argumentNullExceptionConstructor = typeof (ArgumentNullException).GetConstructor (new Type[] {typeof (string)});
		static ConstructorInfo messageWriterConstructor = typeof (MessageWriter).GetConstructor (Type.EmptyTypes);
		static MethodInfo messageWriterWriteMethod = typeof (MessageWriter).GetMethod ("WriteComplex", new Type[] {typeof (object), typeof (Type)});
		static MethodInfo messageWriterWritePad = typeof (MessageWriter).GetMethod ("WritePad", new Type[] {typeof (int)});
		static MethodInfo messageReaderReadPad = typeof (MessageReader).GetMethod ("ReadPad", new Type[] {typeof (int)});

		static Dictionary<Type,MethodInfo> writeMethods = new Dictionary<Type,MethodInfo> ();

		public static MethodInfo GetWriteMethod (Type t)
		{
			MethodInfo meth;

			if (writeMethods.TryGetValue (t, out meth))
				return meth;

			/*
			Type tUnder = t;
			if (t.IsEnum)
				tUnder = Enum.GetUnderlyingType (t);

			meth = typeof (MessageWriter).GetMethod ("Write", BindingFlags.ExactBinding | BindingFlags.Instance | BindingFlags.Public, null, new Type[] {tUnder}, null);
			if (meth != null) {
				writeMethods[t] = meth;
				return meth;
			}
			*/

			DynamicMethod method_builder = new DynamicMethod ("Write" + t.Name, typeof (void), new Type[] {typeof (MessageWriter), t}, typeof (MessageWriter));

			ILGenerator ilg = method_builder.GetILGenerator ();

			ilg.Emit (OpCodes.Ldarg_0);
			ilg.Emit (OpCodes.Ldarg_1);

			GenMarshalWrite (ilg, t);

			ilg.Emit (OpCodes.Ret);

			meth = method_builder;

			writeMethods[t] = meth;
			return meth;
		}

		//takes the Writer instance and the value of Type t off the stack, writes it
		public static void GenWriter (ILGenerator ilg, Type t)
		{
			Type tUnder = t;
			//bool imprecise = false;

			if (t.IsEnum) {
				tUnder = Enum.GetUnderlyingType (t);
				//imprecise = true;
			}

			//MethodInfo exactWriteMethod = typeof (MessageWriter).GetMethod ("Write", new Type[] {tUnder});
			MethodInfo exactWriteMethod = typeof (MessageWriter).GetMethod ("Write", BindingFlags.ExactBinding | BindingFlags.Instance | BindingFlags.Public, null, new Type[] {tUnder}, null);
			//ExactBinding InvokeMethod

			if (exactWriteMethod != null) {
				//if (imprecise)
				//	ilg.Emit (OpCodes.Castclass, tUnder);

				ilg.Emit (exactWriteMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, exactWriteMethod);
			} else {
				//..boxed if necessary
				if (t.IsValueType)
					ilg.Emit (OpCodes.Box, t);

				//the Type parameter
				ilg.Emit (OpCodes.Ldtoken, t);
				ilg.Emit (OpCodes.Call, getTypeFromHandleMethod);

				ilg.Emit (messageWriterWriteMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, messageWriterWriteMethod);
			}
		}

		public static IEnumerable<FieldInfo> GetMarshalFields (Type type)
		{
			// FIXME: Field order!
			return type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		/*
		public static void GetBlittableSegments (IEnumerable<FieldInfo> fields)
		{
			int totalSize = 0;
			//List<FieldInfo>
			//foreach (FieldInfo fi in GetMarshalFields (type))
			foreach (FieldInfo fi in fields) {
				Type t = fi.FieldType;
				Signature sig = Signature.GetSig (t);
				int fixedSize = totalSize;
				if (sig.GetFixedSize (ref fixedSize)) {
					totalSize = fixedSize;
					continue;
				}

				//ilg.Emit (OpCodes.Ldflda, fiStart);
			}
		}
		*/

		//takes a writer and a reference to an object off the stack
		public static void GenMarshalWrite (ILGenerator ilg, Type type)
		{
			LocalBuilder val = ilg.DeclareLocal (type);
			ilg.Emit (OpCodes.Stloc, val);

			LocalBuilder writer = ilg.DeclareLocal (typeof (MessageWriter));
			ilg.Emit (OpCodes.Stloc, writer);

			//align to 8 for structs
			ilg.Emit (OpCodes.Ldloc, writer);
			ilg.Emit (OpCodes.Ldc_I4, 8);
			ilg.Emit (OpCodes.Call, messageWriterWritePad);

			foreach (FieldInfo fi in GetMarshalFields (type)) {
				Type t = fi.FieldType;

				// null checking of fields
				if (!t.IsValueType) {
					Label notNull = ilg.DefineLabel ();

					//if the value is null...
					//ilg.Emit (OpCodes.Ldarg, i);
					ilg.Emit (OpCodes.Ldloc, val);
					ilg.Emit (OpCodes.Ldfld, fi);

					ilg.Emit (OpCodes.Brtrue_S, notNull);

					//...throw Exception
					string paramName = fi.Name;
					ilg.Emit (OpCodes.Ldstr, paramName);
					// TODO: Should not really be argumentNullException
					ilg.Emit (OpCodes.Newobj, argumentNullExceptionConstructor);
					ilg.Emit (OpCodes.Throw);

					//was not null, so all is well
					ilg.MarkLabel (notNull);
				}

				//the Writer to write to
				ilg.Emit (OpCodes.Ldloc, writer);

				//the object to read from
				ilg.Emit (OpCodes.Ldloc, val);
				ilg.Emit (OpCodes.Ldfld, fi);

				GenWriter (ilg, t);
			}
		}

		//takes a reader and a reference to an object off the stack
		public static void GenMarshalRead (ILGenerator ilg, Type type)
		{
			/*
			int fixedSize = 0;
			if (Signature.GetSig (type).GetFixedSize (ref fixedSize)) {
				Console.Error.WriteLine ("Type " + type + " is blittable.");
			}
			*/

			// FIXME: Newobj fails if type has no default ctor!

			//Console.WriteLine ("GenMarshalRead " + type);
			LocalBuilder val = ilg.DeclareLocal (type);
			ConstructorInfo ctor = type.GetConstructor (Type.EmptyTypes);
			//ConstructorInfo ctor = type.TypeInitializer;
			//Console.WriteLine ("ctor: " + ctor);
			//if (ctor != null)
			ilg.Emit (OpCodes.Newobj, ctor);
			//ilg.Emit (OpCodes.Ldloca, type);
			//ilg.Emit (OpCodes.Initobj, type);
			//if (!type.IsValueType)
			ilg.Emit (OpCodes.Stloc, val);

			LocalBuilder reader = ilg.DeclareLocal (typeof (MessageReader));
			ilg.Emit (OpCodes.Stloc, reader);

			//align to 8 for structs
			ilg.Emit (OpCodes.Ldloc, reader);
			ilg.Emit (OpCodes.Ldc_I4, 8);
			ilg.Emit (OpCodes.Call, messageReaderReadPad);

			foreach (FieldInfo fi in GetMarshalFields (type)) {
				Type t = fi.FieldType;

				//the object to read into
				ilg.Emit (OpCodes.Ldloc, val);

				//the Reader to read from
				ilg.Emit (OpCodes.Ldloc, reader);

				GenReader (ilg, t);

				ilg.Emit (OpCodes.Stfld, fi);
			}

			ilg.Emit (OpCodes.Ldloc, val);
			//if (type.IsValueType)
			//	ilg.Emit (OpCodes.Box, type);
		}

		public static void GenHookupMethod (ILGenerator ilg, MethodInfo declMethod, MethodInfo invokeMethod, string @interface, string member)
		{
			ParameterInfo[] parms = declMethod.GetParameters ();
			Type retType = declMethod.ReturnType;

			//the BusObject instance
			ilg.Emit (OpCodes.Ldarg_0);

			//MethodInfo
			/*
			ilg.Emit (OpCodes.Ldtoken, declMethod);
			ilg.Emit (OpCodes.Call, getMethodFromHandleMethod);
			*/

			//interface
			ilg.Emit (OpCodes.Ldstr, @interface);

			//special case event add/remove methods
			if (declMethod.IsSpecialName && (declMethod.Name.StartsWith ("add_") || declMethod.Name.StartsWith ("remove_"))) {
				string[] parts = declMethod.Name.Split (new char[]{'_'}, 2);
				string ename = parts[1];
				//Delegate dlg = (Delegate)inArgs[0];
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
			Signature inSig = Signature.Empty;
			Signature outSig = Signature.Empty;
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
			ilg.Emit (OpCodes.Ldtoken, retType);
			ilg.Emit (OpCodes.Call, getTypeFromHandleMethod);

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

			if (invokeMethod.ReturnType == typeof (MessageReader)) {
				/*
				Label notNull = ilg.DefineLabel ();
				//if the value is null...
				ilg.Emit (OpCodes.Ldarg, i);
				ilg.Emit (OpCodes.Brtrue_S, notNull);
				//was not null, so all is well
				ilg.MarkLabel (notNull);
				*/

				LocalBuilder reader = ilg.DeclareLocal (typeof (MessageReader));
				ilg.Emit (OpCodes.Stloc, reader);

				foreach (ParameterInfo parm in parms)
				{
					//t.IsByRef
					if (!parm.IsOut)
						continue;

					Console.WriteLine ("parm.ParameterType: " + parm.ParameterType);
					Console.WriteLine ("parm.ParameterType.GetElementType (): " + parm.ParameterType.GetElementType ());
					//Type t = parm.ParameterType;
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

		static Dictionary<Type,MethodInfo> readMethods = new Dictionary<Type,MethodInfo> ();
		static void InitReaders ()
		{
			foreach (MethodInfo mi in typeof (MessageReader).GetMethods (BindingFlags.Instance | BindingFlags.Public)) {
				if (!mi.Name.StartsWith ("Read"))
					continue;
				if (mi.ReturnType == typeof (void))
					continue;
				if (mi.GetParameters ().Length != 0)
					continue;
				//Console.WriteLine ("Adding reader " + mi);
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

			/*
			Type tIn = t;
			if (t.IsValueType)
			*/

			/*
			DynamicMethod meth = new DynamicMethod ("Read" + t.Name, t, new Type[] {typeof (MessageReader)}, true);

			ILGenerator ilg = meth.GetILGenerator ();
			ilg.Emit (OpCodes.Ldarg_0);
			ilg.Emit (OpCodes.Ldarg_1);

			//GenMarshalRead (ilg, t);
			GenReader (ilg, t);

			ilg.Emit (OpCodes.Ret);

			writeMethods[t] = meth;
			return meth;
			*/
			return null;
		}

		internal static MethodCaller2 GenCaller2 (MethodInfo target)
		{
			DynamicMethod hookupMethod = GenReadMethod (target);
			MethodCaller2 caller = hookupMethod.CreateDelegate (typeof (MethodCaller2)) as MethodCaller2;
			return caller;
		}

		internal static MethodCaller GenCaller (MethodInfo target, object targetInstance)
		{
			DynamicMethod hookupMethod = GenReadMethod (target);
			MethodCaller caller = hookupMethod.CreateDelegate (typeof (MethodCaller), targetInstance) as MethodCaller;
			return caller;
		}

		internal static DynamicMethod GenReadMethod (MethodInfo target)
		{
			//Type[] parms = new Type[] { typeof (object), typeof (MessageReader), typeof (Message) };
			Type[] parms = new Type[] { typeof (object), typeof (MessageReader), typeof (Message), typeof (MessageWriter) };
			DynamicMethod hookupMethod = new DynamicMethod ("Caller", typeof (void), parms, typeof (MessageReader));
			Gen (hookupMethod, target);
			return hookupMethod;
		}

		static void Gen (DynamicMethod hookupMethod, MethodInfo declMethod)
		{
			//Console.Error.WriteLine ("Target: " + declMethod);
			ILGenerator ilg = hookupMethod.GetILGenerator ();

			ParameterInfo[] parms = declMethod.GetParameters ();
			Type retType = declMethod.ReturnType;

			//if (retType != typeof (void))
			//	throw new Exception ("Bad retType " + retType);

			// The target instance
			ilg.Emit (OpCodes.Ldarg_0);

			Dictionary<ParameterInfo,LocalBuilder> locals = new Dictionary<ParameterInfo,LocalBuilder> ();

			foreach (ParameterInfo parm in parms) {
				/*
				if (parm.IsOut)
					continue;
					*/

				Type parmType = parm.ParameterType;

				if (parm.IsOut) {
					LocalBuilder parmLocal = ilg.DeclareLocal (parmType.GetElementType ());
					locals[parm] = parmLocal;
					ilg.Emit (OpCodes.Ldloca, parmLocal);
					continue;
				}

				/*
				MethodInfo exactMethod = GetReadMethod (parmType);
				// The MessageReader instance
				ilg.Emit (OpCodes.Ldarg_1);
				ilg.Emit (exactMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, exactMethod);
				*/

				ilg.Emit (OpCodes.Ldarg_1);
				GenReader (ilg, parmType);
			}

			//ilg.Emit (OpCodes.Ldc_I4, 8);
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
				/*
				Label needsReply = ilg.DefineLabel ();
				ilg.Emit (OpCodes.Ldarg_3); // writer
				ilg.Emit (OpCodes.Brtrue_S, needsReply);
				ilg.Emit (OpCodes.Pop);
				//ilg.Emit (OpCodes.Brfalse_S, endLabel);
				*/

				//Console.WriteLine ("retType: " + retType);
				LocalBuilder retLocal = ilg.DeclareLocal (retType);
				ilg.Emit (OpCodes.Stloc, retLocal);

				/*
				LocalBuilder writer = ilg.DeclareLocal (typeof (MessageWriter));
				ilg.Emit (OpCodes.Newobj, messageWriterConstructor);
				ilg.Emit (OpCodes.Stloc, writer);
				*/

				ilg.Emit (OpCodes.Ldarg_3); // writer
				//ilg.Emit (OpCodes.Ldloc, writer);
				ilg.Emit (OpCodes.Ldloc, retLocal);
				GenWriter (ilg, retType);

				//ilg.Emit (OpCodes.Ldloc, writer);
				//ilg.MarkLabel (endLabel);
			}

			ilg.Emit (OpCodes.Ret);
		}

		// System.MethodAccessException: Method `ManagedDBusTestExport:<Main>m__0 (string,object,double,MyTuple)' is inaccessible from method `(wrapper dynamic-method) object:Caller (object,NDesk.DBus.MessageReader,NDesk.DBus.Message,NDesk.DBus.MessageWriter)'

		//takes the Reader instance off the stack, puts value of type t on the stack
		public static void GenReader (ILGenerator ilg, Type t)
		{
			// TODO: Cache generated methods
			// TODO: Generate methods with the correct module/type permissions

			Type tUnder = t;
			//bool imprecise = false;

			if (t.IsEnum) {
				tUnder = Enum.GetUnderlyingType (t);
				//imprecise = true;
			}

			MethodInfo exactMethod = GetReadMethod (tUnder);
			if (exactMethod != null)
				ilg.Emit (exactMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, exactMethod);
			else if (t.IsArray)
				GenReadCollection (ilg, t);
			else if (t.IsGenericType && (t.GetGenericTypeDefinition () == typeof (IList<>)))
				GenReadCollection (ilg, t);
			else if (t.IsGenericType && (t.GetGenericTypeDefinition () == typeof (IDictionary<,>)))
				GenReadCollection (ilg, t);
			else if (t.IsInterface)
				GenReadFallback (ilg, tUnder);
			else if (!tUnder.IsValueType) {
				//Console.Error.WriteLine ("Gen struct reader for " + t);
				//ilg.Emit (OpCodes.Newobj, messageWriterConstructor);
				//if (tUnder.IsValueType)
				//	throw new Exception ("Can't handle value types yet");
				GenMarshalRead (ilg, tUnder);
			} else
				GenReadFallback (ilg, tUnder);

			/*
				//if (t.IsValueType)
				//	ilg.Emit (OpCodes.Unbox_Any, t);
				//else
					ilg.Emit (OpCodes.Castclass, t);
					*/
		}

		public static void GenReadFallback (ILGenerator ilg, Type t)
		{
			// TODO: do we want non-tUnder here for Castclass use?
			Console.Error.WriteLine ("Bad! Generating fallback reader for " + t);

			MethodInfo exactMethod;
			exactMethod = typeof (MessageReader).GetMethod ("ReadValue", new Type[] { typeof (System.Type) });

			// The Type parameter
			ilg.Emit (OpCodes.Ldtoken, t);
			ilg.Emit (OpCodes.Call, getTypeFromHandleMethod);

			ilg.Emit (exactMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, exactMethod);

			if (t.IsValueType)
				ilg.Emit (OpCodes.Unbox_Any, t);
			else
				ilg.Emit (OpCodes.Castclass, t);
		}

		public static void GenReadArrayFixed (ILGenerator ilg, Type t, int knownElemSize)
		{
			//Console.Error.WriteLine ("GenReadArrayFixed " + t);
			LocalBuilder readerLocal = ilg.DeclareLocal (typeof (MessageReader));
			ilg.Emit (OpCodes.Stloc, readerLocal);

			Type tElem = t.GetElementType ();
			Signature sigElem = Signature.GetSig (tElem);
			int alignElem = sigElem.Alignment;
			int knownElemSizePadded = Protocol.Padded (knownElemSize, sigElem.Alignment);
			int managedElemSize = System.Runtime.InteropServices.Marshal.SizeOf (tElem);

			/*
			Console.WriteLine ("managedElemSize: " + managedElemSize);
			Console.WriteLine ("elemSize: " + knownElemSize);
			Console.WriteLine ("elemSizePadded: " + knownElemSizePadded);
			*/

			// Read the array's byte length
			ilg.Emit (OpCodes.Ldloc, readerLocal);
			MethodInfo exactMethod = GetReadMethod (typeof (uint));
			ilg.Emit (exactMethod.IsFinal ? OpCodes.Call : OpCodes.Callvirt, exactMethod);
			LocalBuilder sizeLocal = ilg.DeclareLocal (typeof (uint));
			ilg.Emit (OpCodes.Stloc, sizeLocal);

			// WARNING: This may skew pos when we later increment it!
			if (alignElem > 4) {
				// Align to element if alignment requirement is higher than 4 (since we just read a uint)
				ilg.Emit (OpCodes.Ldloc, readerLocal);
				ilg.Emit (OpCodes.Ldc_I4, alignElem);
				ilg.Emit (OpCodes.Call, messageReaderReadPad);
			}

			//ilg.EmitWriteLine (sizeLocal);

			/*
			// Take the array's byte length
			ilg.Emit (OpCodes.Ldloc, sizeLocal);
			// Divide by the known element size
			//ilg.Emit (OpCodes.Ldc_I4, knownElemSizePadded);
			ilg.Emit (OpCodes.Ldc_I4, knownElemSize);
			ilg.Emit (OpCodes.Div_Un);
			*/

			// Create a new array of the correct element length
			ilg.Emit (OpCodes.Ldloc, sizeLocal);
			if (knownElemSizePadded > 1) {
				ilg.Emit (OpCodes.Ldc_I4, alignElem);
				MethodInfo paddedMethod = typeof (Protocol).GetMethod ("Padded");
				ilg.Emit (OpCodes.Call, paddedMethod);
				// Divide by the known element size
				ilg.Emit (OpCodes.Ldc_I4, knownElemSizePadded);
				ilg.Emit (OpCodes.Div_Un);
			}
			ilg.Emit (OpCodes.Newarr, tElem);
			LocalBuilder aryLocal = ilg.DeclareLocal (t);
			ilg.Emit (OpCodes.Stloc, aryLocal);

			Label nonBlitLabel = ilg.DefineLabel ();
			Label endLabel = ilg.DefineLabel ();

			// TODO: Ensure enough bytes available!

			// Blit where possible

			// shouldBlit: Blit if endian is native
			// mustBlit: Blit regardless of endian (ie. byte or structs containing only bytes)
			bool shouldBlit = tElem.IsValueType && knownElemSizePadded == managedElemSize && !sigElem.IsStruct;
			//bool shouldBlit = tElem.IsValueType && knownElemSizePadded == managedElemSize;
			bool mustBlit = shouldBlit && knownElemSizePadded == 1;

			if (shouldBlit) {
				//Console.Error.WriteLine ("Blit read array " + tElem);

				if (!mustBlit) {
					// Check to see if we can blit the data structures
					FieldInfo nativeEndianField = typeof (MessageReader).GetField ("IsNativeEndian");
					ilg.Emit (OpCodes.Ldloc, readerLocal);
					ilg.Emit (OpCodes.Ldfld, nativeEndianField);
					ilg.Emit (OpCodes.Brfalse_S, nonBlitLabel);
				}

				// Get the destination address
				ilg.Emit (OpCodes.Ldloc, aryLocal);
				ilg.Emit (OpCodes.Ldc_I4, 0);
				ilg.Emit (OpCodes.Ldelema, tElem);

				// Get the source address
				FieldInfo dataField = typeof (MessageReader).GetField ("data");
				FieldInfo posField = typeof (MessageReader).GetField ("pos");
				ilg.Emit (OpCodes.Ldloc, readerLocal);
				ilg.Emit (OpCodes.Ldfld, dataField);
				{
					ilg.Emit (OpCodes.Ldloc, readerLocal);
					ilg.Emit (OpCodes.Ldfld, posField);
				}
				ilg.Emit (OpCodes.Ldelema, typeof (byte));

				// The number of bytes to copy
				ilg.Emit (OpCodes.Ldloc, sizeLocal);

				// Blit the array
				ilg.Emit (OpCodes.Cpblk);

				// pos += bytesRead
				ilg.Emit (OpCodes.Ldloc, readerLocal);
				ilg.Emit (OpCodes.Ldloc, readerLocal);
				ilg.Emit (OpCodes.Ldfld, posField);
				ilg.Emit (OpCodes.Ldloc, sizeLocal);
				ilg.Emit (OpCodes.Add);
				ilg.Emit (OpCodes.Stfld, posField);

				ilg.Emit (OpCodes.Br, endLabel);
			}

			if (!mustBlit) {
				ilg.MarkLabel (nonBlitLabel);

				// for (int i = 0 ; i < ary.Length ; i++)
				LocalBuilder indexLocal = ilg.DeclareLocal (typeof (int));
				ilg.Emit (OpCodes.Ldc_I4, 0);
				ilg.Emit (OpCodes.Stloc, indexLocal);

				Label loopStartLabel = ilg.DefineLabel ();
				Label loopEndLabel = ilg.DefineLabel ();

				ilg.Emit (OpCodes.Br, loopEndLabel);

				ilg.MarkLabel (loopStartLabel);

				{
					// Read and store an element to the array
					ilg.Emit (OpCodes.Ldloc, aryLocal);
					ilg.Emit (OpCodes.Ldloc, indexLocal);

					ilg.Emit (OpCodes.Ldloc, readerLocal);
					GenReader (ilg, tElem);

					ilg.Emit (OpCodes.Stelem, tElem);
				}

				// i++
				ilg.Emit (OpCodes.Ldloc, indexLocal);
				ilg.Emit (OpCodes.Ldc_I4, 1);
				ilg.Emit (OpCodes.Add);
				ilg.Emit (OpCodes.Stloc, indexLocal);

				ilg.MarkLabel (loopEndLabel);
				ilg.Emit (OpCodes.Ldloc, indexLocal);
				ilg.Emit (OpCodes.Ldloc, aryLocal);
				ilg.Emit (OpCodes.Ldlen);
				ilg.Emit (OpCodes.Blt, loopStartLabel);
			}

			ilg.MarkLabel (endLabel);

			// Return the new array
			ilg.Emit (OpCodes.Ldloc, aryLocal);
		}

		public static void GenReadCollection (ILGenerator ilg, Type type)
		{
			//Console.WriteLine ("GenReadCollection " + type);
			//Console.WriteLine ("Sig: " + Signature.GetSig (type));
			int fixedSize = 0;
			if (type.IsArray && Signature.GetSig (type.GetElementType ()).GetFixedSize (ref fixedSize)) {
				GenReadArrayFixed (ilg, type, fixedSize);
				return;
			}

			LocalBuilder readerLocal = ilg.DeclareLocal (typeof (MessageReader));
			ilg.Emit (OpCodes.Stloc, readerLocal);

			//Type[] genArgs = type.GetGenericArguments ();
			Type[] genArgs = type.IsArray ? new Type[] { type.GetElementType () } : type.GetGenericArguments ();
			//Type[] genArgs = new Type[] { type.GetElementType () };
			//Type tElem = genArgs[0];

			//Type collType = Mapper.GetGenericType (typeof (List<>), genArgs);
			Type collType = Mapper.GetGenericType (genArgs.Length == 2 ? typeof (Dictionary<,>) : typeof (List<>), genArgs);

			ConstructorInfo ctor = collType.GetConstructor (Type.EmptyTypes);
			ilg.Emit (OpCodes.Newobj, ctor);

			LocalBuilder collLocal = ilg.DeclareLocal (collType);
			ilg.Emit (OpCodes.Stloc, collLocal);

			//MethodInfo addMethod = dictType.GetMethod ("Add", new Type[] { tKey, tValue });
			MethodInfo addMethod = collType.GetMethod ("Add", genArgs);


			// Read the array's byte length
			MethodInfo readUInt32Method = GetReadMethod (typeof (uint));
			ilg.Emit (OpCodes.Ldloc, readerLocal);
			ilg.Emit (readUInt32Method.IsFinal ? OpCodes.Call : OpCodes.Callvirt, readUInt32Method);

			{
				// Align to 8 for structs
				ilg.Emit (OpCodes.Ldloc, readerLocal);
				//ilg.Emit (OpCodes.Ldc_I4, 8);
				// TODO: This padding logic is sketchy
				ilg.Emit (OpCodes.Ldc_I4, genArgs.Length > 1 ? 8 : Signature.GetSig (genArgs[0]).Alignment);
				ilg.Emit (OpCodes.Call, messageReaderReadPad);
			}

			// Similar to the fixed array loop code

			FieldInfo posField = typeof (MessageReader).GetField ("pos");
			LocalBuilder endPosLocal = ilg.DeclareLocal (typeof (int));
			ilg.Emit (OpCodes.Ldloc, readerLocal);
			ilg.Emit (OpCodes.Ldfld, posField);
			
			// Add the current position and byte length to determine endPos
			// TODO: Consider padding?
			ilg.Emit (OpCodes.Add);
			ilg.Emit (OpCodes.Stloc, endPosLocal);

			{
				Label loopStartLabel = ilg.DefineLabel ();
				Label loopEndLabel = ilg.DefineLabel ();

				ilg.Emit (OpCodes.Br, loopEndLabel);

				ilg.MarkLabel (loopStartLabel);

				{
					if (genArgs.Length > 1) {
						// Align to 8 for structs
						ilg.Emit (OpCodes.Ldloc, readerLocal);
						ilg.Emit (OpCodes.Ldc_I4, 8);
						ilg.Emit (OpCodes.Call, messageReaderReadPad);
					}

					// Read and store an element to the array
					ilg.Emit (OpCodes.Ldloc, collLocal);

					foreach (Type genArg in genArgs) {
						ilg.Emit (OpCodes.Ldloc, readerLocal);
						GenReader (ilg, genArg);
					}

					ilg.Emit (OpCodes.Call, addMethod);
				}

				ilg.MarkLabel (loopEndLabel);

				//ilg.Emit (OpCodes.Ldloc, indexLocal);
				ilg.Emit (OpCodes.Ldloc, readerLocal);
				ilg.Emit (OpCodes.Ldfld, posField);

				ilg.Emit (OpCodes.Ldloc, endPosLocal);
				ilg.Emit (OpCodes.Blt, loopStartLabel);
			}

			// Return the new collection
			ilg.Emit (OpCodes.Ldloc, collLocal);

			if (type.IsArray) {
				MethodInfo toArrayMethod = collType.GetMethod ("ToArray", Type.EmptyTypes);
				ilg.Emit (OpCodes.Call, toArrayMethod);
			}
		}
	}

	internal delegate void MethodCaller (MessageReader rdr, Message msg, MessageWriter ret);
	internal delegate void MethodCaller2 (object instance, MessageReader rdr, Message msg, MessageWriter ret);
}

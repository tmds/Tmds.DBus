// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using System.IO;
using System.Xml.Serialization;

namespace NDesk.DBus
{
	using Introspection;

	//FIXME: debug hack
	public delegate void VoidHandler ();

	public partial class Connection
	{
		//dynamically defines a Type for the proxy object using D-Bus introspection
		public object GetObject (string bus_name, ObjectPath path)
		{
			org.freedesktop.DBus.Introspectable intros = GetObject<org.freedesktop.DBus.Introspectable> (bus_name, path);
			string data = intros.Introspect ();

			StringReader sr = new StringReader (data);
			XmlSerializer sz = new XmlSerializer (typeof (Node));
			Node node = (Node)sz.Deserialize (sr);

			Type type = TypeDefiner.Define (node.Interfaces);

			return GetObject (type, bus_name, path);
		}

		//FIXME: debug hack
		~Connection ()
		{
			if (Protocol.Verbose)
				TypeDefiner.Save ();
		}
	}

	static class TypeDefiner
	{
		static AssemblyBuilder asmBdef;
		static ModuleBuilder modBdef;

		static void InitHack ()
		{
			if (asmBdef != null)
				return;

			asmBdef = AppDomain.CurrentDomain.DefineDynamicAssembly (new AssemblyName ("Defs"), AssemblyBuilderAccess.RunAndSave);
			//asmBdef = System.Threading.Thread.GetDomain ().DefineDynamicAssembly (new AssemblyName ("DefAssembly"), AssemblyBuilderAccess.RunAndSave);
			modBdef = asmBdef.DefineDynamicModule ("Defs.dll", "Defs.dll");
		}

		static uint ifaceId = 0;
		public static Type Define (Interface[] ifaces)
		{
			InitHack ();

			//Provide a unique interface name
			//This is a bit ugly
			string ifaceName = "Aggregate" + (ifaceId++);

			TypeBuilder typeB = modBdef.DefineType (ifaceName, TypeAttributes.Public | TypeAttributes.Interface);
			foreach (Interface iface in ifaces)
				typeB.AddInterfaceImplementation (Define (iface));

			return typeB.CreateType ();
		}

		static Type Define (Interface iface)
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

		const MethodAttributes ifaceMethAttr = MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;

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

				Type retType = outSig == Signature.Empty ? typeof (void) : outSig.ToType ();

				MethodBuilder method_builder = typeB.DefineMethod (declMethod.Name, ifaceMethAttr, retType, parms.ToArray ());

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

			if (iface.Properties != null)
			foreach (NDesk.DBus.Introspection.Property prop in iface.Properties) {
				Type propType = new Signature (prop.Type).ToType ();

				PropertyBuilder prop_builder = typeB.DefineProperty (prop.Name, PropertyAttributes.None, propType, Type.EmptyTypes);

				if (prop.Access == propertyAccess.read || prop.Access == propertyAccess.readwrite)
					prop_builder.SetGetMethod (typeB.DefineMethod ("get_" + prop.Name, ifaceMethAttr | MethodAttributes.SpecialName, propType, Type.EmptyTypes));

				if (prop.Access == propertyAccess.write || prop.Access == propertyAccess.readwrite)
					prop_builder.SetSetMethod (typeB.DefineMethod ("set_" + prop.Name, ifaceMethAttr | MethodAttributes.SpecialName, null, new Type[] {propType}));
			}

			if (iface.Signals != null)
			foreach (NDesk.DBus.Introspection.Signal signal in iface.Signals) {
				//Type eventType = typeof (EventHandler);
				Type eventType = typeof (VoidHandler);

				EventBuilder event_builder = typeB.DefineEvent (signal.Name, EventAttributes.None, eventType);

				event_builder.SetAddOnMethod (typeB.DefineMethod ("add_" + signal.Name, ifaceMethAttr | MethodAttributes.SpecialName, null, new Type[] {eventType}));

				event_builder.SetRemoveOnMethod (typeB.DefineMethod ("remove_" + signal.Name, ifaceMethAttr | MethodAttributes.SpecialName, null, new Type[] {eventType}));
			}

			//apply InterfaceAttribute
			ConstructorInfo interfaceAttributeCtor = typeof (InterfaceAttribute).GetConstructor(new Type[] {typeof (string)});

			CustomAttributeBuilder cab = new CustomAttributeBuilder (interfaceAttributeCtor, new object[] {iface.Name});

			typeB.SetCustomAttribute (cab);
		}
	}
}

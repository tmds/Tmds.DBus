// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Reflection;

namespace NDesk.DBus
{
	//TODO: complete this class
	public class Introspector
	{
		const string NAMESPACE = "http://www.freedesktop.org/standards/dbus";
		const string PUBLIC_IDENTIFIER = "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN";
		const string SYSTEM_IDENTIFIER = "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd";

		public StringBuilder sb;
		public string xml;
		public Type target_type = null;
		public ObjectPath root_path = ObjectPath.Root;
		public ObjectPath target_path = ObjectPath.Root;

		protected XmlWriter writer;

		public Introspector ()
		{
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = ("  ");
			settings.OmitXmlDeclaration = true;

			sb = new StringBuilder ();

			writer = XmlWriter.Create (sb, settings);
		}

		public void HandleIntrospect ()
		{
			writer.WriteDocType ("node", PUBLIC_IDENTIFIER, SYSTEM_IDENTIFIER, null);

			//TODO: write version info in a comment, when we get an AssemblyInfo.cs

			writer.WriteComment (" Never rely on XML introspection data for dynamic binding. It is provided only for convenience and is subject to change at any time. ");

			writer.WriteComment (" Warning: Intospection support is incomplete in this implementation ");

			writer.WriteComment (" This is the introspection result for ObjectPath: " + root_path + " ");

			//the root node element
			writer.WriteStartElement ("node");

			//FIXME: don't hardcode this stuff, do it properly!
			if (root_path.Value == "/") {
				writer.WriteStartElement ("node");
				writer.WriteAttributeString ("name", "org");
				writer.WriteEndElement ();
			}

			if (root_path.Value == "/org") {
				writer.WriteStartElement ("node");
				writer.WriteAttributeString ("name", "ndesk");
				writer.WriteEndElement ();
			}

			if (root_path.Value == "/org/ndesk") {
				writer.WriteStartElement ("node");
				writer.WriteAttributeString ("name", "test");
				writer.WriteEndElement ();
			}

			if (root_path.Value == target_path.Value) {
				WriteNodeBody ();
			}

			writer.WriteEndElement ();

			writer.Flush ();
			xml = sb.ToString ();
		}

		//public void WriteNode ()
		public void WriteNodeBody ()
		{
			//writer.WriteStartElement ("node");

			//TODO: non-well-known introspection has paths as well, which we don't do yet. read the spec again
			//hackishly just remove the root '/' to make the path relative for now
			//writer.WriteAttributeString ("name", target_path.Value.Substring (1));
			//writer.WriteAttributeString ("name", "test");

			//reflect our own interface manually
			WriteInterface (typeof (org.freedesktop.DBus.Introspectable));

			//reflect the target interface
			if (target_type != null) {
				WriteInterface (target_type);

				foreach (Type ifType in target_type.GetInterfaces ())
					WriteInterface (ifType);
			}

			//TODO: review recursion of interfaces and inheritance hierarchy

			//writer.WriteEndElement ();
		}

		public void WriteArg (ParameterInfo pi)
		{
			WriteArg (pi.ParameterType, Mapper.GetArgumentName (pi), pi.IsOut, false);
		}

		public void WriteArgReverse (ParameterInfo pi)
		{
			WriteArg (pi.ParameterType, Mapper.GetArgumentName (pi), pi.IsOut, true);
		}

		//TODO: clean up and get rid of reverse (or argIsOut) parm
		public void WriteArg (Type argType, string argName, bool argIsOut, bool reverse)
		{
			argType = argIsOut ? argType.GetElementType () : argType;
			if (argType == typeof (void))
				return;

			//FIXME: remove these special cases, they are just for testing
			if (argType.FullName == "GLib.Value")
				argType = typeof (object);
			if (argType.FullName == "GLib.GType")
				argType = typeof (Signature);

			writer.WriteStartElement ("arg");

			if (!String.IsNullOrEmpty (argName))
				writer.WriteAttributeString ("name", argName);

			//we can't rely on the default direction (qt-dbus requires a direction at time of writing), so we use a boolean to reverse the parameter direction and make it explicit

			if (argIsOut)
				writer.WriteAttributeString ("direction", !reverse ? "out" : "in");
			else
				writer.WriteAttributeString ("direction", !reverse ? "in" : "out");

			Signature sig = Signature.GetSig (argType);

			//FIXME: this hides the fact that there are invalid types coming up
			//sig.Value = sig.Value.Replace ((char)DType.Invalid, (char)DType.Variant);
			//sig.Value = sig.Value.Replace ((char)DType.Single, (char)DType.UInt32);

			//writer.WriteAttributeString ("type", Signature.GetSig (argType).Value);
			writer.WriteAttributeString ("type", sig.Value);

			writer.WriteEndElement ();
		}

		public void WriteMethod (MethodInfo mi)
		{
			writer.WriteStartElement ("method");
			writer.WriteAttributeString ("name", mi.Name);

			foreach (ParameterInfo pi in mi.GetParameters ())
				WriteArg (pi);

			//Mono <= 1.1.13 doesn't support MethodInfo.ReturnParameter, so avoid it
			//WriteArgReverse (mi.ReturnParameter);
			WriteArg (mi.ReturnType, Mapper.GetArgumentName (mi.ReturnTypeCustomAttributes, "ret"), false, true);

			writer.WriteEndElement ();
		}

		public void WriteProperty (PropertyInfo pri)
		{
			//expose properties as dbus properties
			writer.WriteStartElement ("property");
			writer.WriteAttributeString ("name", pri.Name);
			writer.WriteAttributeString ("type", Signature.GetSig (pri.PropertyType).Value);
			string access = (pri.CanRead ? "read" : String.Empty) + (pri.CanWrite ? "write" : String.Empty);
			writer.WriteAttributeString ("access", access);
			writer.WriteEndElement ();

			//expose properties as methods also
			//it may not be worth doing this in the long run
			/*
			if (pri.CanRead) {
				writer.WriteStartElement ("method");
				writer.WriteAttributeString ("name", "Get" + pri.Name);
				WriteArgReverse (pri.GetGetMethod ().ReturnParameter);
				writer.WriteEndElement ();
			}

			if (pri.CanWrite) {
				writer.WriteStartElement ("method");
				writer.WriteAttributeString ("name", "Set" + pri.Name);
				foreach (ParameterInfo pi in pri.GetSetMethod ().GetParameters ())
					WriteArg (pi);
				writer.WriteEndElement ();
			}
			*/
		}

		public void WriteSignal (EventInfo ei)
		{
			writer.WriteStartElement ("signal");
			writer.WriteAttributeString ("name", ei.Name);

			foreach (ParameterInfo pi in ei.EventHandlerType.GetMethod ("Invoke").GetParameters ())
				WriteArgReverse (pi);

			//no need to consider the delegate return value as dbus doesn't support it
			writer.WriteEndElement ();
		}

		const BindingFlags relevantBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		public void WriteInterface (Type type)
		{
			if (type == null)
				return;

			//TODO: this is unreliable, fix it
			if (!Mapper.IsPublic (type))
				return;

			writer.WriteStartElement ("interface");

			writer.WriteAttributeString ("name", Mapper.GetInterfaceName (type));

			/*
			foreach (MemberInfo mbi in type.GetMembers (relevantBindingFlags)) {
				switch (mbi.MemberType) {
					case MemberTypes.Method:
						if (!((MethodInfo)mbi).IsSpecialName)
							WriteMethod ((MethodInfo)mbi);
						break;
					case MemberTypes.Event:
						WriteSignal ((EventInfo)mbi);
						break;
					case MemberTypes.Property:
						WriteProperty ((PropertyInfo)mbi);
						break;
					default:
						Console.Error.WriteLine ("Warning: Unhandled MemberType '{0}' encountered while introspecting {1}", mbi.MemberType, type.FullName);
						break;
				}
			}
			*/

			foreach (MethodInfo mi in type.GetMethods (relevantBindingFlags))
				if (!mi.IsSpecialName)
					WriteMethod (mi);

			foreach (EventInfo ei in type.GetEvents (relevantBindingFlags))
				WriteSignal (ei);

			foreach (PropertyInfo pri in type.GetProperties (relevantBindingFlags))
				WriteProperty (pri);

			//TODO: indexers

			//TODO: attributes as annotations?

			writer.WriteEndElement ();

			//this recursion seems somewhat inelegant
			WriteInterface (type.BaseType);
		}

		public void WriteAnnotation (string name, string value)
		{
			writer.WriteStartElement ("annotation");

			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("value", value);

			writer.WriteEndElement ();
		}
	}
}

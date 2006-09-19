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
			//note that the name of the return value this way is 'ret' instead of 'data'
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
			WriteArg (pi, false);
		}

		public void WriteArgReverse (ParameterInfo pi)
		{
			WriteArg (pi, true);
		}

		public void WriteArg (ParameterInfo pi, bool reverse)
		{
			Type piType = pi.IsOut ? pi.ParameterType.GetElementType () : pi.ParameterType;
			if (piType == typeof (void))
				return;

			//FIXME: remove these special cases, they are just for testing
			if (piType.FullName == "GLib.Value")
				piType = typeof (object);
			if (piType.FullName == "GLib.GType")
				piType = typeof (Signature);

			writer.WriteStartElement ("arg");

			string piName;
			if (pi.IsRetval && String.IsNullOrEmpty (pi.Name))
				piName = "ret";
			else
				piName = pi.Name;

			if (!String.IsNullOrEmpty (piName))
				writer.WriteAttributeString ("name", piName);

			//we can't rely on the default direction (qt-dbus requires a direction at time of writing), so we use a boolean to reverse the parameter direction and make it explicit

			if (pi.IsOut)
				writer.WriteAttributeString ("direction", !reverse ? "out" : "in");
			else
				writer.WriteAttributeString ("direction", !reverse ? "in" : "out");

			Signature sig = Signature.GetSig (piType);

			//FIXME: this hides the fact that there are invalid types coming up
			//sig.Value = sig.Value.Replace ((char)DType.Invalid, (char)DType.Variant);
			//sig.Value = sig.Value.Replace ((char)DType.Single, (char)DType.UInt32);

			//writer.WriteAttributeString ("type", Signature.GetSig (piType).Value);
			writer.WriteAttributeString ("type", sig.Value);

			writer.WriteEndElement ();
		}

		public void WriteMethod (MethodInfo mi)
		{
			writer.WriteStartElement ("method");
			writer.WriteAttributeString ("name", mi.Name);

			foreach (ParameterInfo pi in mi.GetParameters ())
				WriteArg (pi);

			WriteArgReverse (mi.ReturnParameter);

			writer.WriteEndElement ();
		}

		public void WriteProperty (PropertyInfo pri)
		{
			//TODO: do properties in a tidier way?

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
			//TODO: consider member order, maybe using GetMembers ()

			writer.WriteStartElement ("interface");

			writer.WriteAttributeString ("name", Connection.GetInterfaceName (type));

			foreach (MethodInfo mi in type.GetMethods (relevantBindingFlags))
				if (!mi.IsSpecialName)
					WriteMethod (mi);

			foreach (EventInfo ei in type.GetEvents (relevantBindingFlags))
				WriteSignal (ei);

			foreach (PropertyInfo pri in type.GetProperties (relevantBindingFlags))
				WriteProperty (pri);

			//TODO: indexers

			writer.WriteEndElement ();

			//this recursion seems somewhat inelegant
			//if (type.BaseType != null && type.BaseType.IsMarshalByRef)
			if (type.BaseType != null && type.BaseType.IsSubclassOf (typeof (MarshalByRefObject)))
				WriteInterface (type.BaseType);
		}
	}
}

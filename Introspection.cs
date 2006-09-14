// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;

namespace NDesk.DBus
{
	//TODO: complete this class
	public class Introspector
	{
		public string xml;
		public Type target_type;

		public void HandleIntrospect ()
		{
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = ("\t");
			settings.OmitXmlDeclaration = true;

			StringBuilder sb = new StringBuilder ();

	    XmlWriter writer;
			//TODO: doctype
			writer = XmlWriter.Create (sb, settings);
			writer.WriteStartElement ("node");
			writer.WriteStartElement ("interface");
			writer.WriteAttributeString ("name", "org.freedesktop.DBus.Introspectable");
			writer.WriteStartElement ("method");
			writer.WriteAttributeString ("name", "Introspect");
			writer.WriteStartElement ("arg");
			writer.WriteAttributeString ("name", "data");
			writer.WriteAttributeString ("direction", "out");
			writer.WriteAttributeString ("type", "s");
			writer.WriteEndElement ();
			writer.WriteEndElement ();
			writer.WriteEndElement ();
			writer.WriteEndElement ();

			writer.Flush ();
			xml = sb.ToString ();
		}
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NDesk.DBus;
using Schemas;

public class test
{
	public static void Main (string[] args)
	{
		string fname = args[0];
		StreamReader sr = new StreamReader (fname);
		XmlSerializer sz = new XmlSerializer (typeof (Interface));
		Interface iface = (Interface)sz.Deserialize (sr);

		foreach (object o in iface.Items) {
			if (o is Property) {
				Property prop = (Property)o;
				Console.WriteLine (prop.Name);
			}

			if (o is Method) {
				Method meth = (Method)o;
				Console.Write ("void " + meth.Name);
				Console.Write (" (");
				foreach (Argument arg in meth.Arguments)
					Console.Write ("[" + arg.Direction + "] " + arg.Type + " " + arg.Name + ", ");
				Console.Write (");");
				Console.WriteLine ();
			}
		}
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace NDesk.DBus.Introspection
{
	[XmlRootAttribute(IsNullable=true)]
	public class Member {
		[XmlAttributeAttribute("name")]
		public string Name;
	}

	[XmlRootAttribute("node", IsNullable=true)]
	public class Node {

		[XmlAttributeAttribute("name")]
		public string Name;

		[XmlElementAttribute("interface", Type=typeof(@Interface))]
		public Interface[] Interfaces;
		[XmlElementAttribute("node", Type=typeof(Node))]
		public Node[] Nodes;
	}

	[XmlRootAttribute("interface", IsNullable=true)]
	public class @Interface {

		[XmlAttributeAttribute("name")]
		public string Name;

		/*
		[XmlElementAttribute("method", Type=typeof(Method))]
		[XmlElementAttribute("signal", Type=typeof(Signal))]
		[XmlElementAttribute("property", Type=typeof(Property))]
		//[XmlElementAttribute("annotation", Type=typeof(Annotation))]
		//public Member[] Members;
		*/

		[XmlElementAttribute("method", Type=typeof(Method))]
		public Method[] Methods;

		[XmlElementAttribute("signal", Type=typeof(Signal))]
		public Signal[] Signals;

		[XmlElementAttribute("property", Type=typeof(Property))]
		public Property[] Properties;
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Method : Member {

		/*
		[XmlElementAttribute("arg", Type=typeof(Argument))]
		[XmlElementAttribute("annotation", Type=typeof(Annotation))]
		public object[] Items;
		*/

		//[System.ComponentModel.DefaultValue(new Argument[0])]
		[XmlElementAttribute("arg", Type=typeof(Argument))]
		//public List<Argument> Arguments;
		public Argument[] Arguments;
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Argument {

		[XmlAttributeAttribute("name")]
		public string Name = String.Empty;

		[XmlAttributeAttribute("type")]
		public string Type;

		[System.ComponentModel.DefaultValue(ArgDirection.@in)]
		[XmlAttributeAttribute("direction")]
		public ArgDirection Direction = ArgDirection.@in;
	}

	public enum ArgDirection {
		@in,
		@out,
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Annotation {

		[XmlAttributeAttribute("name")]
		public string Name = String.Empty;

		[XmlAttributeAttribute("value")]
		public string Value = String.Empty;
	}

	[XmlRootAttribute("signal", IsNullable=true)]
	public class Signal : Method {
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Property : Member {
		[XmlAttributeAttribute("type")]
		public string Type = String.Empty;

		[XmlAttributeAttribute("access")]
		public propertyAccess Access;

		[XmlElementAttribute("annotation")]
		public Annotation[] Annotations;
	}

	public enum propertyAccess {
		read,
		write,
		readwrite,
	}
}

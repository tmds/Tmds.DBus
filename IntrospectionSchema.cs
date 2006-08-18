// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System.Xml.Serialization;
using System.Collections.Generic;

namespace Schemas {

	[XmlRootAttribute(IsNullable=true)]
	public class Member {
		[XmlAttributeAttribute("name")]
		public string Name;
	}

	/*
	public class Node {

		[XmlAttributeAttribute("name")]
		public string Name;

		[XmlElementAttribute("node", Type=typeof(Node))]
		[XmlElementAttribute("interface", Type=typeof(@Interface))]
		public object[] Items;
	}
	*/

	[XmlRootAttribute("interface", IsNullable=true)]
	public class @Interface {

		[XmlAttributeAttribute("name")]
		public string Name;

		[XmlElementAttribute("method", Type=typeof(Method))]
		[XmlElementAttribute("signal", Type=typeof(Signal))]
		[XmlElementAttribute("property", Type=typeof(Property))]
		[XmlElementAttribute("annotation", Type=typeof(Annotation))]
		public object[] Items;
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Method : Member {

		/*
		[XmlElementAttribute("arg", Type=typeof(Argument))]
		[XmlElementAttribute("annotation", Type=typeof(Annotation))]
		public object[] Items;
		*/

		[XmlElementAttribute("arg", Type=typeof(Argument))]
		public List<Argument> Arguments;
		//public Argument[] Arguments;
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Argument {

		[XmlAttributeAttribute("name")]
		public string Name;

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
		public string Name;

		[XmlAttributeAttribute("value")]
		public string Value;
	}

	[XmlRootAttribute("signal", IsNullable=true)]
	public class Signal : Member {
		[XmlElementAttribute("arg", Type=typeof(Argument))]
		[XmlElementAttribute("annotation", Type=typeof(Annotation))]
		public object[] Items;
	}

	[XmlRootAttribute(IsNullable=true)]
	public class Property : Member {
		[XmlAttributeAttribute("type")]
		public string Type;

		[XmlAttributeAttribute("access")]
		public propertyAccess Access;

		[XmlElementAttribute("annotation")]
		public Annotation[] Annotation;
	}

	public enum propertyAccess {
		read,
		write,
		readwrite,
	}
}

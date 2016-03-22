// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace DBus
{
	public sealed class ObjectPath : IComparable, IComparable<ObjectPath>, IEquatable<ObjectPath>
	{
		public static readonly ObjectPath Root = new ObjectPath ("/");

		internal readonly string Value;

		public ObjectPath (string value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			Validate (value);

			this.Value = value;
		}

		static void Validate (string value)
		{
			if (!value.StartsWith ("/"))
				throw new ArgumentException ("value");
			if (value.EndsWith ("/") && value.Length > 1)
				throw new ArgumentException ("ObjectPath cannot end in '/'");

			bool multipleSlash = false;

			foreach (char c in value) {
				bool valid = (c >= 'a' && c <='z')
					|| (c >= 'A' && c <= 'Z')
					|| (c >= '0' && c <= '9')
					|| c == '_'
					|| (!multipleSlash && c == '/');

				if (!valid) {
					var message = string.Format ("'{0}' is not a valid character in an ObjectPath", c);
					throw new ArgumentException (message, "value");
				}

				multipleSlash = c == '/';
			}

		}

		public int CompareTo (ObjectPath other)
		{
			if (other == null)
				return 1;

			return Value.CompareTo (other.Value);
		}

		public int CompareTo (object otherObject)
		{
			ObjectPath other = otherObject as ObjectPath;

			if (other == null)
				return 1;

			return Value.CompareTo (other.Value);
		}

		public bool Equals (ObjectPath other)
		{
			if (other == null)
				return false;

			return Value == other.Value;
		}

		public override bool Equals (object o)
		{
			ObjectPath b = o as ObjectPath;

			if (b == null)
				return false;

			return Value.Equals (b.Value);
		}

		public static bool operator == (ObjectPath a, ObjectPath b)
		{
			object aa = a, bb = b;
			if (aa == null && bb == null)
				return true;

			if (aa == null || bb == null)
				return false;

			return a.Value == b.Value;
		}

		public static bool operator != (ObjectPath a, ObjectPath b)
		{
			return !(a == b);
		}

		public override int GetHashCode ()
		{
			return Value.GetHashCode ();
		}

		public override string ToString ()
		{
			return Value;
		}

		//this may or may not prove useful
		internal string[] Decomposed
		{
			get {
				return Value.Split (new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
			/*
			} set {
				Value = String.Join ("/", value);
			*/
			}
		}

		internal ObjectPath Parent
		{
			get {
				if (Value == Root.Value)
					return null;

				string par = Value.Substring (0, Value.LastIndexOf ('/'));
				if (par == String.Empty)
					par = "/";

				return new ObjectPath (par);
			}
		}

		/*
		public int CompareTo (object value)
		{
			return 1;
		}

		public int CompareTo (ObjectPath value)
		{
			return 1;
		}

		public bool Equals (ObjectPath value)
		{
			return false;
		}
		*/
	}
}

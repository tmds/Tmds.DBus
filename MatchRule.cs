// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;

namespace NDesk.DBus
{
	//delegate void MessageHandler (Message msg);

	class MatchRule
	{
		public MessageType? MessageType;
		public string Interface;
		public string Member;
		public ObjectPath Path;
		public string Sender;
		public string Destination;
		//TODO: args
		//public Signature Signature;

		public MatchRule ()
		{
		}

		void Append (StringBuilder sb, string key, string value)
		{
			if (sb.Length != 0)
				sb.Append (",");

			sb.Append (key + "='");
			sb.Append (value);
			sb.Append ("'");
		}

		/*
		void AppendArg (StringBuilder sb, int index, string value)
		{
			Append (sb, "arg" + index, value);
		}
		*/

		public override bool Equals (object o)
		{
			MatchRule r = o as MatchRule;

			if (r == null)
				return false;

			if (r.MessageType != MessageType)
				return false;

			if (r.Interface != Interface)
				return false;

			if (r.Member != Member)
				return false;

			//TODO: see why path comparison doesn't work
			if (r.Path.Value != Path.Value)
			//if (r.Path != Path)
				return false;

			if (r.Sender != Sender)
				return false;

			if (r.Destination != Destination)
				return false;

			return true;
		}

		public override int GetHashCode ()
		{
			//FIXME: not at all optimal
			return ToString ().GetHashCode ();
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();

			if (MessageType != null)
				Append (sb, "type", MessageFilter.MessageTypeToString ((MessageType)MessageType));

			if (Interface != null)
				Append (sb, "interface", Interface);

			if (Member != null)
				Append (sb, "member", Member);

			if (Path != null)
				//Append (sb, "path", Path.ToString ());
				Append (sb, "path", Path.Value);

			if (Sender != null)
				Append (sb, "sender", Sender);

			if (Destination != null)
				Append (sb, "destination", Destination);

			return sb.ToString ();
		}

		//this is useful as a Predicate<Message> delegate
		public bool Matches (Message msg)
		{
			if (MessageType != null)
				if (msg.Header.MessageType != MessageType)
					return false;

			object value;

			if (Interface != null)
				if (msg.Header.Fields.TryGetValue (FieldCode.Interface, out value))
					if ((string)value != Interface)
						return false;

			if (Member != null)
				if (msg.Header.Fields.TryGetValue (FieldCode.Member, out value))
					if ((string)value != Member)
						return false;

			if (Path != null)
				if (msg.Header.Fields.TryGetValue (FieldCode.Path, out value))
					//if ((ObjectPath)value != Path)
					if (((ObjectPath)value).Value != Path.Value)
						return false;

			if (Sender != null)
				if (msg.Header.Fields.TryGetValue (FieldCode.Sender, out value))
					if ((string)value != Sender)
						return false;

			if (Destination != null)
				if (msg.Header.Fields.TryGetValue (FieldCode.Destination, out value))
					if ((string)value != Destination)
						return false;

			return true;
		}

		//this could be made more efficient
		public static MatchRule Parse (string text)
		{
			MatchRule r = new MatchRule ();

			foreach (string propStr in text.Split (',')) {
				string[] parts = propStr.Split ('=');

				if (parts.Length < 2)
					throw new Exception ("No equals sign found");
				if (parts.Length > 2)
					throw new Exception ("Too many equals signs found");

				string key = parts[0].Trim ();
				string value = parts[1].Trim ();

				if (!value.StartsWith ("'") || !value.EndsWith ("'"))
					throw new Exception ("Too many equals signs found");

				value = value.Substring (1, value.Length - 2);

				//TODO: more consistent error handling
				switch (key) {
					case "type":
						if (r.MessageType != null)
							return null;
						r.MessageType = MessageFilter.StringToMessageType (value);
						break;
					case "interface":
						if (r.Interface != null)
							return null;
						r.Interface = value;
						break;
					case "member":
						if (r.Member != null)
							return null;
						r.Member = value;
						break;
					case "path":
						if (r.Path != null)
							return null;
						r.Path = new ObjectPath (value);
						break;
					case "sender":
						if (r.Sender != null)
							return null;
						r.Sender = value;
						break;
					case "destination":
						if (r.Destination != null)
							return null;
						r.Destination = value;
						break;
				}
			}

			return r;
		}
	}
}

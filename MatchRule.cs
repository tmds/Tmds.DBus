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

		//TODO: parsing

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
	}
}

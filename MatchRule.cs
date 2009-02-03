// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace NDesk.DBus
{
	class MatchRule
	{
		public MessageType? MessageType;
		public string Interface;
		public string Member;
		public ObjectPath Path;
		public string Sender;
		public string Destination;
		public readonly SortedList<int,MatchTest> Args = new SortedList<int,MatchTest> ();

		public MatchRule ()
		{
		}

		static void Append (StringBuilder sb, string key, string value)
		{
			if (sb.Length != 0)
				sb.Append (',');

			sb.Append (key);
			sb.Append ("='");
			sb.Append (value.Replace (@"\", @"\\").Replace (@"'", @"\'"));
			sb.Append ('\'');
		}

		static void AppendArg (StringBuilder sb, int index, string value)
		{
			Append (sb, "arg" + index, value);
		}

		static void AppendPathArg (StringBuilder sb, int index, ObjectPath value)
		{
			Append (sb, "arg" + index + "path", value.ToString ());
		}

		public override bool Equals (object o)
		{
			MatchRule r = o as MatchRule;
			if (o == null)
				return false;

			return ToString () == r.ToString ();

			/*
			MatchRule r = o as MatchRule;

			if (r == null)
				return false;

			if (r.MessageType != MessageType)
				return false;

			if (r.Interface != Interface)
				return false;

			if (r.Member != Member)
				return false;

			if (r.Path != Path)
				return false;

			if (r.Sender != Sender)
				return false;

			if (r.Destination != Destination)
				return false;

			//FIXME: do args
			if (r.Args.Count > 0 || Args.Count > 0)
				return false;

			return true;
			*/
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

			if (Args != null)
				foreach (KeyValuePair<int,MatchTest> pair in Args)
					if (pair.Value.Signature == Signature.StringSig)
						AppendArg (sb, pair.Key, (string)pair.Value.Value);
					else if (pair.Value.Signature == Signature.ObjectPathSig)
						AppendPathArg (sb, pair.Key, (ObjectPath)pair.Value.Value);

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
				if (!msg.Header.Fields.TryGetValue (FieldCode.Interface, out value) || ((string)value != Interface))
					return false;

			if (Member != null)
				if (!msg.Header.Fields.TryGetValue (FieldCode.Member, out value) || (string)value != Member)
					return false;

			if (Path != null)
				if (!msg.Header.Fields.TryGetValue (FieldCode.Path, out value) || (ObjectPath)value != Path)
					return false;

			if (Sender != null)
				if (!msg.Header.Fields.TryGetValue (FieldCode.Sender, out value) || (string)value != Sender)
					return false;

			if (Destination != null)
				if (!msg.Header.Fields.TryGetValue (FieldCode.Destination, out value) || (string)value != Destination)
					return false;

			if (Args != null && Args.Count > 0) {
				if (msg.Signature == Signature.Empty || msg.Body == null)
					return false;

				int topArgNum = Args.Keys[Args.Count - 1];
				if (topArgNum >= Protocol.MaxMatchRuleArgs)
					return false;

				List<Signature> sigs = new List<Signature> ();
				sigs.AddRange (msg.Signature.GetParts ());
				if (topArgNum >= sigs.Count)
					return false;

				// Spec (0.12) says that only strings can be matched
				// But later, path matching was added
				foreach (KeyValuePair<int,MatchTest> arg in Args)
					if (sigs[arg.Key] != arg.Value.Signature)
						return false;

				MessageReader reader = new MessageReader (msg);

				for (int argNum = 0 ; argNum <= topArgNum ; argNum++) {
					Signature sig = sigs[argNum];

					MatchTest test;
					if (Args.TryGetValue (argNum, out test)) {
						if (sig != test.Signature)
							return false;
						if (test.Signature == Signature.StringSig)
							if (reader.ReadString () != (string)test.Value)
								return false;
						if (test.Signature == Signature.ObjectPathSig)
							if (reader.ReadObjectPath () != (ObjectPath)test.Value)
								return false;
						continue;
					}

					// FIXME: Need to support skipping complex message parts
					if (!sig.IsPrimitive)
						return false;

					// Read and discard primitive values to skip over them
					reader.ReadValue (sig[0]);
				}
			}

			return true;
		}

		static Regex argNRegex = new Regex (@"^arg(\d+)(path)?$");
		static Regex matchRuleRegex = new Regex (@"(\w+)\s*=\s*'((\\\\|\\'|[^'\\])*)'", RegexOptions.Compiled);
		public static MatchRule Parse (string text)
		{
			if (text.Length > Protocol.MaxMatchRuleLength)
				throw new Exception ("Match rule length exceeds maximum " + Protocol.MaxMatchRuleLength + " characters");

			MatchRule r = new MatchRule ();

			// TODO: Stricter validation. Tighten up the regex.
			// It currently succeeds and silently drops malformed test parts.

			for (Match m = matchRuleRegex.Match (text) ; m.Success ; m = m.NextMatch ()) {
				string key = m.Groups[1].Value;
				string value = m.Groups[2].Value;
				// This unescaping may not be perfect..
				value = value.Replace (@"\\", @"\");
				value = value.Replace (@"\'", @"'");

				if (key.StartsWith ("arg")) {
					Match mArg = argNRegex.Match (key);
					if (!mArg.Success)
						return null;
					int argNum = (int)UInt32.Parse (mArg.Groups[1].Value);

					if (argNum < 0 || argNum >= Protocol.MaxMatchRuleArgs)
						throw new Exception ("arg match must be between 0 and " + (Protocol.MaxMatchRuleArgs - 1) + " inclusive");

					if (r.Args.ContainsKey (argNum))
						return null;

					string argType = mArg.Groups[2].Value;

					if (argType == "path")
						r.Args[argNum] = new MatchTest (new ObjectPath (value));
					else
						r.Args[argNum] = new MatchTest (value);

					continue;
				}

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
					default:
						if (Protocol.Verbose)
							Console.Error.WriteLine ("Warning: Unrecognized match rule key: " + key);
						break;
				}
			}

			return r;
		}
	}

	struct MatchTest
	{
		public Signature Signature;
		public object Value;

		public MatchTest (string value)
		{
			Signature = Signature.StringSig;
			Value = value;
		}

		public MatchTest (ObjectPath value)
		{
			Signature = Signature.ObjectPathSig;
			Value = value;
		}
	}
}

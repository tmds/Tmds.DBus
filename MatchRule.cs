// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DBus.Protocol
{
	public class MatchRule
	{
		static readonly Regex argNRegex = new Regex (@"^arg(\d+)(path)?$");
		static readonly Regex matchRuleRegex = new Regex (@"(\w+)\s*=\s*'((\\\\|\\'|[^'\\])*)'", RegexOptions.Compiled);

		MessageType messageType = MessageType.All;
		readonly SortedList<FieldCode,MatchTest> fields = new SortedList<FieldCode,MatchTest> ();
		readonly HashSet<ArgMatchTest> args = new HashSet<ArgMatchTest> ();

		public MatchRule ()
		{
		}

		public HashSet<ArgMatchTest> Args {
			get {
				return this.args;
			}
		}

		public SortedList<FieldCode, MatchTest> Fields {
			get {
				return this.fields;
			}
		}

		public MessageType MessageType {
			get {
				return this.messageType;
			}
			set {
				this.messageType = value;
			}
		}

		static void Append (StringBuilder sb, string key, object value)
		{
			Append (sb, key, value.ToString ());
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
		}

		public override int GetHashCode ()
		{
			//FIXME: not at all optimal
			return ToString ().GetHashCode ();
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();

			if (MessageType != MessageType.All)
				Append (sb, "type", MessageFilter.MessageTypeToString ((MessageType)MessageType));

			// Note that fdo D-Bus daemon sorts in a different order.
			// It shouldn't matter though as long as we're consistent.
			foreach (KeyValuePair<FieldCode,MatchTest> pair in Fields) {
				Append (sb, pair.Key.ToString ().ToLower (), pair.Value.Value);
			}

			// Sorting the list here is not ideal
			List<ArgMatchTest> tests = new List<ArgMatchTest> (Args);
			tests.Sort ( delegate (ArgMatchTest aa, ArgMatchTest bb) { return aa.ArgNum - bb.ArgNum; } );

			if (Args != null)
				foreach (ArgMatchTest test in tests)
					if (test.Signature == Signature.StringSig)
						AppendArg (sb, test.ArgNum, (string)test.Value);
					else if (test.Signature == Signature.ObjectPathSig)
						AppendPathArg (sb, test.ArgNum, (ObjectPath)test.Value);

			return sb.ToString ();
		}

		public static void Test (HashSet<ArgMatchTest> a, Message msg)
		{
			List<Signature> sigs = new List<Signature> ();
			sigs.AddRange (msg.Signature.GetParts ());

			if (sigs.Count == 0) {
				a.Clear ();
				return;
			}

			a.RemoveWhere ( delegate (ArgMatchTest t) { return t.ArgNum >= sigs.Count || t.Signature != sigs[t.ArgNum]; } );

			// Sorting the list here is not ideal
			List<ArgMatchTest> tests = new List<ArgMatchTest> (a);
			tests.Sort ( delegate (ArgMatchTest aa, ArgMatchTest bb) { return aa.ArgNum - bb.ArgNum; } );

			if (tests.Count == 0) {
				a.Clear ();
				return;
			}

			MessageReader reader = new MessageReader (msg);

			int argNum = 0;
			foreach (ArgMatchTest test in tests) {
				if (argNum > test.ArgNum) {
					// This test cannot pass because a previous test already did.
					// So we already know it will fail without even trying.
					// This logic will need to be changed to support wildcards.
					a.Remove (test);
					continue;
				}

				while (argNum != test.ArgNum) {
					Signature sig = sigs[argNum];
					if (!reader.StepOver (sig))
						throw new Exception ();
					argNum++;
				}

				// TODO: Avoid re-reading values
				if (!reader.PeekValue (test.Signature[0]).Equals (test.Value)) {
					a.Remove (test);
					continue;
				}

				argNum++;
			}
		}

		public bool MatchesHeader (Message msg)
		{
			if (MessageType != MessageType.All)
				if (msg.Header.MessageType != MessageType)
					return false;

			foreach (KeyValuePair<FieldCode,MatchTest> pair in Fields) {
				object value;
				if (!msg.Header.TryGetField (pair.Key, out value))
					return false;
				if (!pair.Value.Value.Equals (value))
					return false;
			}

			return true;
		}

		public static MatchRule Parse (string text)
		{
			if (text.Length > ProtocolInformation.MaxMatchRuleLength)
				throw new Exception ("Match rule length exceeds maximum " + ProtocolInformation.MaxMatchRuleLength + " characters");

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

					if (argNum < 0 || argNum >= ProtocolInformation.MaxMatchRuleArgs)
						throw new Exception ("arg match must be between 0 and " + (ProtocolInformation.MaxMatchRuleArgs - 1) + " inclusive");

					//if (r.Args.ContainsKey (argNum))
					//	return null;

					string argType = mArg.Groups[2].Value;

					if (argType == "path")
						r.Args.Add (new ArgMatchTest (argNum, new ObjectPath (value)));
					else
						r.Args.Add (new ArgMatchTest (argNum, value));

					continue;
				}

				//TODO: more consistent error handling
				switch (key) {
					case "type":
						if (r.MessageType != MessageType.All)
							return null;
						r.MessageType = MessageFilter.StringToMessageType (value);
						break;
					case "interface":
						r.Fields[FieldCode.Interface] = new MatchTest (value);
						break;
					case "member":
						r.Fields[FieldCode.Member] = new MatchTest (value);
						break;
					case "path":
						r.Fields[FieldCode.Path] = new MatchTest (new ObjectPath (value));
						break;
					case "sender":
						r.Fields[FieldCode.Sender] = new MatchTest (value);
						break;
					case "destination":
						r.Fields[FieldCode.Destination] = new MatchTest (value);
						break;
					default:
						if (ProtocolInformation.Verbose)
							Console.Error.WriteLine ("Warning: Unrecognized match rule key: " + key);
						break;
				}
			}

			return r;
		}
	}

	public class HeaderTest : MatchTest
	{
		public FieldCode Field;
		public HeaderTest (FieldCode field, object value)
		{
			Field = field;
			Signature = Signature.GetSig (value.GetType ());
			Value = value;
		}
	}

	public struct ArgMatchTest
	{
		public int ArgNum;
		public Signature Signature;
		public object Value;

		public ArgMatchTest (int argNum, string value)
		{
			ArgNum = argNum;
			Signature = Signature.StringSig;
			Value = value;
		}

		public ArgMatchTest (int argNum, ObjectPath value)
		{
			ArgNum = argNum;
			Signature = Signature.ObjectPathSig;
			Value = value;
		}

		public override int GetHashCode ()
		{
			return Signature.GetHashCode () ^ Value.GetHashCode () ^ ArgNum;
		}
	}

	/*
	class ArgMatchTest : MatchTest
	{
		public int ArgNum;

		public ArgMatchTest (int argNum, string value) : base (value)
		{
			ArgNum = argNum;
		}

		public ArgMatchTest (int argNum, ObjectPath value) : base (value)
		{
			ArgNum = argNum;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode () ^ ArgNum;
		}
	}
	*/

	public class MatchTest
	{
		public Signature Signature;
		public object Value;

		public override int GetHashCode ()
		{
			return Signature.GetHashCode () ^ Value.GetHashCode ();
		}

		protected MatchTest ()
		{
		}

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

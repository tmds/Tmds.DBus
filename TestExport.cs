// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestExport
{
	public static void Main ()
	{
		Connection conn = new Connection ();

		//begin ugly bits
		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetObject<Bus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);


		string myNameReq = "org.ndesk.test";

		DemoObject demo;

		if (bus.NameHasOwner (myNameReq)) {
			demo = conn.GetObject<DemoObject> (myNameReq, opath);
		} else {
			NameReply nameReply = bus.RequestName (myNameReq, NameFlag.None);

			Console.WriteLine ("nameReply: " + nameReply);

			demo = new DemoObject ();
			conn.RegisteredObjects["org.ndesk.test"] = demo;

			while (true)
				conn.Iterate ();
		}
		//end ugly bits

		demo.Say ("Hello world!");
		Console.WriteLine (demo.EchoCaps ("foo bar"));
		Console.WriteLine (demo.GetEnum ());
		demo.CheckEnum (DemoEnum.Bar);
		demo.CheckEnum (demo.GetEnum ());

		Console.WriteLine ();
		string[] texts = {"one", "two", "three"};
		texts = demo.EchoCapsArr (texts);
		foreach (string text in texts)
			Console.WriteLine (text);

		Console.WriteLine ();
		int[] vals = demo.TextToInts ("1 2 3");
		foreach (int val in vals)
			Console.WriteLine (val);

		Console.WriteLine ();
		MyTuple fooTuple = demo.GetTuple ();
		Console.WriteLine ("A: " + fooTuple.A);
		Console.WriteLine ("B: " + fooTuple.B);

		Console.WriteLine ();
		//KeyValuePair<string,string>[] kvps = demo.GetDict ();
		IDictionary<string,string> dict = demo.GetDict ();
		foreach (KeyValuePair<string,string> kvp in dict)
			Console.WriteLine (kvp.Key + ": " + kvp.Value);
	}
}

[Interface ("org.ndesk.test")]
public class DemoObject : MarshalByRefObject
{
	public void Say (string text)
	{
		Console.WriteLine (text);
	}

	public string EchoCaps (string text)
	{
		return text.ToUpper ();
	}

	public void CheckEnum (DemoEnum e)
	{
		Console.WriteLine (e);
	}

	public DemoEnum GetEnum ()
	{
		return DemoEnum.Bar;
	}

	public string[] EchoCapsArr (string[] texts)
	{
		string[] retTexts = new string[texts.Length];

		for (int i = 0 ; i != texts.Length ; i++)
			retTexts[i] = texts[i].ToUpper ();

		return retTexts;
	}

	public int[] TextToInts (string text)
	{
		string[] parts = text.Split (' ');
		int[] rets = new int[parts.Length];

		for (int i = 0 ; i != parts.Length ; i++)
			rets[i] = Int32.Parse (parts[i]);

		return rets;
	}

	public MyTuple GetTuple ()
	{
		MyTuple tup;

		tup.A = "alpha";
		tup.B = "beta";

		return tup;
	}

	public IDictionary<string,string> GetDict ()
	{
		Dictionary<string,string> dict = new Dictionary<string,string> ();

		dict["one"] = "1";
		dict["two"] = "2";

		return dict;
	}

	/*
	public KeyValuePair<string,string>[] GetDict ()
	{
		KeyValuePair<string,string>[] rets = new KeyValuePair<string,string>[2];

		//rets[0] = new KeyValuePair<string,string> ("one", "1");
		//rets[1] = new KeyValuePair<string,string> ("two", "2");

		rets[0] = new KeyValuePair<string,string> ("second", " from example-service.py");
		rets[1] = new KeyValuePair<string,string> ("first", "Hello Dict");

		return rets;
	}
	*/
}

public enum DemoEnum
{
	Foo,
	Bar,
}


public struct MyTuple
{
	public string A;
	public string B;
}

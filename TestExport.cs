// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
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

		DProxy prox = new DProxy (conn, opath, name, typeof (Bus));
		Bus bus = (Bus)prox.GetTransparentProxy ();

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);


		string myNameReq = "org.ndesk.test";

		DemoObject demo;

		if (bus.NameHasOwner (myNameReq)) {
			DProxy prox2 = new DProxy (conn, opath, myNameReq, typeof (DemoObject));
			demo = (DemoObject)prox2.GetTransparentProxy ();
		} else {
			NameReply nameReply = bus.RequestName (myNameReq, NameFlag.None);

			Console.WriteLine ("nameReply: " + nameReply);

			demo = new DemoObject ();
			conn.RegisteredObjects["org.ndesk.test"] = demo;

			conn.WaitForReplyTo (0);
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
	}
}

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
}

public enum DemoEnum
{
	Foo,
	Bar,
}

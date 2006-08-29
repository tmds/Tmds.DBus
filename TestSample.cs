// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestSample
{
	public static void Main ()
	{
		Connection conn = new Connection ();
		conn.RegisteredTypes[typeof (SampleInterface)] = "org.designfu.SampleInterface";
		conn.RegisteredTypes[typeof (Bus)] = "org.freedesktop.DBus";
		conn.RegisteredTypes[typeof (Introspectable)] = "org.freedesktop.DBus.Introspectable";

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		DProxy prox = new DProxy (conn, opath, name, typeof (Bus));
		Bus bus = (Bus)prox.GetTransparentProxy ();

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);

		DProxy prox2 = new DProxy (conn, new ObjectPath ("/SomeObject"), "org.designfu.SampleInterface", "org.designfu.SampleService", typeof (SampleInterface));
		SampleInterface sample = (SampleInterface)prox2.GetTransparentProxy ();

		Console.WriteLine ();
		string xmlData = sample.Introspect ();
		Console.WriteLine ("xmlData: " + xmlData);

		//object obj = sample.HelloWorld ("Hello from example-client.py!");
		string[] vals = sample.HelloWorld ("Hello from example-client.py!");
		foreach (string val in vals)
			Console.WriteLine (val);

		Console.WriteLine ();
		MyTuple tup = sample.GetTuple ();
		Console.WriteLine (tup.A);
		Console.WriteLine (tup.B);

		Console.WriteLine ();
		//IDictionary<string,string> dict = sample.GetDict ();
		//DictEntry<string,string>[] dict = sample.GetDict ();
		KeyValuePair<string,string>[] dict = sample.GetDict ();
		foreach (KeyValuePair<string,string> pair in dict)
			Console.WriteLine (pair.Key + ": " + pair.Value);
	}
}

public interface SampleInterface : Introspectable
{
	//void HelloWorld (object hello_message);
	//object HelloWorld (object hello_message);
	string[] HelloWorld (object hello_message);
	MyTuple GetTuple ();
	//IDictionary<string,string> GetDict ();
	//DictEntry<string,string>[] GetDict ();
	KeyValuePair<string,string>[] GetDict ();
}

//(ss)
public struct MyTuple
{
	public string A;
	public string B;
}

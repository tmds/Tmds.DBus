// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace NDesk.DBus
{
	//TODO: complete and use these wrapper classes
	//not sure exactly what I'm thinking but there seems to be sense here

	public class MethodCall
	{
		public ObjectPath Path = new ObjectPath ("");
		public string Interface = "";
		public string Member = "";
		//public string Destination = "";
		//public string Sender = "";
		//public Signature Signature = new Signature ("");
	}

	public class MethodReturn
	{
		public uint ReplySerial = 0;
	}

	public class Error
	{
		public string ErrorName = "";
		public uint ReplySerial = 0;
	}

	public class Signal
	{
		public ObjectPath Path = new ObjectPath ("");
		public string Interface = "";
		public string Member = "";
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

using System.Runtime.InteropServices;

using Mono.Unix;
using Mono.Unix.Native;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	public enum ClientState
	{
		WaitingForData,
		WaitingForOK,
		WaitingForReject,
	}

	public enum ServerState
	{
		WaitingForAuth,
		WaitingForData,
		WaitingForBegin,
	}

	public partial class Connection
	{
		public void Authenticate ()
		{
			//NetworkStream ns = new NetworkStream (sock);
			//UnixStream ns = new UnixStream ((int)sock.Handle);
			StreamReader sr = new StreamReader (ns, System.Text.Encoding.ASCII);
			StreamWriter sw = new StreamWriter (ns, System.Text.Encoding.ASCII);

			sw.NewLine = "\r\n";
			//sw.AutoFlush = true;

			sw.Write ('\0');

			/*
			sw.WriteLine ("AUTH");
			sw.Flush ();

			Console.WriteLine (sr.ReadLine ());
			*/

			sw.WriteLine ("AUTH EXTERNAL 31303030");
			sw.Flush ();

			string ok_rep = sr.ReadLine ();
			//Console.WriteLine (ok_rep);

			string[] parts;
			parts = ok_rep.Split (' ');

			string guid = parts[1];
			Console.WriteLine ("guid: " + guid);

			sw.WriteLine ("BEGIN");
			sw.Flush ();
		}
	}
}

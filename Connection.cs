// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

using System.Runtime.InteropServices;

using Mono.Unix;
using Mono.Unix.Native;

//using Console = System.Diagnostics.Trace;

namespace NDesk.DBus
{
	public partial class Connection
	{
		const string SYSTEM_BUS = "/var/run/dbus/system_bus_socket";

		public Socket sock = null;
		Stream ns = null;

		public Connection ()
		{
			string sessAddr = System.Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");
			bool abstr;
			string path;

			Address.Parse (sessAddr, out abstr, out path);

			//not really correct
			if (path == null)
				path = SYSTEM_BUS;

			if (abstr)
				sock = OpenUnixAbstract (path);
			else
				sock = OpenUnix (path);

			sock.Blocking = true;
			//ns = new UnixStream ((int)sock.Handle);
			ns = new NetworkStream (sock);

			Authenticate ();
		}

		public Socket OpenUnixAbstract (string path)
		{
			/*
			byte[] p = System.Text.Encoding.Default.GetBytes (path);

			SocketAddress sa = new SocketAddress (AddressFamily.Unix, 2 + 1 + p.Length);
			sa[2] = 0; //null prefix for abstract sockets, see unix(7)
			for (int i = 0 ; i != p.Length ; i++)
				sa[i+3] = p[i];

			//TODO: this uglyness is a limitation of Mono.Unix
			UnixEndPoint remoteEndPoint = new UnixEndPoint ("foo");
			EndPoint ep = remoteEndPoint.Create (sa);
			*/

			AbstractUnixEndPoint ep = new AbstractUnixEndPoint (path);

			Socket client = new Socket (AddressFamily.Unix, SocketType.Stream, 0);
			client.Connect (ep);

			return client;
		}

		public Socket OpenUnix (string path)
		{
			UnixEndPoint remoteEndPoint = new UnixEndPoint (path);

			Socket client = new Socket (AddressFamily.Unix, SocketType.Stream, 0);
			client.Connect (remoteEndPoint);

			return client;
		}

		uint serial = 0;
		public uint GenerateSerial ()
		{
			return ++serial;
		}




		public Message SendWithReplyAndBlock (Message msg)
		{
			SendWithReply (msg);

			//TODO: async
			Message retMsg = ReadMessage ();

			return retMsg;
		}

		public uint SendWithReply (Message msg)
		{
			msg.ReplyExpected = true;
			return Send (msg);
		}

		//could be cleaner
		public uint Send (Message msg)
		{
			msg.Serial = GenerateSerial ();

			sock.Poll (-1, SelectMode.SelectWrite);
			ns.Write (msg.HeaderData, 0, msg.HeaderSize);
			if (msg.Body != null) {
				Message.Pad (msg.Body, 8);
				sock.Poll (-1, SelectMode.SelectWrite);
				//ns.Write (msg.Body, 0, msg.BodySize);
				msg.Body.WriteTo (ns);
			}

			return msg.Serial;
		}

		public unsafe Message ReadMessage ()
		{
			//FIXME: fix reading algorithm to work in one step

			byte[] buf = new byte[1024];

			sock.Poll (-1, SelectMode.SelectRead);
			//ns.Read (buf, 0, buf.Length);
			ns.Read (buf, 0, 16);

			//Console.WriteLine ("");
			//Console.WriteLine ("Header:");

			Message msg = new Message ();

			fixed (byte* pbuf = buf) {
				msg.Header = (DHeader*)pbuf;
				//Console.WriteLine (msg.MessageType);
				//System.Console.WriteLine ("Length: " + msg.Header->Length);
				//System.Console.WriteLine ("Header Length: " + msg.Header->HeaderLength);
			}

			int toRead = 0;
			toRead += Message.Padded ((int)msg.Header->HeaderLength, 8);

			//System.Console.WriteLine ("toRead: " + toRead);

			int read;

			sock.Poll (-1, SelectMode.SelectRead);
			read = ns.Read (buf, 16, toRead);

			if (read != toRead)
				System.Console.Error.WriteLine ("Read length mismatch: " + read + " of expected " + toRead);

			msg.HeaderData = buf;

			/*
			System.Console.WriteLine ("Len: " + msg.Header->Length);
			System.Console.WriteLine ("HLen: " + msg.Header->HeaderLength);
			System.Console.WriteLine ("toRead: " + toRead);
			*/
			//read the body
			if (msg.Header->Length != 0) {
				//FIXME
				//msg.Body = new byte[(int)msg.Header->Length];
				byte[] body = new byte[(int)msg.Header->Length];

				sock.Poll (-1, SelectMode.SelectRead);
				//int len = ns.Read (msg.Body, 0, msg.Body.Length);
				int len = ns.Read (body, 0, body.Length);

				//if (len != msg.Body.Length)
				if (len != body.Length)
					System.Console.Error.WriteLine ("Message body size mismatch");

				msg.Body = new MemoryStream (body);
			}

			//this needn't be done here
			Message.IsReading = true;
			msg.ParseHeader ();
			Message.IsReading = false;

			return msg;
		}



		//temporary convenience method
		public void HandleMessage (Message msg)
		{
			string val;
			Message.GetValue (msg.Body, out val);

			Console.WriteLine ("Signal out value: " + val);

			if (Handlers.ContainsKey (msg.Member)) {
				Delegate dlg = Handlers[msg.Member];
				org.freedesktop.DBus.NameAcquiredHandler handler = dlg as org.freedesktop.DBus.NameAcquiredHandler;
				handler (val);
			}
		}

		public Dictionary<string,Delegate> Handlers = new Dictionary<string,Delegate> ();
	}

	//hacky dummy debug console
	internal static class Console
	{
		public static void WriteLine ()
		{
			WriteLine (String.Empty);
		}

		public static void WriteLine (string line)
		{
			//Write (line + "\n");
		}

		public static void WriteLine (object line)
		{
			//Write (line.ToString () + "\n");
		}

		public static void Write (string text)
		{
		}
	}
}

// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.IO.Pipes;

namespace NDesk.DBus.Transports
{
	class PipeTransport : Transport
	{
		protected NamedPipeClientStream pipe;

		public override void Open (AddressEntry entry)
		{
			string host;
			string path;

			if (!entry.Properties.TryGetValue ("host", out host))
				host = "."; // '.' is localhost

			if (!entry.Properties.TryGetValue ("path", out path))
				throw new Exception ("No path specified");

			Open (host, path);
		}

		public void Open (string host, string path)
		{
			//Open (new NamedPipeClientStream (host, path, PipeDirection.InOut, PipeOptions.None));
			Open (new NamedPipeClientStream (host, path, PipeDirection.InOut, PipeOptions.Asynchronous));
		}

		public void Open (NamedPipeClientStream pipe)
		{
			//if (!pipe.IsConnected)
				pipe.Connect ();

			pipe.ReadMode = PipeTransmissionMode.Byte;
			this.pipe = pipe;
			//socket.Blocking = true;
			SocketHandle = (long)pipe.SafePipeHandle.DangerousGetHandle ();
			Stream = pipe;
		}

		public override void WriteCred ()
		{
			Stream.WriteByte (0);
		}

		public override string AuthString ()
		{
			return String.Empty;
		}
	}
}

// Copyright 2009 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.IO.Pipes;

using System.Threading;

namespace NDesk.DBus.Transports
{
	class PipeTransport : Transport
	{
		protected PipeStream pipe;

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

		//public WaitHandle wHandle = new ManualResetEvent (false);
		//public WaitHandle wHandle = new AutoResetEvent (false);
		public void Open (NamedPipeClientStream pipe)
		{

			//if (!pipe.IsConnected)
				pipe.Connect ();

			pipe.ReadMode = PipeTransmissionMode.Byte;
			this.pipe = pipe;
			//socket.Blocking = true;
			//SocketHandle = (long)pipe.SafePipeHandle.DangerousGetHandle ();
			//SocketHandle = (long)pipe.BeginRead()
			//this.ffd = pipe.SafePipeHandle.DangerousGetHandle ();
			//SocketHandle = (long)wHandle.SafeWaitHandle.DangerousGetHandle ();
			//var ovr = new System.Threading.Overlapped ();
			//ovr.AsyncResult = asr;
			Stream = pipe;
			//WaitHandle.WaitAny

		}

		public void RunOnThread ()
		{
			Thread t = new Thread (new ThreadStart (Run));
			t.Start ();
		}

		void Run ()
		{
			connection.mainThread = Thread.CurrentThread;

			while (true) {
				Message msg = ReadMessage ();
				//if (connection.pendingCalls.ContainsKey (msg.Header.Serial))
				if (msg.Header.MessageType == MessageType.MethodReturn || msg.Header.MessageType == MessageType.Error)
					connection.HandleMessage (msg);
				else {
					Inbound.Enqueue (msg);
					FireWakeUp ();
				}
			}
		}

#if ASYNC_PIPES
		protected override int Read (byte[] buffer, int offset, int count)
		{
			pipe = (PipeStream)Stream;
			AsyncCallback cb = delegate (IAsyncResult ar)
			{
				//wHandle = ar.AsyncWaitHandle;
				
				pipe.EndRead (ar);
				((ManualResetEvent)wHandle).Set ();

			};

			//((AutoResetEvent)wHandle).
			IAsyncResult iar = pipe.BeginRead (buffer, offset, count, cb, wHandle);


			Thread.Sleep (100);
			wHandle.WaitOne ();
			((ManualResetEvent)wHandle).Reset ();

			return count;
		}
#endif


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

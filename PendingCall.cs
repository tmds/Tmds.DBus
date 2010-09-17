// Copyright 2007 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;

namespace DBus.Protocol
{
	public class PendingCall : IAsyncResult
	{
		Connection conn;
		Message reply = null;
		//AutoResetEvent waitHandle = new AutoResetEvent (false);
		ManualResetEvent waitHandle;

		public PendingCall (Connection conn)
		{
			this.conn = conn;
		}

		public Message Reply
		{
			get {
				if (reply != null)
					return reply;

				if (Thread.CurrentThread == conn.mainThread) {
					/*
					while (reply == null)
						conn.Iterate ();
					*/

					while (reply == null)
						conn.HandleMessage (conn.Transport.ReadMessage ());

					completedSync = true;

					conn.DispatchSignals ();
				} else {
					if (waitHandle == null)
						Interlocked.CompareExchange (ref waitHandle, new ManualResetEvent (false), null);

					// TODO: Possible race condition?
					while (reply == null)
						waitHandle.WaitOne ();

					completedSync = false;
				}

				return reply;
			} set {
				if (reply != null)
					throw new Exception ("Cannot handle reply more than once");

				reply = value;

				if (waitHandle != null)
					waitHandle.Set ();

				if (Completed != null)
					Completed (reply);
			}
		}

		public event Action<Message> Completed;
		bool completedSync;

		public void Cancel ()
		{
			throw new NotImplementedException ();
		}



		#region IAsyncResult Members

		object IAsyncResult.AsyncState
		{
			get {
				return conn;
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle
		{
			get {
				if (waitHandle == null)
					waitHandle = new ManualResetEvent (false);

				return waitHandle;
			}
		}

		bool IAsyncResult.CompletedSynchronously
		{
			get {
				return reply != null && completedSync;
			}
		}

		bool IAsyncResult.IsCompleted
		{
			get {
				return reply != null;
			}
		}

		#endregion
	}
}

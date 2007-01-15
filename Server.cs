// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NDesk.DBus
{
	//TODO: complete this class
	class Server
	{
		public void Listen (string address)
		{
			this.address = address;
		}

		public void Disconnect ()
		{
		}

		public bool IsConnected
		{
			get {
				return true;
			}
		}

		protected string address;
		public string Address
		{
			get {
				return address;
			}
		}

		//TODO: new connection event/virtual method
	}
}

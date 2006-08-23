// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net.Sockets;

namespace NDesk.DBus
{
	public interface IAuthenticator
	{
		string AuthString ();
	}

	public abstract class Transport : IAuthenticator
	{
		//TODO: design this properly

		//this is just a temporary solution
		public Socket socket;
		public abstract string AuthString ();
	}
}

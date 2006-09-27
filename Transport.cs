// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;

namespace NDesk.DBus.Transports
{
	public interface IAuthenticator
	{
		string AuthString ();
	}

	public abstract class Transport : IAuthenticator
	{
		//TODO: design this properly

		//this is just a temporary solution
		public Stream Stream;
		public long SocketHandle;
		public abstract string AuthString ();
	}
}

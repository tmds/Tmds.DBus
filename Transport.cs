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
		public static Transport Create (AddressEntry entry)
		{
			switch (entry.Method) {
				case "unix":
					//Transport transport = new UnixMonoTransport ();
					Transport transport = new UnixNativeTransport ();
					transport.Open (entry);
					return transport;
				default:
					throw new NotSupportedException ("Transport method \"{0}\" not supported");
			}
		}

		//TODO: design this properly

		//this is just a temporary solution
		public Stream Stream;
		public long SocketHandle;
		public abstract void Open (AddressEntry entry);
		public abstract string AuthString ();
		public abstract void WriteCred ();
	}
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;

namespace DBus.Transports
{
	abstract class UnixTransport : Transport
	{
		public override void Open (AddressEntry entry)
		{
			string path;
			bool abstr;

			if (entry.Properties.TryGetValue ("path", out path))
				abstr = false;
			else if (entry.Properties.TryGetValue ("abstract", out path))
				abstr = true;
			else
				throw new ArgumentException ("No path specified for UNIX transport");

			Open (path, abstr);
		}

		public abstract void Open (string path, bool @abstract);
	}
}

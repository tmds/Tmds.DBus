// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.DBus
{
	public class Address
	{
		//this method is not pretty
		//not worth improving until there is a spec for this format
		public static void Parse (string addr, out bool abstr, out string path)
		{
			//(unix:(path|abstract)=.*,guid=.*|tcp:host=.*(,port=.*)?);? ...
			path = null;
			abstr = false;

			if (addr == null || addr == "")
				return;

			string[] parts;

			parts = addr.Split (':');
			if (parts[0] == "unix") {
				parts = parts[1].Split (',');
				parts = parts[0].Split ('=');
				if (parts[0] == "path")
					abstr = false;
				else if (parts[0] == "abstract")
					abstr = true;
				else
					return;

				path = parts[1];
			} else {
				return;
			}
		}
	}
}

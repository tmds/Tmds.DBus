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
		//TODO: confirm that return value represents parse errors
		public static bool Parse (string addr, out string path, out bool abstr)
		{
			//(unix:(path|abstract)=.*,guid=.*|tcp:host=.*(,port=.*)?);? ...
			path = null;
			abstr = false;

			if (String.IsNullOrEmpty (addr))
				return false;

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
					return false;

				path = parts[1];
			} else {
				return false;
			}

			return true;
		}

		const string SYSTEM_BUS_ADDRESS = "unix:path=/var/run/dbus/system_bus_socket";
		public static string System
		{
			get {
				string addr = Environment.GetEnvironmentVariable ("DBUS_SYSTEM_BUS_ADDRESS");

				if (String.IsNullOrEmpty (addr))
					addr = SYSTEM_BUS_ADDRESS;

				return addr;
			}
		}

		public static string Session
		{
			get {
				return Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");
			}
		}

		public static string Starter
		{
			get {
				return Environment.GetEnvironmentVariable ("DBUS_STARTER_ADDRESS");
			}
		}

		public static string StarterBusType
		{
			get {
				return Environment.GetEnvironmentVariable ("DBUS_STARTER_BUS_TYPE");
			}
		}
	}
}

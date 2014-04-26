// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Text;
using System.Collections.Generic;

namespace DBus
{
	// Subclass obsolete BadAddressException to avoid ABI break
#pragma warning disable 0618
	//public class InvalidAddressException : Exception
	public class InvalidAddressException : BadAddressException
	{
		public InvalidAddressException (string reason) : base (reason) {}
	}
#pragma warning restore 0618

	[Obsolete ("Use InvalidAddressException")]
	public class BadAddressException : Exception
	{
		public BadAddressException (string reason) : base (reason) {}
	}

	static class Address
	{
		// (unix:(path|abstract)=.*,guid=.*|tcp:host=.*(,port=.*)?);? ...
		// or
		// autolaunch:
		public static AddressEntry[] Parse (string addresses)
		{
			if (addresses == null)
				throw new ArgumentNullException (addresses);

			List<AddressEntry> entries = new List<AddressEntry> ();

			foreach (string entryStr in addresses.Split (';'))
				entries.Add (AddressEntry.Parse (entryStr));

			return entries.ToArray ();
		}

		public static string System
		{
			get {
				string addr = Environment.GetEnvironmentVariable ("DBUS_SYSTEM_BUS_ADDRESS");
				if (String.IsNullOrEmpty (addr) && OSHelpers.PlatformIsUnixoid)
					addr = "unix:path=/var/run/dbus/system_bus_socket";
				return addr;
			}
		}

		public static string GetSessionBusAddressFromSharedMemory ()
		{
			string result = OSHelpers.ReadSharedMemoryString ("DBusDaemonAddressInfo", 255);
			if (String.IsNullOrEmpty(result))
				result = OSHelpers.ReadSharedMemoryString ("DBusDaemonAddressInfoDebug", 255); // a DEBUG build of the daemon uses this different address...            
			return result;
		}

		public static string Session {
			get {
				// example: "tcp:host=localhost,port=21955,family=ipv4,guid=b2d47df3207abc3630ee6a71533effb6"
				// note that also "tcp:host=localhost,port=21955,family=ipv4" is sufficient

				// the predominant source for the address is the standard environment variable DBUS_SESSION_BUS_ADDRESS:
				string result = Environment.GetEnvironmentVariable ("DBUS_SESSION_BUS_ADDRESS");

				// On Windows systems, the dbus-daemon additionally uses shared memory to publish the daemon's address.
				// See function _dbus_daemon_publish_session_bus_address() inside the daemon.
				if (string.IsNullOrEmpty (result) && !OSHelpers.PlatformIsUnixoid) {
					result = GetSessionBusAddressFromSharedMemory ();
					if (string.IsNullOrEmpty (result))
						result = "autolaunch:";
				}

				return result;
			}
		}

		public static string Starter {
			get {
				return Environment.GetEnvironmentVariable ("DBUS_STARTER_ADDRESS");
			}
		}

		public static string StarterBusType {
			get {
				return Environment.GetEnvironmentVariable ("DBUS_STARTER_BUS_TYPE");
			}
		}
	}
}

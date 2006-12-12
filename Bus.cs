// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using org.freedesktop.DBus;

namespace NDesk.DBus
{
	public class Bus : Connection
	{
		protected static Bus systemBus = null;
		public static Bus System
		{
			get {
				if (systemBus == null) {
					try {
						if (Address.StarterBusType == "system")
							systemBus = Starter;
						else
							systemBus = Bus.Open (Address.System);
					} catch (Exception e) {
						throw new Exception ("Unable to open the system message bus.", e);
					}
				}

				return systemBus;
			}
		}

		protected static Bus sessionBus = null;
		public static Bus Session
		{
			get {
				if (sessionBus == null) {
					try {
						if (Address.StarterBusType == "session")
							sessionBus = Starter;
						else
							sessionBus = Bus.Open (Address.Session);
					} catch (Exception e) {
						throw new Exception ("Unable to open the session message bus.", e);
					}
				}

				return sessionBus;
			}
		}

		//TODO: parsing of starter bus type, or maybe do this another way
		protected static Bus starterBus = null;
		public static Bus Starter
		{
			get {
				if (starterBus == null) {
					try {
						starterBus = Bus.Open (Address.Starter);
					} catch (Exception e) {
						throw new Exception ("Unable to open the starter message bus.", e);
					}
				}

				return starterBus;
			}
		}

		//public static readonly Bus Session = null;

		//TODO: use the guid, not the whole address string
		//TODO: consider what happens when a connection has been closed
		protected static Dictionary<string,Bus> buses = new Dictionary<string,Bus> ();

		//public static Connection Open (string address)
		public static new Bus Open (string address)
		{
			if (address == null)
				throw new ArgumentNullException ("address");

			if (buses.ContainsKey (address))
				return buses[address];

			Bus bus = new Bus (address);
			buses[address] = bus;

			return bus;
		}

		//protected IBus bus;
		private BusObject bus;

		static readonly string DBusName = "org.freedesktop.DBus";
		static readonly ObjectPath DBusPath = new ObjectPath ("/org/freedesktop/DBus");

		public Bus (string address) : base (address)
		{
			//bus = GetObject<IBus> (DBusName, DBusPath);
			bus = new BusObject (this, DBusName, DBusPath);

			/*
					bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};
		*/
			Register ();
		}

		/*
		public void Register ()
		{
			unique_name = bus.Hello ();
		}

		public ulong GetUnixUser (string name)
		{
			return bus.GetConnectionUnixUser (name);
		}

		public RequestNameReply RequestName (string name)
		{
			return RequestName (name, NameFlag.None);
		}

		public RequestNameReply RequestName (string name, NameFlag flags)
		{
			return bus.RequestName (name, flags);
		}

		public ReleaseNameReply ReleaseName (string name)
		{
			return bus.ReleaseName (name);
		}

		public bool NameHasOwner (string name)
		{
			return bus.NameHasOwner (name);
		}

		public StartReply StartServiceByName (string name)
		{
			return StartServiceByName (name, 0);
		}

		public StartReply StartServiceByName (string name, uint flags)
		{
			return bus.StartServiceByName (name, flags);
		}

		public override void AddMatch (string rule)
		{
			bus.AddMatch (rule);
		}

		public override void RemoveMatch (string rule)
		{
			bus.RemoveMatch (rule);
		}
		*/

		public void Register ()
		{
			if (unique_name != null)
				throw new Exception ("Bus already has a unique name");

			unique_name = (string)bus.InvokeMethod (typeof (IBus).GetMethod ("Hello"));
		}

		protected string unique_name = null;
		public string UniqueName
		{
			get {
				return unique_name;
			} set {
				if (unique_name != null)
					throw new Exception ("Unique name can only be set once");
				unique_name = value;
			}
		}

		public ulong GetUnixUser (string name)
		{
			return (ulong)bus.InvokeMethod (typeof (IBus).GetMethod ("GetConnectionUnixUser"), name);
		}

		public RequestNameReply RequestName (string name)
		{
			return RequestName (name, NameFlag.None);
		}

		public RequestNameReply RequestName (string name, NameFlag flags)
		{
			return (RequestNameReply)bus.InvokeMethod (typeof (IBus).GetMethod ("RequestName"), name, flags);
		}

		public ReleaseNameReply ReleaseName (string name)
		{
			return (ReleaseNameReply)bus.InvokeMethod (typeof (IBus).GetMethod ("ReleaseName"), name);
		}

		public bool NameHasOwner (string name)
		{
			return (bool)bus.InvokeMethod (typeof (IBus).GetMethod ("NameHasOwner"), name);
		}

		public StartReply StartServiceByName (string name)
		{
			return StartServiceByName (name, 0);
		}

		public StartReply StartServiceByName (string name, uint flags)
		{
			return (StartReply)bus.InvokeMethod (typeof (IBus).GetMethod ("StartServiceByName"), name, flags);
		}

		public override void AddMatch (string rule)
		{
			bus.InvokeMethod (typeof (IBus).GetMethod ("AddMatch"), rule);
		}

		public override void RemoveMatch (string rule)
		{
			bus.InvokeMethod (typeof (IBus).GetMethod ("RemoveMatch"), rule);
		}
	}
}

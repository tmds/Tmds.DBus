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
				if (systemBus == null)
					systemBus = Bus.Open (Address.System);

				return systemBus;
			}
		}

		protected static Bus sessionBus = null;
		public static Bus Session
		{
			get {
				if (sessionBus == null)
					sessionBus = Bus.Open (Address.Session);

				return sessionBus;
			}
		}

		//TODO: parsing of starter bus type, or maybe do this another way
		protected static Bus starterBus = null;
		public static Bus Starter
		{
			get {
				if(starterBus == null)
					starterBus = Bus.Open (Address.Starter);

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
			if (buses.ContainsKey (address))
				return buses[address];

			Bus bus = new Bus (address);
			buses[address] = bus;

			return bus;
		}

		//protected org.freedesktop.DBus.Bus bus_proxy;
		protected string unique_name;
		protected IBus bus;

		static readonly string DBusName = "org.freedesktop.DBus";
		static readonly ObjectPath DBusPath = new ObjectPath ("/org/freedesktop/DBus");

		public Bus (string address) : base (address)
		{
			bus = GetObject<IBus> (DBusName, DBusPath);
			/*
					bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};
		*/
			Register ();
			Iterate ();
		}

		protected void Register ()
		{
			unique_name = bus.Hello ();
		}

		public string UniqueName
		{
			get {
				return unique_name;
			} set {
				unique_name = value;
			}
		}

		public ulong GetUnixUser (string name)
		{
			return bus.GetConnectionUnixUser (name);
		}

		public NameReply RequestName (string name)
		{
			return RequestName (name, NameFlag.None);
		}

		public NameReply RequestName (string name, NameFlag flags)
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
			return StartServiceByName (name);
		}

		public StartReply StartServiceByName (string name, uint flags)
		{
			return bus.StartServiceByName (name, flags);
		}

		public override void AddMatch (string rule)
		{
			bus.AddMatch (rule);
			Iterate ();
		}

		public override void RemoveMatch (string rule)
		{
			bus.RemoveMatch (rule);
			Iterate ();
		}
	}
}

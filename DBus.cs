// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using NDesk.DBus;

//namespace org.freedesktop.DBus
namespace org.freedesktop.DBus
{
	/*
	//what's this for?
	public class DBusException : ApplicationException
	{
	}
	*/

#if UNDOCUMENTED_IN_SPEC
	//TODO: maybe this should be mapped to its CLR counterpart directly?
	//not yet used
	[Interface ("org.freedesktop.DBus.Error.InvalidArgs")]
	public class InvalidArgsException : ApplicationException
	{
	}
#endif

	[Flags]
	public enum NameFlag : uint
	{
		None = 0,
		AllowReplacement = 0x1,
		ReplaceExisting = 0x2,
		DoNotQueue = 0x4,
	}

	public enum RequestNameReply : uint
	{
		PrimaryOwner = 1,
		InQueue,
		Exists,
		AlreadyOwner,
	}

	public enum ReleaseNameReply : uint
	{
		Released = 1,
		NonExistent,
		NotOwner,
	}

	public enum StartReply : uint
	{
		//The service was successfully started.
		Success = 1,
		//A connection already owns the given name.
		AlreadyRunning,
	}

	public delegate void NameOwnerChangedHandler (string name, string old_owner, string new_owner);
	public delegate void NameAcquiredHandler (string name);
	public delegate void NameLostHandler (string name);

	[Interface ("org.freedesktop.DBus.Peer")]
	public interface Peer
	{
		void Ping ();
		[return: Argument ("machine_uuid")]
		string GetMachineId ();
	}

	[Interface ("org.freedesktop.DBus.Introspectable")]
	public interface Introspectable
	{
		[return: Argument ("data")]
		string Introspect ();
	}

	[Interface ("org.freedesktop.DBus.Properties")]
	public interface Properties
	{
		//TODO: some kind of indexer mapping?
		//object this [string propname] {get; set;}

		[return: Argument ("value")]
		object Get (string @interface, string propname);
		//void Get (string @interface, string propname, out object value);
		void Set (string @interface, string propname, object value);
	}

	[Interface ("org.freedesktop.DBus")]
	public interface IBus : Introspectable
	{
		RequestNameReply RequestName (string name, NameFlag flags);
		ReleaseNameReply ReleaseName (string name);
		string Hello ();
		string[] ListNames ();
		string[] ListActivatableNames ();
		bool NameHasOwner (string name);
		event NameOwnerChangedHandler NameOwnerChanged;
		event NameLostHandler NameLost;
		event NameAcquiredHandler NameAcquired;
		StartReply StartServiceByName (string name, uint flags);
		string GetNameOwner (string name);
		uint GetConnectionUnixUser (string connection_name);
		void AddMatch (string rule);
		void RemoveMatch (string rule);

		//undocumented in spec
		string[] ListQueuedOwners (string name);
		uint GetConnectionUnixProcessID (string connection_name);
		byte[] GetConnectionSELinuxSecurityContext (string connection_name);
		void ReloadConfig ();
	}
}

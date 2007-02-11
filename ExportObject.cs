// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Reflection;
using System.Reflection.Emit;

using org.freedesktop.DBus;

namespace NDesk.DBus
{
	internal class ExportObject : BusObject //, Peer
	{
		public readonly object obj;

		public ExportObject (Connection conn, string bus_name, ObjectPath object_path, object obj) : base (conn, bus_name, object_path)
		{
			this.obj = obj;

			Type type = obj.GetType ();

			foreach (MemberInfo mi in Mapper.GetPublicMembers (type)) {
				EventInfo ei = mi as EventInfo;

				if (ei == null)
					continue;

				Delegate dlg = GetHookupDelegate (ei);
				ei.AddEventHandler (obj, dlg);
			}
		}

		/*
		public void Ping ()
		{
		}

		public string GetMachineId ()
		{
			//TODO: implement this
			return String.Empty;
		}
		*/
	}
}

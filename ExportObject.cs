// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using org.freedesktop.DBus;

namespace DBus
{
	using Protocol;

	//TODO: perhaps ExportObject should not derive from BusObject
	internal class ExportObject : BusObject, IDisposable //, Peer
	{
		//maybe add checks to make sure this is not called more than once
		//it's a bit silly as a property
		bool isRegistered = false;

		static readonly Dictionary<MethodInfo,MethodCaller2> mCallers = new Dictionary<MethodInfo,MethodCaller2> ();

		public ExportObject (Connection conn, ObjectPath object_path, object obj) : base (conn, null, object_path)
		{
			Object = obj;
		}

		public object Object {
			get;
			private set;
		}

		public virtual bool Registered
		{
			get {
				return isRegistered;
			}
			set {
				if (value == isRegistered)
					return;

				Type type = Object.GetType ();

				foreach (MemberInfo mi in Mapper.GetPublicMembers (type)) {
					EventInfo ei = mi as EventInfo;

					if (ei == null)
						continue;

					Delegate dlg = GetHookupDelegate (ei);

					if (value)
						ei.AddEventHandler (Object, dlg);
					else
						ei.RemoveEventHandler (Object, dlg);
				}

				isRegistered = value;
			}
		}

		internal virtual void WriteIntrospect (Introspector intro)
		{
			intro.WriteType (Object.GetType ());
		}

		internal static MethodCaller2 GetMCaller (MethodInfo mi)
		{
			MethodCaller2 mCaller;
			if (!mCallers.TryGetValue (mi, out mCaller)) {
				//mCaller = TypeImplementer.GenCaller (mi, obj);
				mCaller = TypeImplementer.GenCaller2 (mi);
				mCallers[mi] = mCaller;
			}
			return mCaller;
		}

		public static ExportObject CreateExportObject (Connection conn, ObjectPath object_path, object obj)
		{
#if DLR
			Type type = obj.GetType ();
			if (type.Name == "RubyObject" || type.FullName == "IronPython.Runtime.Types.OldInstance")
				return new DynamicExportObject (conn, object_path, obj);
#endif

			return new ExportObject (conn, object_path, obj);
		}

		public virtual void HandleMethodCall (MethodCall method_call)
		{
			Type type = Object.GetType ();

			//object retObj = type.InvokeMember (msg.Member, BindingFlags.InvokeMethod, null, obj, MessageHelper.GetDynamicValues (msg));

			//TODO: there is no member name mapping for properties etc. yet

			// FIXME: Inefficient to do this on every call
			MethodInfo mi = Mapper.GetMethod (type, method_call);

			if (mi == null) {
				conn.MaybeSendUnknownMethodError (method_call);
				return;
			}

			MethodCaller2 mCaller;
			if (!mCallers.TryGetValue (mi, out mCaller)) {
				//mCaller = TypeImplementer.GenCaller (mi, obj);
				mCaller = TypeImplementer.GenCaller2 (mi);
				mCallers[mi] = mCaller;
			}

			Signature inSig, outSig;
			TypeImplementer.SigsForMethod (mi, out inSig, out outSig);

			Message msg = method_call.message;
			MessageReader msgReader = new MessageReader (method_call.message);
			MessageWriter retWriter = new MessageWriter ();

			/*
			MessageWriter retWriter = null;
			if (msg.ReplyExpected)
				retWriter = new MessageWriter ();
			*/

			Exception raisedException = null;
			try {
				//mCaller (msgReader, method_call.message, retWriter);
				mCaller (Object, msgReader, method_call.message, retWriter);
			} catch (Exception e) {
				raisedException = e;
			}

			if (!msg.ReplyExpected)
				return;

			Message replyMsg;

			if (raisedException == null) {
				MethodReturn method_return = new MethodReturn (msg.Header.Serial);
				replyMsg = method_return.message;
				replyMsg.AttachBodyTo (retWriter);
				replyMsg.Signature = outSig;
			} else {
				Error error;
				// BusException allows precisely formatted Error messages.
				BusException busException = raisedException as BusException;
				if (busException != null)
					error = method_call.CreateError (busException.ErrorName, busException.ErrorMessage);
				else if (raisedException is ArgumentException && raisedException.TargetSite.Name == mi.Name) {
					// Name match trick above is a hack since we don't have the resolved MethodInfo.
					ArgumentException argException = (ArgumentException)raisedException;
					using (System.IO.StringReader sr = new System.IO.StringReader (argException.Message)) {
						error = method_call.CreateError ("org.freedesktop.DBus.Error.InvalidArgs", sr.ReadLine ());
					}
				} else
					error = method_call.CreateError (Mapper.GetInterfaceName (raisedException.GetType ()), raisedException.Message);

				replyMsg = error.Message;
			}

			if (method_call.Sender != null)
				replyMsg.Header[FieldCode.Destination] = method_call.Sender;

			conn.Send (replyMsg);
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

#region IDisposable
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		~ExportObject ()
		{
			Dispose (false);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				if (Object != null) {
					Registered = false;
					Object = null;
				}
			}
		}
#endregion
	}
}

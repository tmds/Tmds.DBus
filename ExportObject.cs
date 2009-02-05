// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using org.freedesktop.DBus;

namespace NDesk.DBus
{
	//TODO: perhaps ExportObject should not derive from BusObject
	internal class ExportObject : BusObject //, Peer
	{
		public readonly object obj;

		public ExportObject (Connection conn, ObjectPath object_path, object obj) : base (conn, null, object_path)
		{
			this.obj = obj;
		}

		//maybe add checks to make sure this is not called more than once
		//it's a bit silly as a property
		public bool Registered
		{
			set {
				Type type = obj.GetType ();

				foreach (MemberInfo mi in Mapper.GetPublicMembers (type)) {
					EventInfo ei = mi as EventInfo;

					if (ei == null)
						continue;

					Delegate dlg = GetHookupDelegate (ei);

					if (value)
						ei.AddEventHandler (obj, dlg);
					else
						ei.RemoveEventHandler (obj, dlg);
				}
			}
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

		static internal readonly Dictionary<MethodInfo,MethodCaller2> mCallers = new Dictionary<MethodInfo,MethodCaller2> ();
		public void HandleMethodCall (MethodCall method_call)
		{
			Type type = obj.GetType ();
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
				mCaller (obj, msgReader, method_call.message, retWriter);
			} catch (Exception e) {
				raisedException = e;
			}

			if (!msg.ReplyExpected)
				return;

			Message replyMsg;

			if (raisedException == null) {
				MethodReturn method_return = new MethodReturn (msg.Header.Serial);
				replyMsg = method_return.message;
				replyMsg.Body = retWriter.ToArray ();
				replyMsg.Signature = outSig;
			} else {
				// TODO: send an error!
				Error error = method_call.CreateError (Mapper.GetInterfaceName (raisedException.GetType ()), raisedException.Message);
				replyMsg = error.message;
			}

			if (method_call.Sender != null)
				replyMsg.Header.Fields[FieldCode.Destination] = method_call.Sender;

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
	}
}

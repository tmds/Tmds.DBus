// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using org.freedesktop.DBus;

namespace DBus
{
	using Protocol;

	//TODO: perhaps ExportObject should not derive from BusObject
	internal class ExportObject : BusObject, IDisposable
	{
		//maybe add checks to make sure this is not called more than once
		//it's a bit silly as a property
		bool isRegistered = false;
		Dictionary<string, MethodInfo> methodInfoCache = new Dictionary<string, MethodInfo> ();

		static readonly Dictionary<MethodInfo, MethodCaller> mCallers = new Dictionary<MethodInfo, MethodCaller> ();

		public ExportObject (Connection conn, ObjectPath object_path, object obj) : base (conn, null, object_path)
		{
			Object = obj;
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

				foreach (var memberForType in Mapper.GetPublicMembers (type)) {
					MemberInfo mi = memberForType.Value;
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

		internal static MethodCaller GetMCaller (MethodInfo mi)
		{
			MethodCaller mCaller;
			if (!mCallers.TryGetValue (mi, out mCaller)) {
				mCaller = TypeImplementer.GenCaller (mi);
				mCallers[mi] = mCaller;
			}
			return mCaller;
		}

		public static ExportObject CreateExportObject (Connection conn, ObjectPath object_path, object obj)
		{
			return new ExportObject (conn, object_path, obj);
		}

		public virtual void HandleMethodCall (MessageContainer method_call)
		{
			MethodInfo mi;
			if (!methodInfoCache.TryGetValue (method_call.Member, out mi))
				methodInfoCache[method_call.Member] = mi = Mapper.GetMethod (Object.GetType (), method_call);

			if (mi == null) {
				conn.MaybeSendUnknownMethodError (method_call);
				return;
			}

			MethodCaller mCaller;
			if (!mCallers.TryGetValue (mi, out mCaller)) {
				mCaller = TypeImplementer.GenCaller (mi);
				mCallers[mi] = mCaller;
			}

			Signature inSig, outSig;
			TypeImplementer.SigsForMethod (mi, out inSig, out outSig);

			Message msg = method_call.Message;
			MessageReader msgReader = new MessageReader (msg);
			MessageWriter retWriter = new MessageWriter ();

			Exception raisedException = null;
			try {
				mCaller (Object, msgReader, msg, retWriter);
			} catch (Exception e) {
				raisedException = e;
			}

			if (!msg.ReplyExpected)
				return;

			Message replyMsg;

			if (raisedException == null) {
				MessageContainer method_return = new MessageContainer {
					Type = MessageType.MethodReturn,
					ReplySerial = msg.Header.Serial
				};
				replyMsg = method_return.Message;
				replyMsg.AttachBodyTo (retWriter);
				replyMsg.Signature = outSig;
			} else {
				// BusException allows precisely formatted Error messages.
				BusException busException = raisedException as BusException;
				if (busException != null)
					replyMsg = method_call.CreateError (busException.ErrorName, busException.ErrorMessage);
				else if (raisedException is ArgumentException && raisedException.TargetSite.Name == mi.Name) {
					// Name match trick above is a hack since we don't have the resolved MethodInfo.
					ArgumentException argException = (ArgumentException)raisedException;
					using (System.IO.StringReader sr = new System.IO.StringReader (argException.Message)) {
						replyMsg = method_call.CreateError ("org.freedesktop.DBus.Error.InvalidArgs", sr.ReadLine ());
					}
				} else
					replyMsg = method_call.CreateError (Mapper.GetInterfaceName (raisedException.GetType ()), raisedException.Message);
			}

			if (method_call.Sender != null)
				replyMsg.Header[FieldCode.Destination] = method_call.Sender;

			conn.Send (replyMsg);
		}

		public object Object {
			get;
			private set;
		}

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
	}
}

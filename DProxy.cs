// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;

namespace NDesk.DBus
{
	//marked internal because this is really an implementation detail and needs to be replaced
	internal class DProxy : RealProxy
	{
		protected BusObject busObject;

		public DProxy (BusObject busObject, Type type) : base(type)
		{
			this.busObject = busObject;
		}

		public override IMessage Invoke (IMessage message)
		{
			IMethodCallMessage callMessage = (IMethodCallMessage) message;

			object[] outArgs;
			object retVal;
			Exception exception;
			busObject.Invoke (callMessage.MethodBase, callMessage.MethodName, callMessage.InArgs, out outArgs, out retVal, out exception);

			MethodReturnMessageWrapper returnMessage = new MethodReturnMessageWrapper ((IMethodReturnMessage) message);
			returnMessage.Exception = exception;
			returnMessage.ReturnValue = retVal;

			return returnMessage;
		}

		/*
		public override ObjRef CreateObjRef (Type ServerType)
		{
			throw new System.NotImplementedException ();
		}
		*/

		~DProxy ()
		{
			//FIXME: remove handlers/match rules here
			if (Protocol.Verbose)
				Console.Error.WriteLine ("Warning: Finalization of " + busObject.Path + " not yet supported");
		}
	}
}

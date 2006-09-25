// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.DBus
{
	public class MessageFilter
	{
		//this should probably be made to use HeaderField or similar
		//this class is not generalized yet

		public static string MessageTypeToString (MessageType mtype)
		{
			switch (mtype)
			{
				case MessageType.MethodCall:
					return "method_call";
				case MessageType.MethodReturn:
					return "method_return";
				case MessageType.Error:
					return "error";
				case MessageType.Signal:
					return "signal";
				case MessageType.Invalid:
					return "invalid";
				default:
					throw new Exception ("Bad MessageType: " + mtype);
			}
		}

		public static string CreateMatchRule (MessageType mtype)
		{
			return "type='" + MessageTypeToString (mtype) + "'";
		}

		public static string CreateMatchRule (MessageType type, ObjectPath path, string @interface, string member)
		{
			return "type='" + MessageTypeToString (type) + "',path='" + path.Value + "',interface='" + @interface + "',member='" + member + "'";
		}

		//TODO
		//this is useful as a Predicate<Message> delegate
		public bool Match (Message message)
		{
			return false;
		}
	}
}

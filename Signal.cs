
using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class Signal
	{
		public Message message = new Message ();

		public Signal (ObjectPath path, string @interface, string member)
		{
			message.Header.MessageType = MessageType.Signal;
			message.Header.Flags = HeaderFlag.NoReplyExpected | HeaderFlag.NoAutoStart;
			message.Header[FieldCode.Path] = path;
			message.Header[FieldCode.Interface] = @interface;
			message.Header[FieldCode.Member] = member;
		}

		public Signal (Message message)
		{
			this.message = message;
			Path = (ObjectPath)message.Header[FieldCode.Path];
			Interface = (string)message.Header[FieldCode.Interface];
			Member = (string)message.Header[FieldCode.Member];
			Sender = (string)message.Header[FieldCode.Sender];
		}

		public ObjectPath Path;
		public string Interface;
		public string Member;
		public string Sender;
	}
}

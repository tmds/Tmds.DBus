
using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class MethodReturn
	{
		public Message message = new Message ();

		public MethodReturn (uint reply_serial)
		{
			message.Header.MessageType = MessageType.MethodReturn;
			message.Header.Flags = HeaderFlag.NoReplyExpected | HeaderFlag.NoAutoStart;
			message.Header[FieldCode.ReplySerial] = reply_serial;
			//signature optional?
			//message.Header[FieldCode.Signature] = signature;
		}

		public MethodReturn (Message message)
		{
			this.message = message;
			ReplySerial = (uint)message.Header[FieldCode.ReplySerial];
		}

		public uint ReplySerial;
	}
}

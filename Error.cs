
using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class Error
	{
		public Message message = new Message ();

		public Error (string error_name, uint reply_serial)
		{
			message.Header.MessageType = MessageType.Error;
			message.Header.Flags = HeaderFlag.NoReplyExpected | HeaderFlag.NoAutoStart;
			message.Header[FieldCode.ErrorName] = error_name;
			message.Header[FieldCode.ReplySerial] = reply_serial;
		}

		public Error (Message message)
		{
			this.message = message;
			ErrorName = (string)message.Header[FieldCode.ErrorName];
			ReplySerial = (uint)message.Header[FieldCode.ReplySerial];
			//Signature = (Signature)message.Header[FieldCode.Signature];
		}

		public string ErrorName;
		public uint ReplySerial;
		//public Signature Signature;
	}
}

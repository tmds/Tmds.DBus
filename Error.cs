
using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class Error
	{
		Message message = new Message ();
		string errorName;
		uint replySerial;

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
			errorName = (string)message.Header[FieldCode.ErrorName];
			replySerial = (uint)message.Header[FieldCode.ReplySerial];
		}

		public string ErrorName {
			get {
				return this.errorName;
			}
		}

		public Message Message {
			get {
				return this.message;
			}
		}

		public uint ReplySerial {
			get {
				return this.replySerial;
			}
		}
	}
}

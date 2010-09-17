
using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class Signal
	{
		Message message = new Message ();
		ObjectPath path;
		string @interface;
		string member;
		string sender;

		public Signal (ObjectPath path, string @interface, string member, Signature signature)
		{
			message.Header.MessageType = MessageType.Signal;
			message.Header.Flags = HeaderFlag.NoReplyExpected | HeaderFlag.NoAutoStart;
			message.Header[FieldCode.Path] = this.path = path;
			message.Header[FieldCode.Interface] = this.@interface = @interface;
			message.Header[FieldCode.Member] = member;
			message.Signature = signature;
		}

		public Signal (Message message)
		{
			this.message = message;
			path = (ObjectPath)message.Header[FieldCode.Path];
			@interface = (string)message.Header[FieldCode.Interface];
			member = (string)message.Header[FieldCode.Member];
			sender = (string)message.Header[FieldCode.Sender];
		}

		public Message Message {
			get {
				return message;
			}
		}

		public string Interface {
			get {
				return @interface;
			}
		}

		public string Member {
			get {
				return member;
			}
		}

		public ObjectPath Path {
			get {
				return path;
			}
		}

		public string Sender {
			get {
				return sender;
			}
		}
	}
}

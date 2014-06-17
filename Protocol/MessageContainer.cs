// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	public class MessageContainer
	{
		Message resultMessage;

		public MessageContainer ()
		{
			Type = MessageType.MethodCall;
		}

		MessageContainer (Message originalMessage)
		{
			this.resultMessage = originalMessage;
		}

		public Message Message {
			get {
				if (resultMessage != null)
					return resultMessage;
				return resultMessage = ExportToMessage ();
			}
		}

		public Message CreateError (string errorName, string errorMessage)
		{
			var message = Message;
			MessageContainer error = new MessageContainer {
				Type = MessageType.Error,
				ErrorName = errorName,
				ReplySerial = message.Header.Serial,
				Signature = Signature.StringSig,
				Destination = Sender
			};

			MessageWriter writer = new MessageWriter (message.Header.Endianness);
			writer.Write (errorMessage);
			message = error.Message;
			message.AttachBodyTo (writer);

			return message;
		}

		public static MessageContainer FromMessage (Message message)
		{
			MessageContainer container = new MessageContainer (message) {
				Path = (ObjectPath)message.Header[FieldCode.Path],
				Interface = (string)message.Header[FieldCode.Interface],
				Member = (string)message.Header[FieldCode.Member],
				Destination = (string)message.Header[FieldCode.Destination],
				//TODO: filled by the bus so reliable, but not the case for p2p
				//so we make it optional here, but this needs some more thought
				//if (message.Header.Fields.ContainsKey (FieldCode.Sender))
				Sender = (string)message.Header[FieldCode.Sender],
				ErrorName = (string)message.Header[FieldCode.ErrorName],
				ReplySerial = (uint?)message.Header[FieldCode.ReplySerial],
				Signature = message.Signature,
				Serial = message.Header.Serial,
				Type = message.Header.MessageType,
			};
#if PROTO_REPLY_SIGNATURE
			//TODO: note that an empty ReplySignature should really be treated differently to the field not existing!
			if (message.Header.Fields.ContainsKey (FieldCode.ReplySignature))
				container.ReplySignature = (Signature)message.Header[FieldCode.ReplySignature];
			else
				container.ReplySignature = Signature.Empty;
#endif

			return container;
		}

		Message ExportToMessage () {
			var message = new Message ();
			message.Header.MessageType = Type;
			if (Type == MessageType.MethodCall)
				message.ReplyExpected = true;
			else
				message.Header.Flags = HeaderFlag.NoReplyExpected | HeaderFlag.NoAutoStart;
			message.Header[FieldCode.Path] = Path;
			message.Header[FieldCode.Interface] = Interface;
			message.Header[FieldCode.Member] = Member;
			message.Header[FieldCode.Destination] = Destination;
			message.Header[FieldCode.ErrorName] = ErrorName;
#if PROTO_REPLY_SIGNATURE
			//TODO
#endif
			message.Signature = Signature;
			if (ReplySerial != null)
				message.Header[FieldCode.ReplySerial] = (uint)ReplySerial;
			if (Serial != null)
				message.Header.Serial = (uint)Serial;

			return message;
		}

		public MessageType Type {
			get;
			set;
		}

		public ObjectPath Path {
			get;
			set;
		}

		public string Interface {
			get;
			set;
		}

		public string Member {
			get;
			set;
		}

		public string Destination {
			get;
			set;
		}

		public string Sender {
			get;
			set;
		}

#if PROTO_REPLY_SIGNATURE
		public Signature ReplySignature {
			get;
			set;
		}
#endif

		public uint? ReplySerial {
			get;
			set;
		}

		public uint? Serial {
			get;
			set;
		}

		public Signature Signature {
			get;
			set;
		}

		public string ErrorName {
			get;
			set;
		}

		/*public override bool Equals (object other)
		{
			MessageContainer otherMethodCall = other as MessageContainer;
			return otherMethodCall == null ? false : Equals (otherMethodCall);
		}

		public bool Equals (MessageContainer other)
		{
			return (Path == other.Path || Path.Equals (other.Path))
				&& Interface == other.Interface
				&& Member == other.Member
				&& Destination == other.Destination
				&& Sender == other.Destination
				&& ErrorName == other.ErrorName
				&& Signature.Equals (other.Signature)
				&& ReplySerial.Equals (other.ReplySerial);
		}

		public override int GetHashCode ()
		{
			return (Path == null ? 0 : Path.GetHashCode ())
				^ (Interface == null ? 0 : Interface.GetHashCode ())
				^ (Member == null ? 0 : Member.GetHashCode ())
				^ (Destination == null ? 0 : Destination.GetHashCode ())
				^ (Sender == null ? 0 : Sender.GetHashCode ())
				^ (ErrorName == null ? 0 : ErrorName.GetHashCode ())
				^ Signature.GetHashCode ();
		}*/
	}
}

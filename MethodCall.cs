// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;

namespace DBus.Protocol
{
	//TODO: complete and use these wrapper classes
	//not sure exactly what I'm thinking but there seems to be sense here

	//FIXME: signature sending/receiving is currently ambiguous in this code
	//FIXME: in fact, these classes are totally broken and end up doing no-op, do not use without understanding the problem
	public class MethodCall
	{
		public Message message = new Message ();

		public MethodCall (ObjectPath path, string @interface, string member, string destination, Signature signature)
		{
			message.Header.MessageType = MessageType.MethodCall;
			message.ReplyExpected = true;
			message.Header[FieldCode.Path] = path;
			message.Header[FieldCode.Interface] = @interface;
			message.Header[FieldCode.Member] = member;
			message.Header[FieldCode.Destination] = destination;
			//TODO: consider setting Sender here for p2p situations
			//this will allow us to remove the p2p hacks in MethodCall and Message
#if PROTO_REPLY_SIGNATURE
			//TODO
#endif
			message.Signature = signature;
		}

		public MethodCall (Message message)
		{
			this.message = message;
			Path = (ObjectPath)message.Header[FieldCode.Path];
			Interface = (string)message.Header[FieldCode.Interface];
			Member = (string)message.Header[FieldCode.Member];
			Destination = (string)message.Header[FieldCode.Destination];
			//TODO: filled by the bus so reliable, but not the case for p2p
			//so we make it optional here, but this needs some more thought
			//if (message.Header.Fields.ContainsKey (FieldCode.Sender))
			Sender = (string)message.Header[FieldCode.Sender];
#if PROTO_REPLY_SIGNATURE
			//TODO: note that an empty ReplySignature should really be treated differently to the field not existing!
			if (message.Header.Fields.ContainsKey (FieldCode.ReplySignature))
				ReplySignature = (Signature)message.Header[FieldCode.ReplySignature];
			else
				ReplySignature = Signature.Empty;
#endif
			Signature = message.Signature;
		}

		public ObjectPath Path;
		public string Interface;
		public string Member;
		public string Destination;
		public string Sender;
#if PROTO_REPLY_SIGNATURE
		public Signature ReplySignature;
#endif
		public Signature Signature;

		public Error CreateError (string errorName, string errorMessage)
		{
			Error error = new Error (errorName, message.Header.Serial);
			error.message.Signature = Signature.StringSig;

			MessageWriter writer = new MessageWriter (message.Header.Endianness);
			//writer.connection = conn;
			writer.Write (errorMessage);
			error.message.Body = writer.ToArray ();

			//if (method_call.Sender != null)
			//	replyMsg.Header[FieldCode.Destination] = method_call.Sender;

			return error;
		}
	}
}

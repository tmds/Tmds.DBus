// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace NDesk.DBus
{
	//TODO: complete and use these wrapper classes
	//not sure exactly what I'm thinking but there seems to be sense here

	//FIXME: signature sending/receiving is currently ambiguous in this code
	//FIXME: in fact, these classes are totally broken and end up doing no-op, do not use without understanding the problem
	public class MethodCall
	{
		public Message message = new Message ();

		public MethodCall (ObjectPath path, string @interface, string member, string destination)
		{
			message.Header.MessageType = MessageType.MethodCall;
			message.ReplyExpected = true;
			message.Header.Fields[FieldCode.Path] = path;
			message.Header.Fields[FieldCode.Interface] = @interface;
			message.Header.Fields[FieldCode.Member] = member;
			message.Header.Fields[FieldCode.Destination] = destination;
#if PROTO_REPLY_SIGNATURE
			//TODO
#endif
		}

		public MethodCall (ObjectPath path, string @interface, string member, string destination, Signature signature) : this (path, @interface, member, destination)
		{
			//message.Header.Fields[FieldCode.Signature] = signature;
			//use the wrapper in Message because it checks for emptiness
			message.Signature = signature;
		}

		public MethodCall (Message message)
		{
			this.message = message;
			Path = (ObjectPath)message.Header.Fields[FieldCode.Path];
			Interface = (string)message.Header.Fields[FieldCode.Interface];
			Member = (string)message.Header.Fields[FieldCode.Member];
			Destination = (string)message.Header.Fields[FieldCode.Destination];
			Sender = (string)message.Header.Fields[FieldCode.Sender];
#if PROTO_REPLY_SIGNATURE
			if (message.Header.Fields.ContainsKey (FieldCode.ReplySignature))
				ReplySignature = (Signature)message.Header.Fields[FieldCode.ReplySignature];
			else
				ReplySignature = new Signature ("");
#endif
			//Signature = (Signature)message.Header.Fields[FieldCode.Signature];
			//use the wrapper in Message because it checks for emptiness
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
	}

	public class MethodReturn
	{
		public Message message = new Message ();

		public MethodReturn (uint reply_serial)
		{
			message.Header.MessageType = MessageType.MethodReturn;
			message.ReplyExpected = false;
			message.Header.Fields[FieldCode.ReplySerial] = (uint)reply_serial;
			//signature optional?
			//message.Header.Fields[FieldCode.Signature] = signature;
		}

		public MethodReturn (Message message)
		{
			this.message = message;
			ReplySerial = (uint)message.Header.Fields[FieldCode.ReplySerial];
		}

		public uint ReplySerial;
	}

	public class Error
	{
		public Message message = new Message ();

		public Error (string error_name, uint reply_serial)
		{
			message.Header.MessageType = MessageType.MethodReturn;
			message.ReplyExpected = false;
			message.Header.Fields[FieldCode.ErrorName] = error_name;
			message.Header.Fields[FieldCode.ReplySerial] = reply_serial;
		}

		public Error (Message message)
		{
			this.message = message;
			ErrorName = (string)message.Header.Fields[FieldCode.ErrorName];
			ReplySerial = (uint)message.Header.Fields[FieldCode.ReplySerial];
			//Signature = (Signature)message.Header.Fields[FieldCode.Signature];
		}

		public string ErrorName;
		public uint ReplySerial;
		//public Signature Signature;
	}

	public class Signal
	{
		public Message message = new Message ();

		public Signal (ObjectPath path, string @interface, string member)
		{
			message.Header.MessageType = MessageType.Signal;
			message.ReplyExpected = false;
			message.Header.Fields[FieldCode.Path] = path;
			message.Header.Fields[FieldCode.Interface] = @interface;
			message.Header.Fields[FieldCode.Member] = member;
		}

		public Signal (Message message)
		{
			this.message = message;
			Path = (ObjectPath)message.Header.Fields[FieldCode.Path];
			Interface = (string)message.Header.Fields[FieldCode.Interface];
			Member = (string)message.Header.Fields[FieldCode.Member];
		}

		public ObjectPath Path;
		public string Interface;
		public string Member;
	}
}

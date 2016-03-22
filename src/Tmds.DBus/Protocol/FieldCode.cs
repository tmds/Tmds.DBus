// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace DBus.Protocol
{
	public enum FieldCode : byte
	{
		Invalid,
		Path,
		Interface,
		Member,
		ErrorName,
		ReplySerial,
		Destination,
		Sender,
		Signature,
#if PROTO_REPLY_SIGNATURE
		ReplySignature, //note: not supported in dbus
#endif
	}
}

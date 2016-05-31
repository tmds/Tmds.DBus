// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus.Protocol
{
    internal enum FieldCode : byte
    {
        Invalid,
        Path,
        Interface,
        Member,
        ErrorName,
        ReplySerial,
        Destination,
        Sender,
        Signature
    }
}

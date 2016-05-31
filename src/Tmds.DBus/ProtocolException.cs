// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    public class ProtocolException : Exception
    {
        internal ProtocolException()
        {}

        internal ProtocolException(string message) : base(message)
        {}

        internal ProtocolException(string message, Exception innerException) : base(message, innerException)
        {}
    }
}

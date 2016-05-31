// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    public class ConnectionException : Exception
    {
        internal ConnectionException()
        {
        }

        internal ConnectionException(string message) : base(message)
        {
        }

        internal ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

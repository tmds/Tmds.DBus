// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    public class ConnectException : Exception
    {
        internal ConnectException()
        {
        }

        internal ConnectException(string message) : base(message)
        {
        }

        internal ConnectException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

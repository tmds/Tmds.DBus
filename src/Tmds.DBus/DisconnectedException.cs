// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    public class DisconnectedException : Exception
    {
        public DisconnectedException(Exception innerException) : base(innerException?.Message, innerException)
        {
        }

        internal DisconnectedException()
        {
        }

        internal DisconnectedException(string message) : base(message)
        {
        }

        internal DisconnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    public class DBusException : Exception
    {
        public DBusException(string errorName, string errorMessage) :
            base($"{errorName: errorMessage}")
        {
            this.ErrorName = errorName;
            this.ErrorMessage = errorMessage;
        }

        public string ErrorName { get; }

        public string ErrorMessage { get; }
    }
}

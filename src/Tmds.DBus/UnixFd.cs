// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    public struct UnixFd
    {
        public UnixFd(uint value)
        {
            Value = value;
        }

        public uint Value { get; set; }
    }
}
// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    public class ConnectionStateChangedEventArgs
    {
        public ConnectionStateChangedEventArgs()
        { }

        public string LocalName { get; internal set; }
        public bool RemoteIsBus { get; internal set; }
        public ConnectionState PreviousState { get; internal set; }
        public ConnectionState State { get; internal set; }
        public Exception DisconnectReason { get; internal set; }
    }
}

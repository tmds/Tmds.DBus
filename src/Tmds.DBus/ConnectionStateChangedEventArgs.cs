// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    public struct ConnectionStateChangedEventArgs
    {
        public ConnectionStateChangedEventArgs(ConnectionState state, Exception disconnectReason)
        {
            State = state;
            DisconnectReason = disconnectReason;
            ConnectionInfo = null;
        }

        public ConnectionInfo ConnectionInfo { get; internal set; }
        public ConnectionState State { get; internal set; }
        public Exception DisconnectReason { get; internal set; }
    }
}

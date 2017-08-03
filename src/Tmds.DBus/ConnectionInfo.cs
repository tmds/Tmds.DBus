// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus
{
    public class ConnectionInfo
    {
        internal ConnectionInfo(string localName)
        {
            LocalName = localName;
        }

        public string LocalName { get; }
        public bool RemoteIsBus => !string.IsNullOrEmpty(LocalName);
    }
}

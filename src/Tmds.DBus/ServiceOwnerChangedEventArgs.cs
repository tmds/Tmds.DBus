// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    public class ServiceOwnerChangedEventArgs
    {
        public ServiceOwnerChangedEventArgs(string serviceName, string oldOwner, string newOwner)
        {
            ServiceName = serviceName;
            OldOwner = oldOwner;
            NewOwner = newOwner;
        }
        public string ServiceName { get; }
        public string OldOwner { get; }
        public string NewOwner { get; }
    }
}

// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DBusInterfaceAttribute : Attribute
    {
        public string Name;
        public string GetPropertyMethod;
        public string SetPropertyMethod;
        public string GetAllPropertiesMethod;
        public string WatchPropertiesMethod;

        public DBusInterfaceAttribute(string name)
        {
            Name = name;
            GetAllPropertiesMethod = "GetAllAsync";
            SetPropertyMethod = "SetAsync";
            GetPropertyMethod = "GetAsync";
            WatchPropertiesMethod = "WatchPropertiesAsync";
        }
    }
}

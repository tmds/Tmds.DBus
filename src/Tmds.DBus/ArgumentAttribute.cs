// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    public class ArgumentAttribute : Attribute
    {
        public string Name;

        public ArgumentAttribute(string name)
        {
            Name = name;
        }

        public ArgumentAttribute()
        {
            Name = "value";
        }
    }
}

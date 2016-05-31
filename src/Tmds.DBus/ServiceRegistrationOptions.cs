// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    [Flags]
    public enum ServiceRegistrationOptions
    {
        None = 0,
        ReplaceExisting = 1,
        AllowReplacement = 2,
        Default = ReplaceExisting | AllowReplacement
    }
}

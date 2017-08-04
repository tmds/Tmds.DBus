// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace Tmds.DBus
{
    public struct PropertyChanges
    {
        public KeyValuePair<string, object>[] Changed { get; set; }
        public string[] Invalidated { get; set; }

        public static PropertyChanges ForProperty(string prop, object val)
        {
            return new PropertyChanges()
            {
                Changed = new [] { new KeyValuePair<string, object>(prop, val) },
                Invalidated = Array.Empty<string>()
            };
        }

        public T GetValue<T>(string name)
        {
            for (int i = 0; i < Changed.Length; i++)
            {
                if (Changed[i].Key == name)
                {
                    return (T)Changed[i].Value;
                }
            }
            return default(T);
        }
    }
}
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace Tmds.DBus.CodeGen
{
    internal class InterfaceDescription
    {
        public InterfaceDescription(Type type, string name, IList<MethodDescription> methods, IList<SignalDescription> signals,
            MethodDescription propertyGetMethod, MethodDescription propertyGetAllMethod, MethodDescription propertySetMethod,
            SignalDescription propertiesChangedSignal)
        {
            Type = type;
            Name = name;
            Methods = methods;
            Signals = signals;
            GetAllPropertiesMethod = propertyGetAllMethod;
            SetPropertyMethod = propertySetMethod;
            GetPropertyMethod = propertyGetMethod;
            PropertiesChangedSignal = propertiesChangedSignal;

            if (Signals != null)
            {
                foreach (var signal in Signals)
                {
                    signal.Interface = this;
                }
            }
            if (propertiesChangedSignal != null)
            {
                PropertiesChangedSignal.Interface = this;
            }
            if (Methods != null)
            {
                foreach (var method in Methods)
                {
                    method.Interface = this;
                }
            }
            foreach (var method in new[] { GetPropertyMethod,
                                           SetPropertyMethod,
                                           GetAllPropertiesMethod})
            {
                if (method != null)
                {
                    method.Interface = this;
                }
            }
        }

        public Type Type { get; }
        public string Name { get; }
        public IList<MethodDescription> Methods { get; }
        public IList<SignalDescription> Signals { get; }
        public MethodDescription GetPropertyMethod { get; }
        public MethodDescription GetAllPropertiesMethod { get; }
        public MethodDescription SetPropertyMethod { get; }
        public SignalDescription PropertiesChangedSignal { get; }
    }
}

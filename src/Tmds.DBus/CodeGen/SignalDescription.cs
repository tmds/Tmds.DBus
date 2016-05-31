// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class SignalDescription
    {
        public SignalDescription(MethodInfo method, string name, Type actionType, Type signalType, Signature? signature, IList<ArgumentDescription> arguments)
        {
            MethodInfo = method;
            Name = name;
            ActionType = actionType;
            SignalType = signalType;
            SignalSignature = signature;
            SignalArguments = arguments;
        }

        public InterfaceDescription Interface { get; internal set; }
        public MethodInfo MethodInfo { get; }
        public string Name { get; }
        public Type SignalType { get; }
        public Signature? SignalSignature { get; }
        public IList<ArgumentDescription> SignalArguments { get; }
        public Type ActionType { get; }
    }
}
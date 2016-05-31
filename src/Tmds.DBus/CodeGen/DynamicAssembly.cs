// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace Tmds.DBus.CodeGen
{
    internal class DynamicAssembly
    {
        public static readonly DynamicAssembly Instance = new DynamicAssembly();

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<Type, TypeInfo> _proxyTypeMap;
        private readonly Dictionary<Type, TypeInfo> _adapterTypeMap;
        private readonly object _gate = new object();

        private DynamicAssembly()
        {
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Tmds.DBus.Connection.DynamicAssemblyName), AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(Tmds.DBus.Connection.DynamicAssemblyName);
            _proxyTypeMap = new Dictionary<Type, TypeInfo>();
            _adapterTypeMap = new Dictionary<Type, TypeInfo>();
        }

        public TypeInfo GetProxyTypeInfo(Type interfaceType)
        {
            TypeInfo typeInfo;            
            lock (_proxyTypeMap)
            {
                if (_proxyTypeMap.TryGetValue(interfaceType, out typeInfo))
                {
                    return typeInfo;
                }
            }

            lock (_gate)
            {
                lock (_proxyTypeMap)
                {
                    if (_proxyTypeMap.TryGetValue(interfaceType, out typeInfo))
                    {
                        return typeInfo;
                    }
                }

                typeInfo = new DBusObjectProxyTypeBuilder(_moduleBuilder).Build(interfaceType);

                lock (_proxyTypeMap)
                {
                    _proxyTypeMap[interfaceType] = typeInfo;
                }

                return typeInfo;
            }
        }

        public TypeInfo GetExportTypeInfo(Type objectType)
        {
            TypeInfo typeInfo;

            lock (_adapterTypeMap)
            {
                if (_adapterTypeMap.TryGetValue(objectType, out typeInfo))
                {
                    return typeInfo;
                }
            }

            lock (_gate)
            {
                lock (_adapterTypeMap)
                {
                    if (_adapterTypeMap.TryGetValue(objectType, out typeInfo))
                    {
                        return typeInfo;
                    }
                }

                typeInfo = new DBusAdapterTypeBuilder(_moduleBuilder).Build(objectType);

                lock (_adapterTypeMap)
                {
                    _adapterTypeMap[objectType] = typeInfo;
                }

                return typeInfo;
            }
        }
    }
}

// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tmds.DBus.CodeGen
{
    static internal class ArgTypeInspector
    {
        private static readonly Type s_idbusObjectType = typeof(IDBusObject);
        private static readonly Type s_idictionaryGenericType = typeof(IDictionary<,>);
        private static readonly Type s_keyValueGenericPairType = typeof(KeyValuePair<,>);
        private static readonly Type s_ienumerableGenricType = typeof(IEnumerable<>);
        private static readonly Type s_ienumerableType = typeof(System.Collections.IEnumerable);
        private static readonly Type s_objectType = typeof(Object);
        private static readonly Type s_valueType = typeof(ValueType);
        private static readonly Type s_ilistGenricType = typeof(IList<>);
        private static readonly Type s_icollectionGenricType = typeof(ICollection<>);

        public static bool IsDBusObjectType(Type type, bool isCompileTimeType)
        {
            if (type == s_idbusObjectType)
            {
                return true;
            }
            var typeInfo = type.GetTypeInfo();
            if (isCompileTimeType)
            {
                return typeInfo.IsInterface && typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType);
            }
            else
            {
                return typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType);
            }
        }

        public static bool IsEnumerableType(Type type, out Type elementType, out bool isDictionaryType, bool isCompileTimeType)
        {
            elementType = null;
            isDictionaryType = false; // we only set this when isCompileTimeType==true
            var typeInfo = type.GetTypeInfo();

            if (isCompileTimeType)
            {
                if (typeInfo.IsArray)
                {
                    elementType = typeInfo.GetElementType();
                    return true;
                }
                if (typeInfo.IsInterface && typeInfo.IsGenericType)
                {
                    var genericTypeDefinition = typeInfo.GetGenericTypeDefinition();
                    if (genericTypeDefinition == s_idictionaryGenericType)
                    {
                        isDictionaryType = true;
                        elementType = s_keyValueGenericPairType.MakeGenericType(typeInfo.GenericTypeArguments);
                        return true;
                    }
                    else if (genericTypeDefinition == s_ienumerableGenricType ||
                             genericTypeDefinition == s_ilistGenricType ||
                             genericTypeDefinition == s_icollectionGenricType)
                    {
                        elementType = typeInfo.GenericTypeArguments[0];
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType))
                {
                    return false;
                }

                if (!typeInfo.ImplementedInterfaces.Contains(s_ienumerableType))
                {
                    return false;
                }

                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == s_ienumerableGenricType)
                {
                    elementType = typeInfo.GenericTypeArguments[0];
                    return true;
                }

                var enumerableTypes = from interf in typeInfo.ImplementedInterfaces
                                    let interfTypeinfo = interf.GetTypeInfo()
                                    where interfTypeinfo.IsGenericType && interfTypeinfo.GetGenericTypeDefinition() == s_ienumerableGenricType
                                    select interfTypeinfo;

                var enumerableCount = enumerableTypes.Count();

                if (enumerableCount == 1)
                {
                    elementType = enumerableTypes.First().GenericTypeArguments[0];
                    return true;
                }
                else
                {
                    throw new ArgumentException($"Cannot determine element type of enumerable type '$type.FullName'");
                }
            }
        }

        public static bool IsStructType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsPointer ||
                typeInfo.IsInterface ||
                typeInfo.IsArray ||
                typeInfo.IsPrimitive ||
                typeInfo.IsAbstract ||
                !typeInfo.IsLayoutSequential)
            {
                return false;
            }


            if (typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType))
            {
                return false;
            }

            if (typeInfo.ImplementedInterfaces.Contains(s_ienumerableType))
            {
                return false;
            }

            if (typeInfo.BaseType != s_objectType &&
                typeInfo.BaseType != s_valueType)
            {
                return false;
            }

            var fields = type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (!fields.Any())
            {
                return false;
            }

            return true;
        }

        public static bool IsKeyValuePairType(Type elementType)
        {
            var typeInfo = elementType.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == s_keyValueGenericPairType;
        }
    }
}
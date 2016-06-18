// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class TypeDescription
    {
        private static readonly Type s_signalReturnType = typeof(Task<IDisposable>);
        private static readonly Type s_idbusObjectType = typeof(IDBusObject);
        private static readonly Type s_emptyActionType = typeof(Action);
        private static readonly Type s_singleParameterActionType = typeof(Action<>);
        private static readonly Type s_emptyTaskType = typeof(Task);
        private static readonly Type s_parameterTaskType = typeof(Task<>);
        private static readonly Type s_cancellationTokenType = typeof(CancellationToken);
        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_objectType = typeof(object);
        private static readonly Signature s_propertiesChangedSignature = new Signature("a{sv}as");
        private static readonly Signature s_getAllOutSignature = new Signature("a{sv}");
        private static readonly Type[] s_mappedTypes = new[] { typeof(bool), typeof(byte), typeof(double), typeof(short), typeof(int),
            typeof(long), typeof(ObjectPath), typeof(Signature), typeof(float), typeof(string), typeof(ushort), typeof(uint), typeof(ulong),
            typeof(object), typeof(IDBusObject)};

        public Type Type { get; }
        public IList<InterfaceDescription> Interfaces { get; }

        public static TypeDescription DescribeObject(Type type)
        {
            return Describe(type, isInterfaceType: false);
        }

        public static TypeDescription DescribeInterface(Type type)
        {
            return Describe(type, isInterfaceType : true);
        }

        private TypeDescription(Type type, IList<InterfaceDescription> interfaces)
        {
            Type = type;
            Interfaces = interfaces;
        }

        private static TypeDescription Describe(Type type, bool isInterfaceType)
        {
            var interfaces = new List<InterfaceDescription>();
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface != isInterfaceType)
            {
                if (isInterfaceType)
                {
                    throw new ArgumentException($"Type '{type.FullName}' must be an interface type");
                }
                else
                {
                    throw new ArgumentException($"Type '{type.FullName}' cannot be an interface type");
                }
            }


            if ((type != s_idbusObjectType)
                && !typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType))
            {
                throw new ArgumentException($"Type {type.FullName} does not implement {typeof(IDBusObject).FullName}");
            }

            if (!isInterfaceType)
            {
                var dbusInterfaces = from interf in typeInfo.ImplementedInterfaces
                                 let interfAttribute = interf.GetTypeInfo().GetCustomAttribute<DBusInterfaceAttribute>(false)
                                 where interfAttribute != null
                                 select new { Type = interf, Attribute = interfAttribute };

                foreach (var dbusInterf in dbusInterfaces)
                {
                    AddInterfaceDescription(dbusInterf.Type, dbusInterf.Attribute, interfaces);
                }
            }
            else if (type == s_idbusObjectType)
            {}
            else
            {
                var interfaceAttribute = typeInfo.GetCustomAttribute<DBusInterfaceAttribute>(false);
                if (interfaceAttribute != null)
                {
                    AddInterfaceDescription(type, interfaceAttribute, interfaces);
                }
                else
                {
                    if (typeInfo.DeclaredMembers.Count() != 0)
                    {
                        throw new ArgumentException($"DBus object type {type.FullName} cannot implement methods. It must inherit one or more DBus interface types.");
                    }
                }

                var dbusInterfaces = from interf in typeInfo.ImplementedInterfaces
                                     where interf != s_idbusObjectType
                                     let interfAttribute = interf.GetTypeInfo().GetCustomAttribute<DBusInterfaceAttribute>(false)
                                     select new { Type = interf, Attribute = interfAttribute };

                foreach (var dbusInterf in dbusInterfaces)
                {
                    AddInterfaceDescription(dbusInterf.Type, dbusInterf.Attribute, interfaces);
                }

                if (dbusInterfaces.Any(interf => interf.Attribute == null))
                {
                    throw new ArgumentException($"DBus object type {type.FullName} inherits one or more interfaces which are not DBus interface types.");
                }

                if ((interfaces.Count == 0) && (!typeInfo.ImplementedInterfaces.Contains(s_idbusObjectType)))
                {
                    throw new ArgumentException($"Type {type.FullName} does not inherit '{s_idbusObjectType.FullName}' or any interfaces with the {typeof(DBusInterfaceAttribute).FullName}.");
                }
            }
            return new TypeDescription(type, interfaces);
        }

        private static void AddInterfaceDescription(Type type, DBusInterfaceAttribute interfaceAttribute, List<InterfaceDescription> interfaces)
        {
            if (interfaces.Any(interf => interf.Name == interfaceAttribute.Name))
            {
                throw new ArgumentException($"DBus interface {interfaceAttribute.Name} is inherited multiple times");
            }

            IList<MethodDescription> methods = null;
            IList<SignalDescription> signals = null;
            MethodDescription propertyGetMethod = null;
            MethodDescription propertySetMethod = null;
            MethodDescription propertyGetAllMethod = null;
            SignalDescription propertiesChangedSignal = null;

            foreach (var member in type.GetMethods())
            {
                string memberName = member.ToString();
                if (!member.Name.EndsWith("Async"))
                {
                    throw new ArgumentException($"{memberName} does not end with 'Async'");
                }
                var isSignal = member.Name.StartsWith("Watch");
                if (isSignal)
                {
                    if (member.ReturnType != s_signalReturnType)
                    {
                        throw new ArgumentException($"Signal {memberName} does not return 'Task<IDisposable>'");
                    }

                    var name = member.Name.Substring(5, member.Name.Length - 10);
                    if (name.Length == 0)
                    {
                        throw new ArgumentException($"Signal {memberName} has an empty name");
                    }

                    Signature? parameterSignature = null;
                    IList<ArgumentDescription> arguments = null;
                    var parameters = member.GetParameters();
                    var actionParameter = parameters.Length > 0 ? parameters[0] : null;
                    Type parameterType = null;
                    bool valid = false;
                    if (actionParameter != null)
                    {
                        if (actionParameter.ParameterType == s_emptyActionType)
                        {
                            valid = true;
                            parameterType = null;
                        }
                        else if (actionParameter.ParameterType.GetTypeInfo().IsGenericType
                                 && actionParameter.ParameterType.GetGenericTypeDefinition() == s_singleParameterActionType)
                        {
                            valid = true;
                            parameterType = actionParameter.ParameterType.GetGenericArguments()[0];
                            var argumentAttribute = actionParameter.GetCustomAttribute<ArgumentAttribute>(false);
                            InspectParameterType(parameterType, argumentAttribute, out parameterSignature, out arguments);
                        }
                    }
                    if (!valid || parameters.Length != 2 || parameters[1].ParameterType != s_cancellationTokenType)
                    {;
                        throw new ArgumentException($"Signal {memberName} must accept a first argument of Type 'Action'/'Action<>' and a second argument of Type 'CancellationToken'");
                    }

                    var signal = new SignalDescription(member, name, actionParameter.ParameterType, parameterType, parameterSignature, arguments);
                    if (member.Name == interfaceAttribute.WatchPropertiesMethod)
                    {
                        if (propertiesChangedSignal != null)
                        {
                            throw new ArgumentException($"Multiple property changes signals are declared: {memberName}, {propertyGetMethod.MethodInfo.ToString()}");
                        }
                        propertiesChangedSignal = signal;
                        if (propertiesChangedSignal.SignalSignature != s_propertiesChangedSignature)
                        {
                            throw new ArgumentException($"PropertiesChanged signal {memberName} must accept an Action<T> where T is a struct with an IDictionary<string, object> and an string[] field");
                        }
                    }
                    else
                    {
                        signals = signals ?? new List<SignalDescription>();
                        signals.Add(signal);
                    }
                }
                else
                {
                    var name = member.Name.Substring(0, member.Name.Length - 5);
                    if (name.Length == 0)
                    {
                        throw new ArgumentException($"DBus Method {memberName} has an empty name");
                    }

                    IList<ArgumentDescription> outArguments = null;
                    Signature? outSignature = null;
                    var taskParameter = member.ReturnType;
                    Type outType = null;
                    bool valid = false;
                    bool isGenericOut = false;
                    if (taskParameter != null)
                    {
                        if (taskParameter == s_emptyTaskType)
                        {
                            valid = true;
                            outType = null;
                        }
                        else if (taskParameter.GetTypeInfo().IsGenericType
                                 && taskParameter.GetGenericTypeDefinition() == s_parameterTaskType)
                        {
                            valid = true;
                            outType = taskParameter.GetGenericArguments()[0];
                            var argumentAttribute = member.ReturnParameter.GetCustomAttribute<ArgumentAttribute>(false);
                            if (outType.IsGenericParameter)
                            {
                                outType = s_objectType;
                                isGenericOut = true;
                            }
                            InspectParameterType(outType, argumentAttribute, out outSignature, out outArguments);
                        }
                    }
                    if (!valid)
                    {
                        throw new ArgumentException($"DBus Method {memberName} does not return 'Task'/'Task<>'");
                    }

                    IList<ArgumentDescription> inArguments = null;
                    Signature? inSignature = null;
                    var parameters = member.GetParameters();
                    if (parameters.Length == 0 || parameters[parameters.Length - 1].ParameterType != s_cancellationTokenType)
                    {
                        throw new ArgumentException($"DBus Method {memberName} must accept 'CancellationToken' as a last argument");
                    }
                    for (int i = 0; i < (parameters.Length - 1); i++)
                    {
                        var param = parameters[i];
                        var parameterType = param.ParameterType;
                        var paramSignature = Signature.GetSig(parameterType, isCompileTimeType: true);
                        if (inSignature == null)
                        {
                            inSignature = paramSignature;
                        }
                        else
                        {
                            inSignature += paramSignature;
                        }
                        inArguments = inArguments ?? new List<ArgumentDescription>();
                        var argumentAttribute = param.GetCustomAttribute<ArgumentAttribute>(false);
                        var argName = argumentAttribute != null ? argumentAttribute.Name : param.Name;
                        inArguments.Add(new ArgumentDescription(argName, paramSignature, parameterType));
                    }

                    var methodDescription = new MethodDescription(member, name, inArguments, inSignature, outType, isGenericOut, outSignature, outArguments);
                    if (member.Name == interfaceAttribute.GetPropertyMethod)
                    {
                        if (propertyGetMethod != null)
                        {
                            throw new ArgumentException($"Multiple property Get methods are declared: {memberName}, {propertyGetMethod.MethodInfo.ToString()}");
                        }
                        propertyGetMethod = methodDescription;
                        if ((propertyGetMethod.InSignature != Signature.StringSig) ||
                            (propertyGetMethod.OutSignature != Signature.VariantSig))
                        {
                            throw new ArgumentException($"Property Get method {memberName} must accept a 'string' parameter and return 'Task<object>'");
                        }
                    }
                    else if (member.Name == interfaceAttribute.GetAllPropertiesMethod)
                    {
                        if (propertyGetAllMethod != null)
                        {
                            throw new ArgumentException($"Multiple property GetAll are declared: {memberName}, {propertyGetAllMethod.MethodInfo.ToString()}");
                        }
                        propertyGetAllMethod = methodDescription;
                        if ((propertyGetAllMethod.InArguments != null) ||
                            (propertyGetAllMethod.OutSignature != s_getAllOutSignature))
                        {
                            throw new ArgumentException($"Property GetAll method {memberName} must accept no parameters and return 'Task<IDictionary<string, object>>'");
                        }
                    }
                    else if (member.Name == interfaceAttribute.SetPropertyMethod)
                    {
                        if (propertySetMethod != null)
                        {
                            throw new ArgumentException($"Multiple property Set are declared: {memberName}, {propertySetMethod.MethodInfo.ToString()}");
                        }
                        propertySetMethod = methodDescription;
                        if ((propertySetMethod.InArguments?.Count != 2 || propertySetMethod.InArguments[0].Type != s_stringType || propertySetMethod.InArguments[1].Type != s_objectType) ||
                            (propertySetMethod.OutArguments != null))
                        {
                            throw new ArgumentException($"Property Set method {memberName} must accept a 'string' and 'object' parameter and return 'Task'");
                        }
                    }
                    else
                    {
                        methods = methods ?? new List<MethodDescription>();
                        methods.Add(methodDescription);
                    }
                }
            }
            interfaces.Add(new InterfaceDescription(type, interfaceAttribute.Name, methods, signals,
                                propertyGetMethod, propertyGetAllMethod, propertySetMethod, propertiesChangedSignal));
        }

        private static void InspectParameterType(Type parameterType, ArgumentAttribute argumentAttribute, out Signature? signature, out IList<ArgumentDescription> arguments)
        {
            arguments = new List<ArgumentDescription>();
            if (argumentAttribute != null)
            {
                signature = Signature.GetSig(parameterType, isCompileTimeType: true);
                arguments.Add(new ArgumentDescription(argumentAttribute.Name, signature, parameterType));
            }
            else if (IsStructType(parameterType))
            {
                signature = null;
                foreach (var field in parameterType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var fieldType = field.FieldType;
                    var argumentSignature = Signature.GetSig(fieldType, isCompileTimeType: true);
                    arguments.Add(new ArgumentDescription(field.Name, argumentSignature, fieldType));
                    if (signature == null)
                    {
                        signature = argumentSignature;
                    }
                    else
                    {
                        signature += argumentSignature;
                    }
                }
            }
            else
            {
                signature = Signature.GetSig(parameterType, isCompileTimeType: true);
                arguments.Add(new ArgumentDescription("value", signature, parameterType));
            }
        }

        private static bool IsStructType(Type type)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                return false;
            }
            if (s_mappedTypes.Contains(type))
            {
                return false;
            }

            if (ArgTypeInspector.IsDBusObjectType(type, isCompileTimeType: true))
            {
                return false;
            }
            Type elementType;
            if (ArgTypeInspector.InspectEnumerableType(type, out elementType, isCompileTimeType: true)
                    != ArgTypeInspector.EnumerableType.NotEnumerable)
            {
                return false;
            }

            return ArgTypeInspector.IsStructType(type);
        }
    }
}

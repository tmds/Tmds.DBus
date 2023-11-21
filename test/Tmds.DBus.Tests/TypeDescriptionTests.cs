using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Tmds.DBus.CodeGen;
using Tmds.DBus.Protocol;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class TypeDescriptionTests
    {
        [Theory]
        [InlineData(typeof(IEmptyDBusInterface), true)]
        [InlineData(typeof(IEmptyInterface), false)]
        [InlineData(typeof(IValidDBusObjectInterface), true)]
        [InlineData(typeof(IValidDBusObjectInterface2), true)]
        [InlineData(typeof(EmptyDBusObject), false)]
        [InlineData(typeof(EmptyObject), false)]
        [InlineData(typeof(ValidDBusObject), false)]
        [InlineData(typeof(ValidDBusObject2), false)]
        [InlineData(typeof(IDBusObject), true)]
        [InlineData(typeof(IInvalidDBusObjectInterface1), false)]
        public void ValidInterfaceType(Type type, bool expectedValid)
        {
            bool valid = true;
            try
            {
                var description = TypeDescription.DescribeInterface(type);
            }
            catch (System.Exception e)
            {
                Assert.IsAssignableFrom<ArgumentException>(e);
                valid = false;
            }
            Assert.Equal(expectedValid, valid);
        }

        [Theory]
        [InlineData(typeof(EmptyDBusObject), true)]
        [InlineData(typeof(EmptyObject), false)]
        [InlineData(typeof(ValidDBusObject), true)]
        [InlineData(typeof(ValidDBusObject2), true)]
        [InlineData(typeof(IEmptyDBusInterface), false)]
        [InlineData(typeof(IEmptyInterface), false)]
        [InlineData(typeof(IValidDBusObjectInterface), false)]
        [InlineData(typeof(IValidDBusObjectInterface2), false)]
        [InlineData(typeof(IDBusObject), false)]
        public void ValidObjectType(Type type, bool expectedValid)
        {
            bool valid = true;
            try
            {
                var description = TypeDescription.DescribeObject(type);
            }
            catch (System.Exception e)
            {
                Assert.IsAssignableFrom<ArgumentException>(e);
                valid = false;
            }
            Assert.Equal(expectedValid, valid);
        }

        [Theory]
        [InlineData(typeof(IValidDBusMethod1), true)]
        [InlineData(typeof(IValidDBusMethod2), true)]
        [InlineData(typeof(IValidDBusMethod3), true)]
        [InlineData(typeof(IInvalidDBusMethod1), false)]
        [InlineData(typeof(IInvalidDBusMethod3), false)]
        [InlineData(typeof(IInvalidDBusMethod4), false)]
        [InlineData(typeof(IInvalidDBusMethod5), false)]
        [InlineData(typeof(IValidSignal1), true)]
        [InlineData(typeof(IValidSignal2), true)]
        [InlineData(typeof(IInvalidSignal2), false)]
        [InlineData(typeof(IInvalidSignal3), false)]
        [InlineData(typeof(IInvalidSignal4), false)]
        [InlineData(typeof(IInvalidSignal5), false)]
        [InlineData(typeof(IInvalidSignal6), false)]
        [InlineData(typeof(IInvalidSignal7), false)]
        [InlineData(typeof(IInvalidSignal8), false)]
        [InlineData(typeof(IInvalidSignal9), false)]
        public void ValidMembers(Type type, bool expectedValid)
        {
            bool valid = true;
            try
            {
                var description = TypeDescription.DescribeInterface(type);
            }
            catch (System.Exception e)
            {
                Assert.IsAssignableFrom<ArgumentException>(e);
                valid = false;
            }
            Assert.Equal(expectedValid, valid);
        }

        [Fact]
        public void TypeAndInterfaceDescription()
        {
            var description = TypeDescription.DescribeInterface(typeof(IValidDBusObjectInterface));

            Assert.Equal(typeof(IValidDBusObjectInterface), description.Type);
            Assert.Equal(2, description.Interfaces.Count);
            Assert.Equal("tmds.dbus.tests.empty", description.Interfaces[0].Name);
            Assert.Equal("tmds.dbus.tests.empty2", description.Interfaces[1].Name);
            Assert.Equal(typeof(IEmptyDBusInterface), description.Interfaces[0].Type);
            Assert.Equal(typeof(IEmptyDBusInterface2), description.Interfaces[1].Type);
        }

        [Fact]
        public void SignalDescription()
        {
            // Task<IDisposable> WatchSomethingAsync(Action a);
            var description = TypeDescription.DescribeInterface(typeof(IValidSignal1));
            var signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.False(signalDescription.HasOnError);
            Assert.Null(signalDescription.SignalType);
            Assert.Equal(typeof(Action), signalDescription.ActionType);
            Assert.Equal((Signature?)null, signalDescription.SignalSignature);
            Assert.Equal(0, signalDescription.SignalArguments.Count);
            Assert.Equal(typeof(IValidSignal1).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);

            // Task<IDisposable> WatchSomethingAsync(Action<int> a);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal2));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.False(signalDescription.HasOnError);
            Assert.Equal(typeof(int), signalDescription.SignalType);
            Assert.Equal(typeof(Action<int>), signalDescription.ActionType);
            Assert.Equal("i", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal2).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(1, signalDescription.SignalArguments.Count);
            var argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("value", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);

            // Task<IDisposable> WatchSomethingAsync(Action<IntPair> a);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal3));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.Equal(typeof(IntPair), signalDescription.SignalType);
            Assert.Equal(typeof(Action<IntPair>), signalDescription.ActionType);
            Assert.Equal("ii", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal3).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(2, signalDescription.SignalArguments.Count);
            argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("arg1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = signalDescription.SignalArguments[1];
            Assert.Equal("arg2", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);

            //Task<IDisposable> WatchSomethingAsync([Argument("myArg")]Action<IntPair> a);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal4));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.Equal(typeof(IntPair), signalDescription.SignalType);
            Assert.Equal(typeof(Action<IntPair>), signalDescription.ActionType);
            Assert.Equal("(ii)", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal4).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(1, signalDescription.SignalArguments.Count);
            argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("myArg", argDescription.Name);
            Assert.Equal(typeof(IntPair), argDescription.Type);
            Assert.Equal("(ii)", argDescription.Signature);

            // Task<IDisposable> WatchSomethingAsync(Action<(int arg1, string arg2)> a);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal5));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), signalDescription.SignalType);
            Assert.Equal(typeof(Action<ValueTuple<int, string>>), signalDescription.ActionType);
            Assert.Equal("is", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal5).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(2, signalDescription.SignalArguments.Count);
            argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("arg1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = signalDescription.SignalArguments[1];
            Assert.Equal("arg2", argDescription.Name);
            Assert.Equal(typeof(string), argDescription.Type);
            Assert.Equal("s", argDescription.Signature);

            // Task<IDisposable> WatchSomethingAsync(Action<ValueTuple<int, string>> a);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal6));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), signalDescription.SignalType);
            Assert.Equal(typeof(Action<ValueTuple<int, string>>), signalDescription.ActionType);
            Assert.Equal("is", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal6).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(2, signalDescription.SignalArguments.Count);
            argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("Item1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = signalDescription.SignalArguments[1];
            Assert.Equal("Item2", argDescription.Name);
            Assert.Equal(typeof(string), argDescription.Type);
            Assert.Equal("s", argDescription.Signature);

            // Task<IDisposable> WatchSomethingAsync([Argument("myArg")]Action<(int arg1, string arg2)> a);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal7));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), signalDescription.SignalType);
            Assert.Equal(typeof(Action<ValueTuple<int, string>>), signalDescription.ActionType);
            Assert.Equal("(is)", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal7).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(1, signalDescription.SignalArguments.Count);
            argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("myArg", argDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), argDescription.Type);
            Assert.Equal("(is)", argDescription.Signature);

            // Task<IDisposable> WatchSomethingAsync(Action a, Action<Exception>);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal8));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.True(signalDescription.HasOnError);
            Assert.Null(signalDescription.SignalType);
            Assert.Equal(typeof(Action), signalDescription.ActionType);
            Assert.Equal((Signature?)null, signalDescription.SignalSignature);
            Assert.Equal(0, signalDescription.SignalArguments.Count);
            Assert.Equal(typeof(IValidSignal8).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);

            // Task<IDisposable> WatchSomethingAsync(Action<int> a, Action<Exception>);
            description = TypeDescription.DescribeInterface(typeof(IValidSignal9));
            signalDescription = description.Interfaces[0].Signals[0];
            Assert.Equal("Something", signalDescription.Name);
            Assert.True(signalDescription.HasOnError);
            Assert.Equal(typeof(int), signalDescription.SignalType);
            Assert.Equal(typeof(Action<int>), signalDescription.ActionType);
            Assert.Equal("i", signalDescription.SignalSignature);
            Assert.Equal(typeof(IValidSignal9).GetTypeInfo().GetMethod("WatchSomethingAsync"), signalDescription.MethodInfo);
            Assert.NotNull(signalDescription.SignalArguments);
            Assert.Equal(1, signalDescription.SignalArguments.Count);
            argDescription = signalDescription.SignalArguments[0];
            Assert.Equal("value", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
        }

        [Fact]
        public void MethodDescription()
        {
            // Task FooAsync();
            var description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod1));
            var methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod1).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);

            // Task FooAsync(int arg1);
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod2));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal("i", methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod2).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.InArguments);
            Assert.Equal(1, methodDescription.InArguments.Count);
            var argDescription = methodDescription.InArguments[0];
            Assert.Equal("arg1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);

            // Task FooAsync(int arg1, int arg2);
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod3));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal("ii", methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod3).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.InArguments);
            Assert.Equal(2, methodDescription.InArguments.Count);
            argDescription = methodDescription.InArguments[0];
            Assert.Equal("arg1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = methodDescription.InArguments[1];
            Assert.Equal("arg2", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);

            // Task FooAsync(IntPair val);
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod4));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal("(ii)", methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod4).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.InArguments);
            Assert.Equal(1, methodDescription.InArguments.Count);
            argDescription = methodDescription.InArguments[0];
            Assert.Equal("val", argDescription.Name);
            Assert.Equal(typeof(IntPair), argDescription.Type);
            Assert.Equal("(ii)", argDescription.Signature);

            // Task FooAsync([Argument("arg")]IntPair val);
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod5));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal("(ii)", methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod5).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.InArguments);
            Assert.Equal(1, methodDescription.InArguments.Count);
            argDescription = methodDescription.InArguments[0];
            Assert.Equal("arg", argDescription.Name);
            Assert.Equal(typeof(IntPair), argDescription.Type);
            Assert.Equal("(ii)", argDescription.Signature);

            // Task<int> FooAsync();
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod6));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Equal(typeof(int), methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal("i", methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod6).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.OutArguments);
            Assert.Equal(1, methodDescription.OutArguments.Count);
            argDescription = methodDescription.OutArguments[0];
            Assert.Equal("value", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);

            // Task<IntPair> FooAsync();
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod7));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Equal(typeof(IntPair), methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal("ii", methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod7).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.OutArguments);
            Assert.Equal(2, methodDescription.OutArguments.Count);
            argDescription = methodDescription.OutArguments[0];
            Assert.Equal("arg1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = methodDescription.OutArguments[1];
            Assert.Equal("arg2", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);

            // [return: Argument("arg")]
            // Task<IntPair> FooAsync();
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod8));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Equal(typeof(IntPair), methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal("(ii)", methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod8).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.OutArguments);
            Assert.Equal(1, methodDescription.OutArguments.Count);
            argDescription = methodDescription.OutArguments[0];
            Assert.Equal("arg", argDescription.Name);
            Assert.Equal(typeof(IntPair), argDescription.Type);
            Assert.Equal("(ii)", argDescription.Signature);

            // Task<(int arg1, string arg2)> FooAsync();
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod9));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal("is", methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod9).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.OutArguments);
            Assert.Equal(2, methodDescription.OutArguments.Count);
            argDescription = methodDescription.OutArguments[0];
            Assert.Equal("arg1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = methodDescription.OutArguments[1];
            Assert.Equal("arg2", argDescription.Name);
            Assert.Equal(typeof(string), argDescription.Type);
            Assert.Equal("s", argDescription.Signature);

            // Task<ValueTuple<int, string>> FooAsync();
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod10));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal("is", methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod10).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.OutArguments);
            Assert.Equal(2, methodDescription.OutArguments.Count);
            argDescription = methodDescription.OutArguments[0];
            Assert.Equal("Item1", argDescription.Name);
            Assert.Equal(typeof(int), argDescription.Type);
            Assert.Equal("i", argDescription.Signature);
            argDescription = methodDescription.OutArguments[1];
            Assert.Equal("Item2", argDescription.Name);
            Assert.Equal(typeof(string), argDescription.Type);
            Assert.Equal("s", argDescription.Signature);

            // [return: Argument("arg")]
            // Task<(int arg1, string arg2)> FooAsync();
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod11));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), methodDescription.OutType);
            Assert.Equal((Signature?)null, methodDescription.InSignature);
            Assert.Equal("(is)", methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.InArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod11).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.OutArguments);
            Assert.Equal(1, methodDescription.OutArguments.Count);
            argDescription = methodDescription.OutArguments[0];
            Assert.Equal("arg", argDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), argDescription.Type);
            Assert.Equal("(is)", argDescription.Signature);

            // Task FooAsync(ValueTuple<int, string> val);
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod12));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal("(is)", methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod12).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.InArguments);
            Assert.Equal(1, methodDescription.InArguments.Count);
            argDescription = methodDescription.InArguments[0];
            Assert.Equal("val", argDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), argDescription.Type);
            Assert.Equal("(is)", argDescription.Signature);

            // Task FooAsync([Argument("arg")]ValueTuple<int, string> val);
            description = TypeDescription.DescribeInterface(typeof(IValidDBusMethod13));
            methodDescription = description.Interfaces[0].Methods[0];
            Assert.Equal("Foo",  methodDescription.Name);
            Assert.Null(methodDescription.OutType);
            Assert.Equal("(is)", methodDescription.InSignature);
            Assert.Equal((Signature?)null, methodDescription.OutSignature);
            Assert.Equal(0, methodDescription.OutArguments.Count);
            Assert.Equal(typeof(IValidDBusMethod13).GetTypeInfo().GetMethod("FooAsync"), methodDescription.MethodInfo);
            Assert.NotNull(methodDescription.InArguments);
            Assert.Equal(1, methodDescription.InArguments.Count);
            argDescription = methodDescription.InArguments[0];
            Assert.Equal("arg", argDescription.Name);
            Assert.Equal(typeof(ValueTuple<int, string>), argDescription.Type);
            Assert.Equal("(is)", argDescription.Signature);
        }

        [Theory]
        [InlineData(typeof(IUnsupportedDBusMethod1))]
        [InlineData(typeof(IUnsupportedSignal1))]
        public void UnsupportedInterfaces(Type type)
        {
            Assert.Throws<NotSupportedException>(() => TypeDescription.DescribeInterface(type));
        }

        [Dictionary]
        public class PersonProperties
        {
            public string Name;
            public int? Age;
            public bool IsMarried;
        }


        [Theory]
        [InlineData(typeof(IPersonProperties1))]
        [InlineData(typeof(IPersonProperties2))]
        public void Properties(Type type)
        {
            var description = TypeDescription.DescribeInterface(type);
            var interf = description.Interfaces[0];
            Assert.NotNull(interf.Properties);
            var nameProperty = interf.Properties.Where(p => p.Name == "Name").SingleOrDefault();
            Assert.NotNull(nameProperty);
            Assert.Equal("s", nameProperty.Signature);
            var ageProperty = interf.Properties.Where(p => p.Name == "Age").SingleOrDefault();
            Assert.NotNull(ageProperty);
            Assert.Equal("i", ageProperty.Signature);
            var isMarriedProperty = interf.Properties.Where(p => p.Name == "IsMarried").SingleOrDefault();
            Assert.NotNull(isMarriedProperty);
            Assert.Equal("b", isMarriedProperty.Signature);
        }

        [DBusInterface("tmds.dbus.tests.personproperties1")]
        interface IPersonProperties1 : IDBusObject
        {
            Task<PersonProperties> GetAllAsync();
        }

        [DBusInterface("tmds.dbus.tests.personproperties2", PropertyType = typeof(PersonProperties))]
        interface IPersonProperties2 : IDBusObject
        {}

        [DBusInterface("tmds.dbus.tests.empty")]
        interface IEmptyDBusInterface : IDBusObject
        {}
        [DBusInterface("tmds.dbus.tests.empty")]
        interface IEmptyDBusInterfaceDuplicate : IDBusObject
        {}
        [DBusInterface("tmds.dbus.tests.empty2")]
        interface IEmptyDBusInterface2 : IDBusObject
        {}
        interface IEmptyInterface
        {}
        interface IValidDBusObjectInterface : IEmptyDBusInterface, IEmptyDBusInterface2, IDBusObject
        {}
        interface IValidDBusObjectInterface2 : IDBusObject
        {}
        class EmptyDBusObject : IEmptyDBusInterface, IDBusObject
        {
            public ObjectPath ObjectPath { get { throw new NotImplementedException(); } }
        }
        class EmptyObject : IEmptyInterface
        {}
        class ValidDBusObject : IValidDBusObjectInterface
        {
            public ObjectPath ObjectPath { get { throw new NotImplementedException(); } }
        }
        class ValidDBusObject2 : IValidDBusObjectInterface2
        {
            public ObjectPath ObjectPath { get { throw new NotImplementedException(); } }
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod1 : IDBusObject
        {
            Task FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod2 : IDBusObject
        {
            Task FooAsync(int arg1);
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod3 : IDBusObject
        {
            Task FooAsync(int arg1, int arg2);
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod4 : IDBusObject
        {
            Task FooAsync(IntPair val);
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod5 : IDBusObject
        {
            Task FooAsync([Argument("arg")]IntPair val);
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod6 : IDBusObject
        {
            Task<int> FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod7 : IDBusObject
        {
            Task<IntPair> FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod8 : IDBusObject
        {
            [return: Argument("arg")]
            Task<IntPair> FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod9 : IDBusObject
        {
            Task<(int arg1, string arg2)> FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod10 : IDBusObject
        {
            Task<ValueTuple<int, string>> FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod11 : IDBusObject
        {
            [return: Argument("arg")]
            Task<(int arg1, string arg2)> FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod12 : IDBusObject
        {
            Task FooAsync(ValueTuple<int, string> val);
        }
        [DBusInterface("tmds.dbus.tests.validmethod")]
        interface IValidDBusMethod13 : IDBusObject
        {
            Task FooAsync([Argument("arg")]ValueTuple<int, string> val);
        }

        [DBusInterface("tmds.dbus.tests.invalidmethod")]
        interface IInvalidDBusMethod1 : IDBusObject
        {
            Task Foo();
        }
        [DBusInterface("tmds.dbus.tests.invalidmethod")]
        interface IInvalidDBusMethod3 : IDBusObject
        {
            int FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.invalidmethod")]
        interface IInvalidDBusMethod4 : IDBusObject
        {
            void FooAsync();
        }
        [DBusInterface("tmds.dbus.tests.invalidmethod")]
        interface IInvalidDBusMethod5 : IDBusObject
        {
            void Async();
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal1 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action a);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal2 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action<int> a);
        }
#pragma warning disable 0649 // Field 'is never assigned to, and will always have its default value 0
        struct IntPair
        {
            public int arg1;
            public int arg2;
        }
#pragma warning restore
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal3 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action<IntPair> a);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal4 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync([Argument("myArg")]Action<IntPair> a);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal5 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action<(int arg1, string arg2)> a);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal6 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action<ValueTuple<int, string>> a);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal7 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync([Argument("myArg")]Action<(int arg1, string arg2)> a);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal8 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action a, Action<Exception> error);
        }
        [DBusInterface("tmds.dbus.tests.validsignal")]
        interface IValidSignal9 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action<int> a, Action<Exception> error);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal2 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync();
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal3 : IDBusObject
        {
            Task<IDisposable> WatchSomething(Action a);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal4 : IDBusObject
        {
            Task<IDisposable> WatchAsync(Action a);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal5 : IDBusObject
        {
            Task<IDisposable> SomethingAsync(Action a);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal6 : IDBusObject
        {
            Task WatchSomethingAsync(Action a);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal7 : IDBusObject
        {
            void WatchSomethingAsync(Action a);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal8 : IDBusObject
        {
            int WatchSomethingAsync(Action a);
        }
        [DBusInterface("tmds.dbus.tests.invalidsignal")]
        interface IInvalidSignal9 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action<Exception> error);
        }
        interface IInvalidDBusObjectInterface1 : IEmptyDBusInterface, IEmptyDBusInterfaceDuplicate, IDBusObject
        {}
        [DBusInterface("tmds.dbus.tests.unsupportedmethod")]
        interface IUnsupportedDBusMethod1 : IDBusObject
        {
            Task FooAsync(int arg1, int arg2, CancellationToken cancellationToken);
        }
        [DBusInterface("tmds.dbus.tests.unsupportedsignal")]
        interface IUnsupportedSignal1 : IDBusObject
        {
            Task<IDisposable> WatchSomethingAsync(Action a, CancellationToken cancellationToken);
        }
    }
}
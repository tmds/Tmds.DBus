using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public async Task Method()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IStringOperations>("servicename", StringOperations.Path);
            await conn2.RegisterObjectAsync(new StringOperations());
            var reply = await proxy.ConcatAsync("hello ", "world");
            Assert.Equal("hello world", reply);
        }

        [Fact]
        public async Task Signal()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            var tcs = new TaskCompletionSource<string>();
            await proxy.WatchPongAsync(message => tcs.SetResult(message));
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("hello world");
            var reply = await tcs.Task;
            Assert.Equal("hello world", reply);
        }

        [Fact]
        public async Task SignalNoArg()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            var tcs = new TaskCompletionSource<string>();
            await proxy.WatchPongNoArgAsync(() => tcs.SetResult(null));
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("hello world");
            var reply = await tcs.Task;
            Assert.Equal(null, reply);
        }

        [Fact]
        public async Task SignalWithException()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            var tcs = new TaskCompletionSource<string>();
            await proxy.WatchPongWithExceptionAsync(message => tcs.SetResult(message), null);
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("hello world");
            var reply = await tcs.Task;
            Assert.Equal("hello world", reply);
        }

        [Fact]
        public async Task Properties()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPropertyObject>("servicename", PropertyObject.Path);
            var dictionary = new Dictionary<string, object>{{"key1", 1}, {"key2", 2}};
            await conn2.RegisterObjectAsync(new PropertyObject(dictionary));

            var properties = await proxy.GetAllAsync();
            Assert.Equal(dictionary, properties);

            var val1 = await proxy.GetAsync("key1");
            Assert.Equal(1, val1);

            var tcs = new TaskCompletionSource<PropertyChanges>();
            await proxy.WatchPropertiesAsync(_ => tcs.SetResult(_));
            await proxy.SetAsync("key1", "changed");

            var val1Changed = await proxy.GetAsync("key1");
            Assert.Equal("changed", val1Changed);

            var changes = await tcs.Task;
            Assert.Equal(1, changes.Changed.Length);
            Assert.Equal("key1", changes.Changed.First().Key);
            Assert.Equal("changed", changes.Changed.First().Value);
            Assert.Equal(0, changes.Invalidated.Length);
        }

        [InlineData("tcp:host=localhost,port=1")]
        [InlineData("unix:path=/does/not/exist")]
        [Theory]
        public async Task UnreachableAddress(string address)
        {
            using (var connection = new Connection(address))
            {
                await Assert.ThrowsAsync<ConnectException>(() => connection.ConnectAsync());
            }
        }

        [DBusInterface("tmds.dbus.tests.Throw")]
        public interface IThrow : IDBusObject
        {
            Task ThrowAsync();
        }

        public class Throw : IThrow
        {
            public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/throw");
            public static readonly string ExceptionMessage = "throwing";

            public ObjectPath ObjectPath => Path;

            public async Task ThrowAsync()
            {
                await Task.Yield();
                throw new Exception(ExceptionMessage);
            }
        }

        [Fact]
        public async Task PassException()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IThrow>("servicename", Throw.Path);
            await conn2.RegisterObjectAsync(new Throw());
            var exception = await Assert.ThrowsAsync<DBusException>(proxy.ThrowAsync);
            Assert.Equal(Throw.ExceptionMessage, exception.ErrorMessage);
        }

        [Fact]
        public async Task SignalWatcherPathNamespace()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            var tcs = new TaskCompletionSource<(ObjectPath @object, string value)>();
            await conn1.WatchSignalAsync<string>(new SignalMatchRule {
                Interface = "tmds.dbus.tests.PingPong",
                Member = "Pong",
                PathNamespace = PingPong.Path.Parent,
            }, _ => tcs.SetResult(_));
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("hello world");
            var reply = await tcs.Task;
            Assert.Equal(PingPong.Path, reply.@object);
            Assert.Equal("hello world", reply.value);
        }

        [Fact]
        public async Task SignalWatcherMatchArg()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            var tcs = new TaskCompletionSource<(ObjectPath @object, string value)>();
            await conn1.WatchSignalAsync<string>(new SignalMatchRule {
                Interface = "tmds.dbus.tests.PingPong",
                Member = "Pong",
                Path = PingPong.Path,
                Args = new[] { (0, "hello2") },
            }, _ => tcs.SetResult(_));
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("hello1"); // ignored
            await proxy.PingAsync("hello2"); // matched
            var reply = await tcs.Task;
            Assert.Equal(PingPong.Path, reply.@object);
            Assert.Equal("hello2", reply.value);
        }

        [Fact]
        public async Task SignalWatcherMatchArgPath()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            var tcs = new TaskCompletionSource<(ObjectPath @object, string value)>();
            await conn1.WatchSignalAsync<string>(new SignalMatchRule {
                Interface = "tmds.dbus.tests.PingPong",
                Member = "Pong",
                Path = PingPong.Path,
                ArgPaths = new[] { (0, "/aa/bb") },
            }, _ => tcs.SetResult(_));
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("/aa/aa/aa"); // ignored
            await proxy.PingAsync("/aa/bb/cc"); // matched
            var reply = await tcs.Task;
            Assert.Equal(PingPong.Path, reply.@object);
            Assert.Equal("/aa/bb/cc", reply.value);
        }

        [Fact]
        public async Task SignalWatcherMatchArg0Namespace()
        {
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;
            var proxy = conn1.CreateProxy<IPingPong>("servicename", PingPong.Path);
            // wrapping string in ValueTuple to exercise additional code paths
            var tcs = new TaskCompletionSource<(ObjectPath @object, ValueTuple<string> value)>();
            await conn1.WatchSignalAsync<ValueTuple<string>>(new SignalMatchRule {
                Interface = "tmds.dbus.tests.PingPong",
                Member = "Pong",
                Path = PingPong.Path,
                Arg0Namespace = "tmds.test",
            }, _ => tcs.SetResult(_));
            await conn2.RegisterObjectAsync(new PingPong());
            await proxy.PingAsync("tmds.other.name"); // ignored
            await proxy.PingAsync("tmds.test.name"); // matched
            var reply = await tcs.Task;
            Assert.Equal(PingPong.Path, reply.@object);
            Assert.Equal("tmds.test.name", reply.value.Item1);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;
using XunitSkip;

namespace Tmds.DBus.Tests
{
    public class DaemonTests
    {
        [Fact]
        public async Task Method()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                var conn1 = new Connection(address);
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();

                var conn2Name = conn2Info.LocalName;
                var path = StringOperations.Path;
                var proxy = conn1.CreateProxy<IStringOperations>(conn2Name, path);

                await conn2.RegisterObjectAsync(new StringOperations());

                var reply = await proxy.ConcatAsync("hello ", "world");
                Assert.Equal("hello world", reply);
            }
        }

        [Fact]
        public async Task Signal()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                var conn1 = new Connection(address);
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();
                await conn2.RegisterObjectAsync(new PingPong());

                var conn2Name = conn2Info.LocalName;
                var path = PingPong.Path;
                var proxy = conn1.CreateProxy<IPingPong>(conn2Name, path);
                var tcs = new TaskCompletionSource<string>();
                await proxy.WatchPongAsync(message => tcs.SetResult(message));
                await proxy.PingAsync("hello world");

                var reply = await tcs.Task;
                Assert.Equal("hello world", reply);

                conn1.Dispose();
                conn2.Dispose();
            }
        }

        [Fact]
        public async Task Properties()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                var conn1 = new Connection(address);
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();

                var dictionary = new Dictionary<string, object>{{"key1", 1}, {"key2", 2}};
                await conn2.RegisterObjectAsync(new PropertyObject(dictionary));

                var proxy = conn1.CreateProxy<IPropertyObject>(conn2Info.LocalName, PropertyObject.Path);

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
                Assert.Single(changes.Changed);
                Assert.Equal("key1", changes.Changed.First().Key);
                Assert.Equal("changed", changes.Changed.First().Value);
                Assert.Empty(changes.Invalidated);
            }
        }

        [Fact]
        public async Task BusOperations()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                IConnection conn1 = new Connection(address);
                await conn1.ConnectAsync();

                await conn1.ListActivatableServicesAsync();

                var exception = await Assert.ThrowsAsync<DBusException>(() => conn1.ActivateServiceAsync("com.some.service"));
                Assert.Equal("org.freedesktop.DBus.Error.ServiceUnknown", exception.ErrorName);

                var isRunning = await conn1.IsServiceActiveAsync("com.some.service");
                Assert.False(isRunning);
            }
        }

        [DBusInterface("tmds.dbus.tests.slow")]
        public interface ISlow : IDBusObject
        {
            Task SlowAsync();
            Task<IDisposable> WatchSomethingErrorAsync(Action value, Action<Exception> onError);
        }

        public class Slow : ISlow
        {
            public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/propertyobject");

            public ObjectPath ObjectPath
            {
                get
                {
                    return Path;
                }
            }

            public Task SlowAsync()
            {
                return Task.Delay(30000);
            }

            public Task<IDisposable> WatchSomethingErrorAsync(Action value, Action<Exception> onError)
            {
                return Task.FromResult<IDisposable>(new NoopDisposable());
            }

            private class NoopDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }

        [Fact]
        public async Task ClientDisconnect()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                IConnection conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();
                await conn2.RegisterObjectAsync(new Slow());

                // connection
                IConnection conn1 = new Connection(address);
                await conn1.ConnectAsync();
                // resolve
                var resolverTcs = new TaskCompletionSource<object>();
                await conn1.ResolveServiceOwnerAsync("some.service", _ => {}, e => resolverTcs.SetException(e));

                var proxy = conn1.CreateProxy<ISlow>(conn2Info.LocalName, Slow.Path);
                // method
                var pendingMethod = proxy.SlowAsync();
                // signal
                var signalTcs = new TaskCompletionSource<object>();
                await proxy.WatchSomethingErrorAsync(() => { }, e => signalTcs.SetException(e));

                conn1.Dispose();

                // method
                await Assert.ThrowsAsync<ObjectDisposedException>(() => pendingMethod);
                // signal
                await Assert.ThrowsAsync<ObjectDisposedException>(() => signalTcs.Task);
                // resolve
                await Assert.ThrowsAsync<ObjectDisposedException>(() => resolverTcs.Task);
            }
        }

        [Fact]
        public async Task DaemonDisconnect()
        {
            var dbusDaemon = new DBusDaemon();
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                IConnection conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();
                await conn2.RegisterObjectAsync(new Slow());

                IConnection conn1 = new Connection(address);
                await conn1.ConnectAsync();

                var proxy = conn1.CreateProxy<ISlow>(conn2Info.LocalName, Slow.Path);

                var pending = proxy.SlowAsync();

                dbusDaemon.Dispose();

                await Assert.ThrowsAsync<DisconnectedException>(() => pending);
            }
        }

        [DBusInterface("tmds.dbus.tests.FdOperations")]
        public interface IFdOperations : IDBusObject
        {
            Task<SafeFileHandle> PassAsync(SafeFileHandle fd2);
        }

        public class FdOperations : IFdOperations
        {
            public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/fdoperations");

            public ObjectPath ObjectPath => Path;

            public Task<SafeFileHandle> PassAsync(SafeFileHandle fd)
            {
                if (!fd.IsInvalid)
                {
                    return Task.FromResult(fd);
                }
                else
                {
                    var fileName = System.IO.Path.GetTempFileName();
                    var expected = "invalid";
                    File.WriteAllText(fileName, expected);
                    var fileStream = File.OpenRead(fileName);
                    var handle = fileStream.SafeFileHandle;
                    File.Delete(fileName);
                    return Task.FromResult(handle);
                }
            }
        }

        [Fact]
        public async Task UnixFd()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(DBusDaemonProtocol.Unix);
                var address = dbusDaemon.Address;

                var conn1 = new Connection(address);
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();

                var conn2Name = conn2Info.LocalName;
                var path = FdOperations.Path;
                var proxy = conn1.CreateProxy<IFdOperations>(conn2Name, path);

                await conn2.RegisterObjectAsync(new FdOperations());

                var fileName = Path.GetTempFileName();
                var expected = "content";
                File.WriteAllText(fileName, expected);
                try
                {
                    SafeFileHandle receivedHandle;
                    using (var fileStream = File.OpenRead(fileName))
                    {
                        var handle = fileStream.SafeFileHandle;
                        Assert.False(handle.IsClosed);
                        receivedHandle = await proxy.PassAsync(handle);
                        Assert.True(handle.IsClosed);
                    }
                    using (var reader = new StreamReader(new FileStream(receivedHandle, FileAccess.Read)))
                    {
                        var text = reader.ReadToEnd();
                        Assert.Equal(expected, text);
                    }
                }
                finally
                {
                    File.Delete(fileName);
                }
            }
        }

        [SkippableFact]
        public async Task UnixFd_Unsupported()
        {
            if (DBusDaemon.IsSELinux)
            {
                throw new SkipTestException("Cannot provide SELinux context to DBus daemon over TCP");
            }
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(DBusDaemonProtocol.Tcp);
                var address = dbusDaemon.Address;

                var conn1 = new Connection(address);
                var conn1Info = await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                var conn2Info = await conn2.ConnectAsync();

                var conn2Name = conn2Info.LocalName;
                var path = FdOperations.Path;
                var proxy = conn1.CreateProxy<IFdOperations>(conn2Name, path);

                await conn2.RegisterObjectAsync(new FdOperations());

                var fileName = Path.GetTempFileName();
                var expected = "content";
                File.WriteAllText(fileName, expected);
                try
                {
                    using (var fileStream = File.OpenRead(fileName))
                    {
                        var handle = fileStream.SafeFileHandle;
                        Assert.False(handle.IsClosed);
                        SafeFileHandle receivedHandle = await proxy.PassAsync(handle);
                        Assert.True(handle.IsClosed);
                        Assert.True(receivedHandle.IsInvalid);
                    }
                }
                finally
                {
                    File.Delete(fileName);
                }
            }
        }

        [Fact]
        public async Task AutoConnect()
        {
            string socketPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string address = $"unix:path={socketPath}";

            var connection = new Connection(new ClientConnectionOptions(address) { AutoConnect = true });

            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(DBusDaemonProtocol.Unix, socketPath);

                var reply = await connection.ListServicesAsync();
            }

            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(DBusDaemonProtocol.Unix, socketPath);

                var reply = await connection.ListServicesAsync();
            }

            var exception = await Assert.ThrowsAsync<ConnectException>(() => connection.ListServicesAsync());
        }

        [Fact]
        public async Task StateChanged()
        {
            string socketPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string address = $"unix:path={socketPath}";

            var connection = new Connection(new ClientConnectionOptions(address) { AutoConnect = true });
            var changeEvents = new BlockingCollection<ConnectionStateChangedEventArgs>(new ConcurrentQueue<ConnectionStateChangedEventArgs>());
            connection.StateChanged += (o, change) => changeEvents.Add(change);
            ConnectionStateChangedEventArgs e;

            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(DBusDaemonProtocol.Unix, socketPath);

                var reply = await connection.ListServicesAsync();

                // Created -> Connecting
                e = await changeEvents.TakeAsync();
                Assert.Equal(ConnectionState.Connecting, e.State);

                // Connecting -> Connected
                e = await changeEvents.TakeAsync();
                Assert.Equal(ConnectionState.Connected, e.State);
                Assert.True(e.ConnectionInfo.RemoteIsBus);
                Assert.NotNull(e.ConnectionInfo.LocalName);
            }

            // Connected -> Disconnecting
            e = await changeEvents.TakeAsync();
            Assert.Equal(ConnectionState.Disconnecting, e.State);
            Assert.NotNull(e.DisconnectReason);
            Assert.IsType<DisconnectedException>(e.DisconnectReason);

            // Disconnecting -> Disconnected
            e = await changeEvents.TakeAsync();
            Assert.Equal(ConnectionState.Disconnected, e.State);
            Assert.NotNull(e.DisconnectReason);
            Assert.IsType<DisconnectedException>(e.DisconnectReason);

            var exception = await Assert.ThrowsAsync<ConnectException>(() => connection.ListServicesAsync());

            // Disconnected -> Connecting
            e = await changeEvents.TakeAsync();
            Assert.Equal(ConnectionState.Connecting, e.State);
            Assert.NotNull(e.DisconnectReason);
            Assert.IsType<DisconnectedException>(e.DisconnectReason);

            // Connecting -> Disconnecting
            e = await changeEvents.TakeAsync();
            Assert.Equal(ConnectionState.Disconnecting, e.State);
            Assert.NotNull(e.DisconnectReason);
            Assert.IsType<ConnectException>(e.DisconnectReason);

            // Disconnecting -> Disconnected
            e = await changeEvents.TakeAsync();
            Assert.Equal(ConnectionState.Disconnected, e.State);
            Assert.NotNull(e.DisconnectReason);
            Assert.IsType<ConnectException>(e.DisconnectReason);

            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(DBusDaemonProtocol.Unix, socketPath);

                var reply = await connection.ListServicesAsync();

                // Created -> Connecting
                e = await changeEvents.TakeAsync();
                Assert.Equal(ConnectionState.Connecting, e.State);
                Assert.NotNull(e.DisconnectReason);
                Assert.IsType<ConnectException>(e.DisconnectReason);

                // Connecting -> Connected
                e = await changeEvents.TakeAsync();
                Assert.Equal(ConnectionState.Connected, e.State);
                Assert.True(e.ConnectionInfo.RemoteIsBus);
                Assert.NotNull(e.ConnectionInfo.LocalName);
                Assert.Null(e.DisconnectReason);
            }
        }

        private class MyConnectionOptions : ClientConnectionOptions
        {
            public Func<Task<ClientSetupResult>> ConnectFunction { get; set; }
            public Action<object> DisposeAction { get; set; }

            protected internal override Task<ClientSetupResult> SetupAsync()
                => ConnectFunction();

            protected internal override void Teardown(object token)
                => DisposeAction(token);
        }

        [Fact]
        public async Task ConnectionFunction()
        {
            var tokenTcs = new TaskCompletionSource<object>();
            var token = new object();
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                var conn1 = new Connection(new MyConnectionOptions {
                    ConnectFunction = () => Task.FromResult(
                        new ClientSetupResult
                        {
                            ConnectionAddress = address,
                            TeardownToken = token
                        }),
                    DisposeAction = tokenTcs.SetResult});

                 await conn1.ConnectAsync();
            }

            var disposeToken = await tokenTcs.Task;
            Assert.Equal(token, disposeToken);
        }
    }
}
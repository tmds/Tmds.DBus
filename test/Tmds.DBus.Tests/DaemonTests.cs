using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

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
                await conn2.ConnectAsync();

                var conn2Name = conn2.LocalName;
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
                await conn2.ConnectAsync();
                await conn2.RegisterObjectAsync(new PingPong());

                var conn2Name = conn2.LocalName;
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
                await conn2.ConnectAsync();

                var dictionary = new Dictionary<string, object>{{"key1", 1}, {"key2", 2}};
                await conn2.RegisterObjectAsync(new PropertyObject(dictionary));

                var proxy = conn1.CreateProxy<IPropertyObject>(conn2.LocalName, PropertyObject.Path);

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
                Assert.Equal(false, isRunning);
            }
        }

        [DBusInterface("tmds.dbus.tests.slow")]
        public interface ISlow : IDBusObject
        {
            Task SlowAsync();
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
        }

        [Fact]
        public async Task ClientDisconnect()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;

                IConnection conn2 = new Connection(address);
                await conn2.ConnectAsync();
                await conn2.RegisterObjectAsync(new Slow());

                IConnection conn1 = new Connection(address);
                var tcs = new TaskCompletionSource<Exception>();
                await conn1.ConnectAsync(e => tcs.SetResult(e));

                var proxy = conn1.CreateProxy<ISlow>(conn2.LocalName, Slow.Path);

                var pending = proxy.SlowAsync();

                conn1.Dispose();

                var disconnectReason = await tcs.Task;
                Assert.Null(disconnectReason);

                await Assert.ThrowsAsync<ObjectDisposedException>(() => pending);
            }
        }

        // https://github.com/dotnet/corefx/issues/21865
        // [Fact]
        // public async Task DaemonDisconnect()
        // {
        //     var dbusDaemon = new DBusDaemon();
        //     {
        //         await dbusDaemon.StartAsync();
        //         var address = dbusDaemon.Address;

        //         IConnection conn2 = new Connection(address);
        //         await conn2.ConnectAsync();
        //         await conn2.RegisterObjectAsync(new Slow());

        //         IConnection conn1 = new Connection(address);
        //         var tcs = new TaskCompletionSource<Exception>();
        //         await conn1.ConnectAsync(e => tcs.SetResult(e));

        //         var proxy = conn1.CreateProxy<ISlow>(conn2.LocalName, Slow.Path);

        //         var pending = proxy.SlowAsync();

        //         dbusDaemon.Dispose();

        //         await Assert.ThrowsAsync<DisconnectedException>(() => pending);
        //         var disconnectReason = await tcs.Task;
        //         Assert.NotNull(disconnectReason);
        //     }
        // }

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
                return Task.FromResult(fd);
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
                await conn2.ConnectAsync();

                var conn2Name = conn2.LocalName;
                var path = FdOperations.Path;
                var proxy = conn1.CreateProxy<IFdOperations>(conn2Name, path);

                await conn2.RegisterObjectAsync(new FdOperations());

                var fileName = Path.GetTempFileName();
                var expected = "content";
                File.WriteAllText(fileName, expected);

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
        }
    }
}
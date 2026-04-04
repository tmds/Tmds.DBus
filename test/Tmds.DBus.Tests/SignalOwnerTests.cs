extern alias Protocol;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class SignalOwnerTests
    {
        [DBusInterface("com.example.TestInterface")]
        public interface ITestSignals : IDBusObject
        {
            Task<IDisposable> WatchTestSignalAsync(Action<string> handler, Action<Exception> onError = null);
        }

        [Fact]
        public async Task AddMatchFollowsNameOwner()
        {
            using var dbusDaemon = new DBusDaemon();
            await dbusDaemon.StartAsync();

            string wellKnownName = "tmds.Service";

            // Service provider
            using var serviceConnection = new Protocol::Tmds.DBus.Protocol.DBusConnection(dbusDaemon.Address!);
            await serviceConnection.ConnectAsync();
            await serviceConnection.RequestNameAsync(wellKnownName, Protocol::Tmds.DBus.Protocol.RequestNameOptions.AllowReplacement);

            // Client connection
            var clientConnection = new Connection(dbusDaemon.Address!);
            var clientInfo = await clientConnection.ConnectAsync();

            // Subscribe to signals
            var receivedSignals = new List<string>();
            var signalLock = new object();
            var signalSemaphore = new SemaphoreSlim(0);
            var proxy = clientConnection.CreateProxy<ITestSignals>(wellKnownName, "/com/example/Object");
            await proxy.WatchTestSignalAsync(msg =>
            {
                lock (signalLock)
                {
                    receivedSignals.Add(msg);
                }
                signalSemaphore.Release();
            });

            // Owner sends a signal
            SendSignal(serviceConnection, "signal1");

            await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Single(receivedSignals);

            // Another connection spoofs a NameOwnerChanged signal
            using var evilConnection = new Protocol::Tmds.DBus.Protocol.DBusConnection(dbusDaemon.Address!);
            await evilConnection.ConnectAsync();
            using (var writer = evilConnection.GetMessageWriter())
            {
                writer.WriteSignalHeader(
                    path: "/org/freedesktop/DBus",
                    @interface: "org.freedesktop.DBus",
                    member: "NameOwnerChanged",
                    destination: clientInfo.LocalName,
                    signature: "sss");
                writer.WriteString(wellKnownName);
                writer.WriteString(serviceConnection.UniqueName!);
                writer.WriteString(evilConnection.UniqueName!);
                var buffer = writer.CreateMessage();
                evilConnection.TrySendMessage(buffer);
            }

            // Fake owner sends a signal.
            // It will be ignored because the above NameOwnerChanged did not come from org.freedesktop.DBus
            SendSignal(evilConnection, "signal-evil");

            // Original owner's signals should still match (mapping unchanged)
            SendSignal(serviceConnection, "signal2");
            await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(2, receivedSignals.Count);

            // Second service takes over the name legitimately
            using var service2 = new Protocol::Tmds.DBus.Protocol.DBusConnection(dbusDaemon.Address!);
            await service2.ConnectAsync();
            await service2.RequestNameAsync(wellKnownName, Protocol::Tmds.DBus.Protocol.RequestNameOptions.ReplaceExisting);

            // First service's signals should no longer match
            SendSignal(serviceConnection, "signal-old-owner");

            // New owner signal should match
            SendSignal(service2, "signal3");
            await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(3, receivedSignals.Count);

            // Release the name
            await service2.ReleaseNameAsync(wellKnownName);

            // Signals after release should not match
            SendSignal(service2, "signal-after-release");

            // Make a method call to be sure all signals that came before it are processed.
            await clientConnection.ResolveServiceOwnerAsync("com.example.FakeName");

            // Verify the complete list of received signals
            Assert.Equal(new[] { "signal1", "signal2", "signal3" }, receivedSignals);

            static void SendSignal(Protocol::Tmds.DBus.Protocol.DBusConnection conn, string message)
            {
                using var writer = conn.GetMessageWriter();
                writer.WriteSignalHeader(
                    path: "/com/example/Object",
                    @interface: "com.example.TestInterface",
                    member: "TestSignal",
                    signature: "s");
                writer.WriteString(message);
                var buffer = writer.CreateMessage();
                conn.TrySendMessage(buffer);
            }
        }

        [Fact]
        public async Task AddMatchWhenNoOwner()
        {
            using var dbusDaemon = new DBusDaemon();
            await dbusDaemon.StartAsync();

            string wellKnownName = "tmds.Service";

            var clientConnection = new Connection(dbusDaemon.Address!);
            await clientConnection.ConnectAsync();

            // Subscribe to signals before name is owned
            var receivedSignals = new List<string>();
            var signalLock = new object();
            var semaphore = new SemaphoreSlim(0);
            var proxy = clientConnection.CreateProxy<ITestSignals>(wellKnownName, "/com/example/Object");
            await proxy.WatchTestSignalAsync(msg =>
            {
                lock (signalLock)
                {
                    receivedSignals.Add(msg);
                }
                semaphore.Release();
            });

            // Create service and send signal before acquiring name (should not match)
            using var serviceConnection = new Protocol::Tmds.DBus.Protocol.DBusConnection(dbusDaemon.Address!);
            await serviceConnection.ConnectAsync();
            SendSignal(serviceConnection, "signal-before-acquire");

            // Now acquire the name
            await serviceConnection.RequestNameAsync(wellKnownName);

            // Send another signal - should now match
            SendSignal(serviceConnection, "signal-after-acquire");
            await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(new[] { "signal-after-acquire" }, receivedSignals);

            static void SendSignal(Protocol::Tmds.DBus.Protocol.DBusConnection conn, string message)
            {
                using var writer = conn.GetMessageWriter();
                writer.WriteSignalHeader(
                    path: "/com/example/Object",
                    @interface: "com.example.TestInterface",
                    member: "TestSignal",
                    signature: "s");
                writer.WriteString(message);
                var buffer = writer.CreateMessage();
                conn.TrySendMessage(buffer);
            }
        }

        [Fact]
        public async Task MultipleObserversForSameSender()
        {
            using var dbusDaemon = new DBusDaemon();
            await dbusDaemon.StartAsync();

            string wellKnownName = "tmds.Service";
            using var serviceConnection = new Protocol::Tmds.DBus.Protocol.DBusConnection(dbusDaemon.Address!);
            await serviceConnection.ConnectAsync();
            await serviceConnection.RequestNameAsync(wellKnownName);

            var clientConnection = new Connection(dbusDaemon.Address!);
            await clientConnection.ConnectAsync();

            // Add first observer
            var signals1 = 0;
            var semaphore1 = new SemaphoreSlim(0);
            var proxy1 = clientConnection.CreateProxy<ITestSignals>(wellKnownName, "/com/example/Object");
            var observer1 = await proxy1.WatchTestSignalAsync(msg =>
            {
                Interlocked.Increment(ref signals1);
                semaphore1.Release();
            });

            // Add second observer for same service
            var signals2 = 0;
            var semaphore2 = new SemaphoreSlim(0);
            var proxy2 = clientConnection.CreateProxy<ITestSignals>(wellKnownName, "/com/example/Object");
            var observer2 = await proxy2.WatchTestSignalAsync(msg =>
            {
                Interlocked.Increment(ref signals2);
                semaphore2.Release();
            });

            // Send signal
            SendSignal(serviceConnection);
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(5));
            await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, signals1);
            Assert.Equal(1, signals2);

            // Remove first observer - name owner watch should still exist
            observer1.Dispose();

            SendSignal(serviceConnection);
            await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, signals1); // Unchanged (observer disposed)
            Assert.Equal(2, signals2); // Incremented

            // Remove second observer - name owner watch should be cleaned up
            observer2.Dispose();

            // Send another signal after all observers are disposed
            SendSignal(serviceConnection);

            // Make a method call to be sure all signals that came before it are processed.
            await clientConnection.ResolveServiceOwnerAsync("com.example.FakeName");

            // Verify no observers received the signal since we have unsubscribed.
            Assert.Equal(1, signals1); // Unchanged
            Assert.Equal(2, signals2); // Unchanged

            static void SendSignal(Protocol::Tmds.DBus.Protocol.DBusConnection conn)
            {
                using var writer = conn.GetMessageWriter();
                writer.WriteSignalHeader(
                    path: "/com/example/Object",
                    @interface: "com.example.TestInterface",
                    member: "TestSignal",
                    signature: "s");
                writer.WriteString("signal");
                var buffer = writer.CreateMessage();
                conn.TrySendMessage(buffer);
            }
        }

        [Fact]
        public async Task AddMatchWithSenderUniqueName()
        {
            using var dbusDaemon = new DBusDaemon();
            await dbusDaemon.StartAsync();

            var clientConnection = new Connection(dbusDaemon.Address!);
            await clientConnection.ConnectAsync();

            using var serviceConnection = new Protocol::Tmds.DBus.Protocol.DBusConnection(dbusDaemon.Address!);
            await serviceConnection.ConnectAsync();
            var serviceUniqueName = serviceConnection.UniqueName!;

            // Subscribe to signals from a fake unique name (should not match)
            var nonMatchingSignalsReceived = 0;
            var fakeProxy = clientConnection.CreateProxy<ITestSignals>(":fake-id", "/com/example/Object");
            await fakeProxy.WatchTestSignalAsync(msg => Interlocked.Increment(ref nonMatchingSignalsReceived));

            // Subscribe to signals from the actual sender's unique name
            var matchingSignalsReceived = 0;
            var semaphore = new SemaphoreSlim(0);
            var realProxy = clientConnection.CreateProxy<ITestSignals>(serviceUniqueName, "/com/example/Object");
            await realProxy.WatchTestSignalAsync(msg =>
            {
                Interlocked.Increment(ref matchingSignalsReceived);
                semaphore.Release();
            });

            // Send signal
            using (var writer = serviceConnection.GetMessageWriter())
            {
                writer.WriteSignalHeader(
                    path: "/com/example/Object",
                    @interface: "com.example.TestInterface",
                    member: "TestSignal",
                    signature: "s");
                writer.WriteString("signal");
                var buffer = writer.CreateMessage();
                serviceConnection.TrySendMessage(buffer);
            }

            // Only observer matching the unique id will receive the signal
            await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, matchingSignalsReceived);
            Assert.Equal(0, nonMatchingSignalsReceived);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AddMatchWithoutBusIgnoresSender(bool useUniqueId)
        {
            // Create paired connections (peer-to-peer, no bus)
            var connections = await PairedConnection.CreateConnectedPairAsync();
            var conn1 = connections.Item1;
            var conn2 = connections.Item2;

            var signalReceived = new TaskCompletionSource<bool>();

            var proxy = conn1.CreateProxy<ITestSignals>(
                useUniqueId ? ":fake-id" : "com.example.WellKnownName",
                "/com/example/Object");

            await proxy.WatchTestSignalAsync(msg => signalReceived.TrySetResult(true));

            // Send signal from conn2
            await conn2.RegisterObjectAsync(new TestSignalEmitter());
            TestSignalEmitter.Instance.EmitSignal();

            // Verify the signal was received (sender is ignored when there's no bus)
            var received = await signalReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(received);
        }

        private class TestSignalEmitter : ITestSignals
        {
            public static readonly ObjectPath Path = new ObjectPath("/com/example/Object");
            public static TestSignalEmitter Instance { get; private set; }

            public TestSignalEmitter()
            {
                Instance = this;
            }

            public event Action<string> OnTestSignal;

            public ObjectPath ObjectPath => Path;

            public Task<IDisposable> WatchTestSignalAsync(Action<string> handler, Action<Exception> onError = null)
            {
                OnTestSignal += handler;
                return Task.FromResult<IDisposable>(new SignalDisposable(this, handler));
            }

            public void EmitSignal()
            {
                OnTestSignal?.Invoke("test");
            }

            private class SignalDisposable : IDisposable
            {
                private TestSignalEmitter _emitter;
                private Action<string> _handler;

                public SignalDisposable(TestSignalEmitter emitter, Action<string> handler)
                {
                    _emitter = emitter;
                    _handler = handler;
                }

                public void Dispose()
                {
                    if (_emitter != null)
                    {
                        _emitter.OnTestSignal -= _handler;
                        _emitter = null;
                        _handler = null;
                    }
                }
            }
        }
    }
}

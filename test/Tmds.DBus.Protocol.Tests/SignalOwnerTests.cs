using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class SignalOwnerTests
{
    [Fact]
    public async Task AddMatchFollowsNameOwner()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.Service";

        // Service provider
        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        // Client connection
        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        // Subscribe to a signal
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = wellKnownName,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        var receivedSignals = new List<string>();
        var signalLock = new object();
        var signalSemaphore = new SemaphoreSlim(0);
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    lock (signalLock)
                    {
                        receivedSignals.Add(msg);
                    }
                    signalSemaphore.Release();
                }
            },
            null, null, false, ObserverFlags.None);

        // Track NameOwnerChanged signals without sender filter to verify they're received (including the ignored)
        var nameOwnerChangedReceived = 0;
        var nameOwnerChangedSemaphore = new SemaphoreSlim(0);
        var nameOwnerChangedRule = new MatchRule
        {
            Type = MessageType.Signal,
            Interface = "org.freedesktop.DBus",
            Member = "NameOwnerChanged",
            Path = "/org/freedesktop/DBus"
        };
        await clientConnection.AddMatchAsync(nameOwnerChangedRule,
            (Message m, object? s) =>
            {
                var reader = m.GetBodyReader();
                return (reader.ReadString(), reader.ReadString(), reader.ReadString());
            },
            (Exception? ex, (string, string, string) data, object? rs, object? hs) =>
            {
                if (ex == null && data.Item1 == wellKnownName)
                {
                    Interlocked.Increment(ref nameOwnerChangedReceived);
                    nameOwnerChangedSemaphore.Release();
                }
            },
            null, null, false, ObserverFlags.NoSubscribe);

        // Owner sends a signal
        SendSignal(serviceConnection, "signal1");

        await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(receivedSignals);

        // Another connection spoofs a NameOwnerChanged signal.
        using var evilConnection = new DBusConnection(dbusDaemon.Address!);
        await evilConnection.ConnectAsync();
        using (var writer = evilConnection.GetMessageWriter())
        {
            writer.WriteSignalHeader(
                path: "/org/freedesktop/DBus",
                @interface: "org.freedesktop.DBus",
                member: "NameOwnerChanged",
                destination: clientConnection.UniqueName!, // Send it directly to the client.
                signature: "sss");
            writer.WriteString(wellKnownName);
            writer.WriteString(serviceConnection.UniqueName!);
            writer.WriteString(evilConnection.UniqueName!);
            var buffer = writer.CreateMessage();
            evilConnection.TrySendMessage(buffer);
        }

        // The client receives the NameOwnerChanged message because it was the destination for the message.
        await nameOwnerChangedSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, nameOwnerChangedReceived);

        // Fake owner sends a signal.
        // It will be ignored because the above NameOwnerChanged did not come from org.freedesktop.DBus
        SendSignal(evilConnection, "signal-evil");

        // Original owner's signals should still match (mapping unchanged)
        SendSignal(serviceConnection, "signal2");
        await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, receivedSignals.Count);

        // Second service takes over the name legitimately
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        // The client receives the NameOwnerChanged from the message bus.
        await nameOwnerChangedSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, nameOwnerChangedReceived);

        // First service's signals should no longer match
        SendSignal(serviceConnection, "signal-old-owner");

        // New owner signal should match.
        SendSignal(service2, "signal3");
        await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, receivedSignals.Count);

        // Release the name
        await service2.ReleaseNameAsync(wellKnownName);

        // Verify the NameOwnerChanged for release was received
        await nameOwnerChangedSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, nameOwnerChangedReceived);

        // Signals after release should not match
        SendSignal(service2, "signal-after-release");

        // Verify the complete list of received signals (release prevented signal5)
        Assert.Equal(new[] { "signal1", "signal2", "signal3" }, receivedSignals);

        static void SendSignal(DBusConnection conn, string message)
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
    public async Task AddMatchWithSenderUniqueName()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        var serviceUniqueName = serviceConnection.UniqueName!;

        // Subscribe to signals from a fake unique name
        var nonMatchingSignalsReceived = 0;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ":fake-id",
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    Interlocked.Increment(ref nonMatchingSignalsReceived);
                }
            },
            null, null, false, ObserverFlags.None);

         // Subscribe to signals from the actual sender's unique name
        var matchingSignalsReceived = 0;
        rule.Sender = serviceUniqueName;
        var semaphore = new SemaphoreSlim(0);
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    Interlocked.Increment(ref matchingSignalsReceived);
                    semaphore.Release();
                }
            },
            null, null, false, ObserverFlags.None);

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

        // Only observer matching the unique id will receive the signal.
        await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, matchingSignalsReceived);
        Assert.Equal(0, nonMatchingSignalsReceived);
    }

    [Fact]
    public async Task AddMatchForDBusDoesNotWatchOwner()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        using var client = new DBusConnection(dbusDaemon.Address!);
        await client.ConnectAsync();

        var signalsReceived = 0;
        var nameChangeSemaphore = new SemaphoreSlim(0);

        // Track MethodReturns so we know how many method calls we made.
        var methodReturns = 0;
        var methodReturnRule = new MatchRule
        {
            Type = MessageType.MethodReturn
        };
        await client.AddMatchAsync(methodReturnRule,
            (Message m, object? s) => 0,
            (Exception? ex, int _, object? rs, object? hs) =>
            {
                Interlocked.Increment(ref methodReturns);
            },
            null, null, false, ObserverFlags.NoSubscribe);

        // Subscribe to NameOwnerChanged from org.freedesktop.DBus
        int methodReturnsBeforeMatch = methodReturns;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = "org.freedesktop.DBus",  // Should not trigger name owner watching
            Interface = "org.freedesktop.DBus",
            Member = "NameOwnerChanged",
            Path = "/org/freedesktop/DBus"
        };
        var observer = await client.AddMatchAsync(rule,
            (Message m, object? s) =>
            {
                var reader = m.GetBodyReader();
                return (reader.ReadString(), reader.ReadString(), reader.ReadString());
            },
            (Exception? ex, (string, string, string) data, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    if (data.Item1 == "test.trigger.signal")
                    {
                        Interlocked.Increment(ref signalsReceived);
                        nameChangeSemaphore.Release();
                    }
                }
            },
            null, null, false, ObserverFlags.None);
        int methodReturnsAfterMatch = methodReturns;

        // Only 1 MethodReturn (AddMatch for the signal rule).
        Assert.Equal(methodReturnsBeforeMatch + 1, methodReturnsAfterMatch);

        // Request a name to trigger a NameOwnerChanged signal
        using var otherConn = new DBusConnection(dbusDaemon.Address!);
        await otherConn.ConnectAsync();
        await otherConn.RequestNameAsync("test.trigger.signal");
        await nameChangeSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, signalsReceived);

        // Dispose the observer
        int signalsBeforeDispose = signalsReceived;
        observer.Dispose();

        // Have another connection request a different name (should trigger NameOwnerChanged)
        using var anotherConn = new DBusConnection(dbusDaemon.Address!);
        await anotherConn.ConnectAsync();
        await anotherConn.RequestNameAsync("test.another.signal");

        // Make a method call to be sure all signals that came before it are processed.
        Assert.False(await client.ReleaseNameAsync("com.example.FakeName"));

        // Verify signalsReceived didn't increment (observer was disposed)
        Assert.Equal(signalsBeforeDispose, signalsReceived);
    }

    [Fact]
    public async Task AddMatchWhenNoOwner()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.Service";

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        // Subscribe to signals before name is owned
        var receivedSignals = new List<string>();
        var signalLock = new object();
        var semaphore = new SemaphoreSlim(0);
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = wellKnownName,  // Name has no owner yet
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    lock (signalLock)
                    {
                        receivedSignals.Add(msg);
                    }
                    semaphore.Release();
                }
            },
            null, null, false, ObserverFlags.None);

        // Create service and send signal before acquiring name (should not match)
        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        SendSignal(serviceConnection, "signal-before-acquire");

        // Now acquire the name
        await serviceConnection.RequestNameAsync(wellKnownName);

        // Send another signal. Signal should now match.
        SendSignal(serviceConnection, "signal-after-acquire");
        await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(new[] { "signal-after-acquire" }, receivedSignals);

        static void SendSignal(DBusConnection conn, string message = "signal")
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
        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        // Track MethodReturns so we know how many method calls we made.
        var methodReturns = 0;
        var methodReturnRule = new MatchRule
        {
            Type = MessageType.MethodReturn
        };
        await clientConnection.AddMatchAsync(methodReturnRule,
            (Message m, object? s) => 0,
            (Exception? ex, int _, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    Interlocked.Increment(ref methodReturns);
                }
            },
            null, null, false, ObserverFlags.NoSubscribe);

        // Add first observer
        int methodReturnsBeforeFirstObserver = methodReturns;
        var signals1 = 0;
        var semaphore1 = new SemaphoreSlim(0);
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = wellKnownName,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        var observer1 = await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    Interlocked.Increment(ref signals1);
                    semaphore1.Release();
                }
            },
            null, null, false, ObserverFlags.None);

        // Capture method returns count after first observer (GetNameOwner was called)
        int methodReturnsAfterFirstObserver = methodReturns;

        // There should be 3 method returns:
        // 1. AddMatch for the signal rule
        // 2. AddMatch for NameOwnerChanged (to track owner changes)
        // 3. GetNameOwner (to get the current unique name for the well-known name)
        Assert.Equal(methodReturnsBeforeFirstObserver + 3, methodReturnsAfterFirstObserver);

        // Add second observer for same rule
        var signals2 = 0;
        var semaphore2 = new SemaphoreSlim(0);
        var observer2 = await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    Interlocked.Increment(ref signals2);
                    semaphore2.Release();
                }
            },
            null, null, false, ObserverFlags.None);

        // Verify second observer only triggered 1 additional MethodReturn (AddMatch for the signal rule)
        // No AddMatch for NameOwnerChanged or GetNameOwner because observers share the name owner watch
        int methodReturnsAfterSecondObserver = methodReturns;
        Assert.Equal(methodReturnsAfterFirstObserver, methodReturnsAfterSecondObserver);

        // Add third observer with NoSubscribe
        var signals3 = 0;
        var observer3 = await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    Interlocked.Increment(ref signals3);
                }
            },
            null, null, false, ObserverFlags.NoSubscribe);

        // Verify third observer with NoSubscribe didn't trigger any additional MethodReturns.
        int methodReturnsAfterThirdObserver = methodReturns;
        Assert.Equal(methodReturnsAfterSecondObserver, methodReturnsAfterThirdObserver);

        // Send signal
        SendSignal(serviceConnection);
        await semaphore1.WaitAsync(TimeSpan.FromSeconds(5));
        await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, signals1);
        Assert.Equal(1, signals2);

        // Remove first observer - name owner watch should still exist
        observer1.Dispose();
        // Verify disposing the first observer only triggered 1 MethodReturn (RemoveMatch for the signal rule)
        // No RemoveMatch for NameOwnerChanged because observer2 still needs the name owner watch
        Assert.Equal(methodReturnsAfterSecondObserver, methodReturns);

        SendSignal(serviceConnection);
        await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, signals1); // Unchanged (observer disposed)
        Assert.Equal(2, signals2); // Incremented

        // Remove second observer - name owner watch should be cleaned up
        observer2.Dispose();
        // Note: we don't observe method returns for RemoveMatch since we make these requests with 'NoReply'.
        Assert.Equal(methodReturnsAfterSecondObserver, methodReturns);

        // Verify NoSubscribe observer received the same signals as regular observers
        Assert.Equal(signals2, signals3);

        // Ensure the daemon has processed the RemoveMatch messages before sending the next signal.
        // RemoveMatch is sent with NoReplyExpected, so we make a method call to synchronize.
        Assert.False(await clientConnection.ReleaseNameAsync("com.example.FakeName"));

        // Send another signal after all subscribing observers are disposed
        SendSignal(serviceConnection);
        // Make a method call to be sure all signals that came before it are processed.
        Assert.False(await clientConnection.ReleaseNameAsync("com.example.FakeName"));

        // Verify no observers received the signal since we have unsubscribed.
        Assert.Equal(1, signals1); // Unchanged (observer disposed)
        Assert.Equal(2, signals2); // Unchanged (observer disposed)
        Assert.Equal(2, signals3); // Unchanged (observer is NoSubscribe)

        static void SendSignal(DBusConnection conn, string message = "signal")
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
    public async Task EmitOnOwnerChanged_DisposesObserverWhenOwnerChanges()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.OwnerChangeService";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var notificationTcs = new TaskCompletionSource<Notification<string>>();
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = wellKnownName,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };

        await clientConnection.AddMatchAsync(rule,
            reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
            handler: ctx =>
            {
                if (ctx.IsCompletion)
                {
                    notificationTcs.TrySetResult(ctx);
                }
            },
            emitOnCapturedContext: false,
            flags: ObserverFlags.EmitOnOwnerChanged);

        // Another connection takes over the name, triggering NameOwnerChanged.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        var notification = await notificationTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(notification.IsCompletion);
        Assert.False(notification.HasValue);
        Assert.Equal(NotificationType.OwnerChanged, notification.Type);
    }

    [Fact]
    public async Task EmitOnOwnerChanged_NotTriggeredWithoutOwnerChange()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.StableService";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var signalSemaphore = new SemaphoreSlim(0);
        var ownerChangedReceived = false;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = wellKnownName,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };

        await clientConnection.AddMatchAsync(rule,
            reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
            handler: ctx =>
            {
                if (ctx.HasValue)
                {
                    signalSemaphore.Release();
                }
                if (ctx.IsCompletion)
                {
                    ownerChangedReceived = true;
                }
            },
            emitOnCapturedContext: false,
            flags: ObserverFlags.EmitOnOwnerChanged);

        // Send signal - owner hasn't changed so EmitOnOwnerChanged should not fire.
        SendSignal(serviceConnection);
        await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(ownerChangedReceived);

        static void SendSignal(DBusConnection conn)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddMatchWithoutBusIgnoresSender(bool useUniqueId)
    {
        var connections = PairedConnection.CreatePair();
        using var conn1 = connections.Item1;
        using var conn2 = connections.Item2;

        var signalReceived = new TaskCompletionSource<bool>();

        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = useUniqueId ? ":fake-id" : "com.example.WellKnownName",
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };

        await conn1.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex == null)
                {
                    signalReceived.TrySetResult(true);
                }
            },
            null, null, false, ObserverFlags.None);

        // Send signal from conn2
        using (var writer = conn2.GetMessageWriter())
        {
            writer.WriteSignalHeader(
                path: "/com/example/Object",
                @interface: "com.example.TestInterface",
                member: "TestSignal",
                signature: "s");
            writer.WriteString("test-signal");
            var buffer = writer.CreateMessage();
            conn2.TrySendMessage(buffer);
        }

        // Verify the signal was received (sender is ignored when there's no bus)
        var received = await signalReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(received);
    }
}

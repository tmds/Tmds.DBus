using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class NameOwnerWatcherTests
{
    [Fact]
    public async Task WatchNameOwner_GetCurrentOwner_ReturnsOwner()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        string? owner = watcher.GetCurrentOwner();
        Assert.NotNull(owner);
        Assert.Contains(serviceConnection.UniqueName!, owner);
    }

    [Fact]
    public async Task WatchNameOwner_WaitForOwner_ReturnsImmediatelyWhenOwned()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        string owner = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(owner);
        Assert.Contains(serviceConnection.UniqueName!, owner);
    }

    [Fact]
    public async Task WatchNameOwner_WaitForOwner_WaitsUntilNameAcquired()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        // Name not owned yet, GetCurrentOwner should return null.
        Assert.Null(watcher.GetCurrentOwner());

        // Start waiting before the name is owned.
        Task<string> waitTask = watcher.WaitForOwnerAsync();
        Assert.False(waitTask.IsCompleted);

        // Now acquire the name.
        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        string owner = await waitTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(owner);
        Assert.Contains(serviceConnection.UniqueName!, owner);
    }

    [Fact]
    public async Task WatchNameOwner_OwnerChanges_UpdatesCurrentOwner()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        string? owner1 = watcher.GetCurrentOwner();
        Assert.NotNull(owner1);
        Assert.Contains(service1.UniqueName!, owner1);

        // Get a token that fires when the owner changes.
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken(owner1);

        // Second service takes over.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        // Wait for the owner change to be processed.
        var tcs = new TaskCompletionSource();
        ct.Register(() => tcs.SetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Now the owner should be updated.
        string? owner2 = watcher.GetCurrentOwner();
        Assert.NotNull(owner2);
        Assert.Contains(service2.UniqueName!, owner2);
        Assert.NotEqual(owner1, owner2);
    }

    [Fact]
    public async Task WatchNameOwner_NameReleased_GetCurrentOwnerReturnsNull()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        Assert.NotNull(watcher.GetCurrentOwner());

        // Release the name.
        await serviceConnection.ReleaseNameAsync(wellKnownName);

        // Wait for the owner change to propagate. We need WaitForOwnerAsync to
        // wait for an owner again, but first we can poll or use a small delay
        // and a method call to flush.
        Assert.False(await clientConnection.ReleaseNameAsync("com.example.FakeName"));

        Assert.Null(watcher.GetCurrentOwner());
    }

    [Fact]
    public async Task WatchNameOwner_GetOwnerChangedCancellationToken_CancelsOnOwnerChange()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        string currentOwner = watcher.GetCurrentOwner()!;
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken(currentOwner);

        Assert.False(ct.IsCancellationRequested);

        // Change the owner.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        // Wait for the cancellation to fire.
        var tcs = new TaskCompletionSource();
        ct.Register(() => tcs.SetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(ct.IsCancellationRequested);
    }

    [Fact]
    public async Task WatchNameOwner_GetOwnerChangedCancellationToken_AlreadyChangedReturnsAlreadyCancelled()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        // Get a fake owner that doesn't match current.
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken("fake-owner");

        Assert.True(ct.IsCancellationRequested);
    }

    [Fact]
    public async Task WatchNameOwner_Dispose_ThrowsOnSubsequentCalls()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        watcher.Dispose();

        Assert.Throws<ObjectDisposedException>(() => watcher.GetCurrentOwner());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => watcher.WaitForOwnerAsync());
        Assert.Throws<ObjectDisposedException>(() => watcher.GetOwnerChangedCancellationToken("owner"));
    }

    [Fact]
    public async Task WatchNameOwner_DoubleDispose_DoesNotThrow()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        watcher.Dispose();
        watcher.Dispose(); // Should not throw.
    }

    [Fact]
    public async Task WatchNameOwner_ConnectionDisposed_ThrowsDisconnectedException()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        // Start waiting before disconnecting.
        Task<string> waitTask = watcher.WaitForOwnerAsync();

        clientConnection.Dispose();

        await Assert.ThrowsAsync<DisconnectedException>(() => waitTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task WatchNameOwner_WaitForOwner_CancellationWorks()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => watcher.WaitForOwnerAsync(cts.Token));
    }

    [Fact]
    public async Task AddMatch_SenderWithOwnerFormat_FiltersSignalsByOwner()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.OwnerFormatTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        // Watch the name to get the owner string in "uniqueId (serviceName)" format.
        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Subscribe using the owner format string as sender.
        var signalSemaphore = new SemaphoreSlim(0);
        int signalCount = 0;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ownerStr,
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
                    Interlocked.Increment(ref signalCount);
                    signalSemaphore.Release();
                }
            },
            null, null, false, ObserverFlags.None);

        // Signal from the original owner should match.
        SendSignal(service1, "signal1");
        await signalSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, signalCount);

        // Change the owner.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        // Flush to ensure NameOwnerChanged is processed.
        Assert.False(await clientConnection.ReleaseNameAsync("com.example.FakeName"));

        // Signal from the new owner should NOT match (observer was bound to original owner).
        SendSignal(service2, "signal-new-owner");

        // Flush again.
        Assert.False(await clientConnection.ReleaseNameAsync("com.example.FakeName2"));

        Assert.Equal(1, signalCount);

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
    public async Task SendMessage_WithDestinationOwner_SucceedsWhenOwnerMatches()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.DestOwnerTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);
        serviceConnection.AddMethodHandler(new EchoHandler());

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        // Watch the name to get the owner string.
        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Send a method call using the owner string as destination.
        string reply = await clientConnection.CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadString(), null)
            .WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("pong", reply);

        MessageBuffer CreateMessage()
        {
            using var writer = clientConnection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: ownerStr,
                path: "/echo",
                @interface: "com.example.Echo",
                member: "Ping",
                signature: "s");
            writer.WriteString("ping");
            return writer.CreateMessage();
        }
    }

    [Fact]
    public async Task SendMessage_WithDestinationOwner_ThrowsWhenOwnerChanged()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.DestOwnerTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        // Watch the name to get the owner string.
        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Get a token that fires when the owner changes.
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken(ownerStr);

        // Change the owner.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        // Wait for the owner change to be processed.
        var ownerChangedTcs = new TaskCompletionSource();
        ct.Register(() => ownerChangedTcs.SetResult());
        await ownerChangedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Sending a message with the old owner as destination should throw.
        await Assert.ThrowsAsync<DBusOwnerChangedException>(() =>
            clientConnection.CallMethodAsync(CreateMessage()));

        MessageBuffer CreateMessage()
        {
            using var writer = clientConnection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: ownerStr,
                path: "/com/example/Object",
                @interface: "com.example.TestInterface",
                member: "TestMethod");
            return writer.CreateMessage();
        }
    }

    [Fact]
    public async Task SendMessage_WithDestinationOwner_WatcherDisposed_ThrowsOwnerChanged()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Dispose the watcher, removing the owner from the tracked set.
        watcher.Dispose();

        // Sending a message with the owner identifier as destination should throw.
        await Assert.ThrowsAsync<DBusOwnerChangedException>(() =>
            clientConnection.CallMethodAsync(CreateMessage()));

        MessageBuffer CreateMessage()
        {
            using var writer = clientConnection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: ownerStr,
                path: "/com/example/Object",
                @interface: "com.example.TestInterface",
                member: "TestMethod");
            return writer.CreateMessage();
        }
    }

    [Fact]
    public async Task AddMatch_EmitOnOwnerChanged_EmitsWhenOwnerChanges()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Subscribe with EmitOnOwnerChanged.
        var emitSemaphore = new SemaphoreSlim(0);
        Exception? emittedException = null;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ownerStr,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                if (ex != null)
                {
                    emittedException = ex;
                    emitSemaphore.Release();
                }
            },
            null, null, false, ObserverFlags.EmitOnOwnerChanged);

        // Change the owner.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        await emitSemaphore.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(emittedException);
        Assert.True(ObserverHandler.IsOwnerChanged(emittedException));
    }

    [Fact]
    public async Task AddMatch_StaleOwnerIdentifier_EmitsImmediately()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Change the owner so ownerStr becomes stale.
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken(ownerStr);
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        var tcs = new TaskCompletionSource();
        ct.Register(() => tcs.SetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Subscribe with the stale owner identifier.
        Exception? emittedException = null;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ownerStr,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                emittedException = ex;
            },
            null, null, false, ObserverFlags.EmitOnOwnerChanged);

        Assert.NotNull(emittedException);
        Assert.True(ObserverHandler.IsOwnerChanged(emittedException));
    }

    [Fact]
    public async Task AddMatch_StaleOwnerIdentifier_WithoutEmitFlag_DisposedSilently()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Change the owner so ownerStr becomes stale.
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken(ownerStr);
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        var tcs = new TaskCompletionSource();
        ct.Register(() => tcs.SetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Subscribe with the stale owner identifier without EmitOnOwnerChanged.
        bool handlerCalled = false;
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ownerStr,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) =>
            {
                handlerCalled = true;
            },
            null, null, false, ObserverFlags.None);

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task WatchNameOwner_MultipleWatchers_IndependentLifecycles()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName, RequestNameOptions.AllowReplacement);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var watcher1 = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        var watcher2 = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        string? owner1 = watcher1.GetCurrentOwner();
        string? owner2 = watcher2.GetCurrentOwner();
        Assert.NotNull(owner1);
        Assert.Equal(owner1, owner2);

        // Disposing watcher1 should not affect watcher2.
        watcher1.Dispose();

        Assert.Throws<ObjectDisposedException>(() => watcher1.GetCurrentOwner());
        Assert.Equal(owner1, watcher2.GetCurrentOwner());

        // watcher2 should still track owner changes.
        CancellationToken ct = watcher2.GetOwnerChangedCancellationToken(owner1);

        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName, RequestNameOptions.ReplaceExisting);

        var tcs = new TaskCompletionSource();
        ct.Register(() => tcs.SetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        string? newOwner = watcher2.GetCurrentOwner();
        Assert.NotNull(newOwner);
        Assert.NotEqual(owner1, newOwner);
        Assert.Contains(service2.UniqueName!, newOwner);

        watcher2.Dispose();
    }

    [Fact]
    public async Task WatchNameOwner_WaitForOwner_NameReleasedAndReacquired()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);

        // Name not owned yet, start waiting.
        Task<string> waitTask = watcher.WaitForOwnerAsync();
        Assert.False(waitTask.IsCompleted);

        // Acquire the name.
        var service1 = new DBusConnection(dbusDaemon.Address!);
        await service1.ConnectAsync();
        await service1.RequestNameAsync(wellKnownName);

        string owner1 = await waitTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(owner1);

        // Release the name by disposing the service connection.
        CancellationToken ct = watcher.GetOwnerChangedCancellationToken(owner1);
        service1.Dispose();

        var tcs = new TaskCompletionSource();
        ct.Register(() => tcs.SetResult());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Name is now unowned.
        Assert.Null(watcher.GetCurrentOwner());

        // Wait for a new owner.
        Task<string> waitTask2 = watcher.WaitForOwnerAsync();
        Assert.False(waitTask2.IsCompleted);

        // Re-acquire with a different service.
        using var service2 = new DBusConnection(dbusDaemon.Address!);
        await service2.ConnectAsync();
        await service2.RequestNameAsync(wellKnownName);

        string owner2 = await waitTask2.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(owner2);
        Assert.NotEqual(owner1, owner2);
        Assert.Contains(service2.UniqueName!, owner2);
    }

    [Fact]
    public async Task AddMatch_OwnerIdentifier_WatcherDisposed_ThrowsInvalidOperation()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Dispose the watcher, removing the ObservedName.
        watcher.Dispose();

        // Subscribing with the owner identifier should throw.
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ownerStr,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            clientConnection.AddMatchAsync(rule,
                (Message m, object? s) => m.GetBodyReader().ReadString(),
                (Exception? ex, string msg, object? rs, object? hs) => { },
                null, null, false, ObserverFlags.None).AsTask());
    }

    [Fact]
    public async Task AddMatch_DisposeObserver_CleansUpNameOwnerSubscription()
    {
        using var dbusDaemon = new DBusDaemon();
        await dbusDaemon.StartAsync();

        string wellKnownName = "tmds.WatcherTest";

        using var serviceConnection = new DBusConnection(dbusDaemon.Address!);
        await serviceConnection.ConnectAsync();
        await serviceConnection.RequestNameAsync(wellKnownName);

        using var clientConnection = new DBusConnection(dbusDaemon.Address!);
        await clientConnection.ConnectAsync();

        using var watcher = await clientConnection.WatchNameOwnerAsync(wellKnownName);
        string ownerStr = await watcher.WaitForOwnerAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Subscribe to a signal using the owner identifier.
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = ownerStr,
            Interface = "com.example.TestInterface",
            Member = "TestSignal",
            Path = "/com/example/Object"
        };
        var observer = await clientConnection.AddMatchAsync(rule,
            (Message m, object? s) => m.GetBodyReader().ReadString(),
            (Exception? ex, string msg, object? rs, object? hs) => { },
            null, null, false, ObserverFlags.None);

        // Dispose the observer — this triggers RemoveWatcherUser which
        // must correctly clean up the name owner watcher subscription
        // via the AddMatchAsyncOwnerTask continuation.
        observer.Dispose();

        // Flush to allow the continuation to execute.
        Assert.False(await clientConnection.ReleaseNameAsync("com.example.FakeName"));

        // The name owner watcher should still work correctly.
        Assert.NotNull(watcher.GetCurrentOwner());
    }

    [Theory]
    [InlineData("com.example.Foo(:1.42@1234567890)", ":1.42")]
    [InlineData("org.freedesktop.NetworkManager(:1.0@9999)", ":1.0")]
    [InlineData("a(:1.123@0)", ":1.123")]
    public void GetOwnerBusName_ValidIdentifier_ReturnsBusName(string ownerIdentifier, string expectedBusName)
    {
        Assert.Equal(expectedBusName, NameOwnerWatcher.GetOwnerBusName(ownerIdentifier));
    }

    [Theory]
    [InlineData("")]
    [InlineData(":1.42")]
    [InlineData("com.example.Foo")]
    [InlineData("com.example.Foo()")]
    [InlineData("(:1.42@123)")]
    public void GetOwnerBusName_InvalidIdentifier_ThrowsArgumentException(string ownerIdentifier)
    {
        Assert.Throws<ArgumentException>(() => NameOwnerWatcher.GetOwnerBusName(ownerIdentifier));
    }

    private class EchoHandler : IMethodHandler
    {
        public string Path => "/echo";

        public bool RunMethodHandlerSynchronously(Message message) => true;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            using var writer = context.CreateReplyWriter("s");
            writer.WriteString("pong");
            context.Reply(writer.CreateMessage());
            return default;
        }
    }
}

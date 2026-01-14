using System;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class ServiceNameTests
{
    [Fact]
    public async Task Register()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            await conn1.RequestNameAsync(serviceName, RequestNameOptions.None);

            bool released = await conn1.ReleaseNameAsync(serviceName);
            Assert.True(released);
        }
    }

    [Fact]
    public async Task NameAlreadyRegisteredOnOtherConnection()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();
            await conn1.RequestNameAsync(serviceName, RequestNameOptions.None);

            var conn2 = new DBusConnection(address!);
            await conn2.ConnectAsync();
            await Assert.ThrowsAsync<DBusException>(() => conn2.RequestNameAsync(serviceName, RequestNameOptions.None));
        }
    }

    [Fact]
    public async Task NameAlreadyRegisteredOnSameConnection()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();
            await conn1.RequestNameAsync(serviceName, RequestNameOptions.None);
            await Assert.ThrowsAsync<InvalidOperationException>(() => conn1.RequestNameAsync(serviceName, RequestNameOptions.None));
        }
    }

    [Fact]
    public async Task ReplaceRegistered()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();
            await conn1.RequestNameAsync(serviceName, RequestNameOptions.AllowReplacement);

            var conn2 = new DBusConnection(address!);
            await conn2.ConnectAsync();
            await conn2.RequestNameAsync(serviceName, RequestNameOptions.ReplaceExisting);
        }
    }

    [Fact]
    public async Task EmitLost()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            var lostTcs = new TaskCompletionSource<bool>();
            await conn1.RequestNameAsync(serviceName, RequestNameOptions.AllowReplacement, onLost: (name, _) => lostTcs.SetResult(true));

            var conn2 = new DBusConnection(address!);
            await conn2.ConnectAsync();
            await conn2.RequestNameAsync(serviceName, RequestNameOptions.ReplaceExisting);

            // Wait for the lost event with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(lostTcs.Task, timeoutTask);
            Assert.Equal(lostTcs.Task, completedTask);
            Assert.True(await lostTcs.Task);
        }
    }

    [Fact]
    public async Task EmitAcquired()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            var conn1AcquiredTcs = new TaskCompletionSource<bool>();
            var conn1LostTcs = new TaskCompletionSource<bool>();
            await conn1.QueueNameRequestAsync(serviceName,
                RequestNameOptions.AllowReplacement,
                onAcquired: (name, _) => conn1AcquiredTcs.SetResult(true),
                onLost: (name, _) => conn1LostTcs.SetResult(true));

            // Wait for conn1 to acquire the name
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(conn1AcquiredTcs.Task, timeoutTask);
            Assert.Equal(conn1AcquiredTcs.Task, completedTask);
            Assert.True(await conn1AcquiredTcs.Task);

            var conn2 = new DBusConnection(address!);
            await conn2.ConnectAsync();

            var conn2AcquiredTcs = new TaskCompletionSource<bool>();
            var conn2LostTcs = new TaskCompletionSource<bool>();
            await conn2.QueueNameRequestAsync(serviceName,
                onAcquired: (name, _) => conn2AcquiredTcs.SetResult(true),
                onLost: (name, _) => conn2LostTcs.SetResult(true));

            // Wait for conn1 to lose the name and conn2 to acquire it
            timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            completedTask = await Task.WhenAny(conn1LostTcs.Task, timeoutTask);
            Assert.Equal(conn1LostTcs.Task, completedTask);
            Assert.True(await conn1LostTcs.Task);

            timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            completedTask = await Task.WhenAny(conn2AcquiredTcs.Task, timeoutTask);
            Assert.Equal(conn2AcquiredTcs.Task, completedTask);
            Assert.True(await conn2AcquiredTcs.Task);

            // Verify conn2 lost event has not fired
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            Assert.False(conn2LostTcs.Task.IsCompleted);
        }
    }

    [Fact]
    public async Task UnregisterNonExistentName()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            bool released = await conn1.ReleaseNameAsync(serviceName);
            Assert.False(released);
        }
    }

    [Fact]
    public async Task RegisterWithDefaultOptions()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            // Register with default options (ReplaceExisting | AllowReplacement)
            await conn1.RequestNameAsync(serviceName);

            var conn2 = new DBusConnection(address!);
            await conn2.ConnectAsync();

            // Should be able to replace since default options include AllowReplacement
            await conn2.RequestNameAsync(serviceName);

            bool released = await conn2.ReleaseNameAsync(serviceName);
            Assert.True(released);
        }
    }

    [Fact]
    public async Task RegisterWithOnLostRequiresAllowReplacement()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            // Should throw when trying to set onLost without AllowReplacement
            await Assert.ThrowsAsync<ArgumentException>(() =>
                conn1.RequestNameAsync(serviceName, RequestNameOptions.None, onLost: (name, _) => { }));
        }
    }

    [Fact]
    public async Task QueueServiceWithOnLostRequiresAllowReplacement()
    {
        using (var dbusDaemon = new DBusDaemon())
        {
            string serviceName = "tmds.dbus.protocol.test";

            await dbusDaemon.StartAsync();
            var address = dbusDaemon.Address;

            var conn1 = new DBusConnection(address!);
            await conn1.ConnectAsync();

            // Should throw when trying to set onLost without AllowReplacement
            await Assert.ThrowsAsync<ArgumentException>(() =>
                conn1.QueueNameRequestAsync(serviceName, RequestNameOptions.None, onLost: (name, _) => { }));
        }
    }
}

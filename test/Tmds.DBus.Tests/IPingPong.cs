using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    [DBusInterface("tmds.dbus.tests.PingPong")]
    public interface IPingPong : IDBusObject
    {
        Task PingAsync(string message, CancellationToken cancellationToken);
        Task<IDisposable> WatchPongAsync(Action<string> reply, CancellationToken cancellationToken);
    }
}
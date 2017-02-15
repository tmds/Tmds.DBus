using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    class PingPong : IPingPong
    {
        public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/pingpong");

        public event Action<string> OnPing;

        public Task PingAsync(string message)
        {
            OnPing?.Invoke(message);
            return Task.CompletedTask;
        }

        public Task<IDisposable> WatchPongAsync(Action<string> reply)
        {
            return EventHandler.AddAsync(this, nameof(OnPing), reply);
        }

        public ObjectPath ObjectPath { get { return Path; } }
    }
}
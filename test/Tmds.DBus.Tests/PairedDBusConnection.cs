using System;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    class PairedDBusConnection
    {
        public static async Task<Tuple<IDBusConnection, IDBusConnection>> CreateConnectedPairAsync()
        {
            var streams = PairedMessageStream.CreatePair();
            var conn1 = new DBusConnection(streams.Item1);
            var task1 = conn1.ConnectAsync();
            var conn2 = new DBusConnection(streams.Item2);
            var task2 = conn2.ConnectAsync();
            await task1;
            await task2;
            return Tuple.Create<IDBusConnection, IDBusConnection>(conn1, conn2);
        }
    }
}

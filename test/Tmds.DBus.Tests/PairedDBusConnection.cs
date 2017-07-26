using System;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    class PairedDBusConnection
    {
        public static async Task<Tuple<IDBusConnection, IDBusConnection>> CreateConnectedPairAsync()
        {
            var streams = PairedMessageStream.CreatePair();
            var task1 = DBusConnection.CreateAndConnectAsync(streams.Item1);
            var task2 = DBusConnection.CreateAndConnectAsync(streams.Item2);
            var conn1 = await task1;
            var conn2 = await task2;
            return Tuple.Create<IDBusConnection, IDBusConnection>(conn1, conn2);
        }
    }
}

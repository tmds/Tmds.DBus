using System;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    class PairedConnection
    {
        public static async Task<Tuple<IConnection, IConnection>> CreateConnectedPairAsync()
        {
            var dbusConnections = await PairedDBusConnection.CreateConnectedPairAsync();
            var conn1 = new Connection("conn1-address");
            conn1.Connect(dbusConnections.Item1);
            var conn2 = new Connection("conn2-address");
            conn2.Connect(dbusConnections.Item2);
            return Tuple.Create<IConnection, IConnection>(conn1, conn2);
        }
    }
}
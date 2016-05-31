using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class TransportTests
    {
        [InlineData(DBusDaemonProtocol.Tcp)]
        [InlineData(DBusDaemonProtocol.Unix)]
        [InlineData(DBusDaemonProtocol.UnixAbstract)]
        [Theory]
        public async Task Transport(DBusDaemonProtocol protocol)
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(protocol);
                var connection = new Connection(dbusDaemon.Address);
                await connection.ConnectAsync();

                Assert.StartsWith(":", connection.LocalName);
                Assert.Equal(true, connection.RemoteIsBus);
            }
        }

        [Fact]
        public async Task TryMultipleAddresses()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();

                string address = "unix:path=/does/not/exist;"
                                 + dbusDaemon.Address;

                var connection = new Connection(address);
                await connection.ConnectAsync();

                Assert.StartsWith(":", connection.LocalName);
                Assert.Equal(true, connection.RemoteIsBus);
            }
        }
    }
}
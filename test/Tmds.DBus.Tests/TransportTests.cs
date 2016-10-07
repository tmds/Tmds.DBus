using System.IO;
using System.Threading.Tasks;
using Xunit;
using XunitSkip;

namespace Tmds.DBus.Tests
{
    public class TransportTests
    {
        private static bool IsSELinux => Directory.Exists("/etc/selinux");

        [InlineData(DBusDaemonProtocol.Tcp)]
        [InlineData(DBusDaemonProtocol.Unix)]
        [InlineData(DBusDaemonProtocol.UnixAbstract)]
        [SkippableTheory]
        public async Task Transport(DBusDaemonProtocol protocol)
        {
            if (IsSELinux && protocol == DBusDaemonProtocol.Tcp)
            {
                // On an SELinux system the DBus daemon gets the security context
                // of the peer: the call 'getsockopt(fd, SOL_SOCKET, SO_PEERSEC, ...)'
                // returns ENOPROTOOPT and the daemon closes the connection.
                throw new SkipTestException("Cannot provide SELinux context to DBus daemon over TCP");
            }
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
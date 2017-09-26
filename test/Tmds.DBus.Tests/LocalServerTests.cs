using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Tests
{
    public class LocalServerTests
    {
        [InlineData(DBusDaemonProtocol.Tcp)]
        [InlineData(DBusDaemonProtocol.Unix)]
        [InlineData(DBusDaemonProtocol.UnixAbstract)]
        [Theory]
        public async Task LocalServer(DBusDaemonProtocol protocol)
        {
            string service = "any.service";
            string listenAddress = null;
            switch (protocol)
            {
                case DBusDaemonProtocol.Tcp:
                    listenAddress = "tcp:host=localhost";
                    break;
                case DBusDaemonProtocol.Unix:
                    listenAddress = $"unix:path={Path.GetTempPath()}/{Path.GetRandomFileName()}";
                    break;
                case DBusDaemonProtocol.UnixAbstract:
                    listenAddress = $"unix:abstract={Guid.NewGuid()}";
                    break;
            }

            // server
            var server = new ServerConnectionOptions();
            using (var connection = new Connection(server))
            {
                await connection.RegisterObjectAsync(new PingPong());
                var boundAddress = await server.StartAsync(listenAddress);

                for (int i = 0; i < 2; i++)
                {
                    // client
                    using (var client = new Connection(boundAddress))
                    {
                        // method
                        await client.ConnectAsync();
                        var proxy = client.CreateProxy<IPingPong>(service, PingPong.Path);
                        var echoed = await proxy.EchoAsync("test");
                        Assert.Equal("test", echoed);

                        // signal
                        var tcs = new TaskCompletionSource<string>();
                        await proxy.WatchPongAsync(message => tcs.SetResult(message));
                        await proxy.PingAsync("hello world");
                        var reply = await tcs.Task;
                        Assert.Equal("hello world", reply);
                    }
                }
            }
        }
    }
}
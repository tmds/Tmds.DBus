# How-to

## Local Server

Tmds.DBus supports running an in-process server that accepts connections. This allows other clients to connect
without an intermediate bus.

This is done by passing `ServerConnectionOptions` to the `Connection` constructor.
Calling `StartAsync` on the `ServerConnectionOptions` then enables the server.
The server is disposed together with the `Connection`.

```C#
using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Example
{
    [DBusInterface("tmds.greet")]
    public interface IGreet : IDBusObject
    {
        Task<string> GreetAsync(string message);
    }

    class Greet : IGreet
    {
        public static readonly ObjectPath Path = new ObjectPath("/tmds/greet");

        public Task<string> GreetAsync(string name)
        {
            return Task.FromResult($"Hello {name}!");
        }

        public ObjectPath ObjectPath { get { return Path; } }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // server
            var server = new ServerConnectionOptions();
            using (var connection = new Connection(server))
            {
                await connection.RegisterObjectAsync(new Greet());
                var boundAddress = await server.StartAsync("tcp:host=localhost");
                System.Console.WriteLine($"Server listening at {boundAddress}");

                // client
                using (var client = new Connection(boundAddress))
                {
                    await client.ConnectAsync();
                    System.Console.WriteLine("Client connected");
                    var proxy = client.CreateProxy<IGreet>("any.service", Greet.Path);
                    var greeting = await proxy.GreetAsync("world");
                    System.Console.WriteLine(greeting);
                }
            }
        }
    }
}
```
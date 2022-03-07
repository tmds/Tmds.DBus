# How-to

## Threading

D-Bus APIs assume peers will consume the messages from a single logical thread.

For example, this preserves the ordering of values from `PropertyChanged` signals, and method returns of `Properties.Get`.

By design .NET Tasks do not preserve the ordering because (a.) a Task can complete
synchronously, and (b.) a Task can complete on the threadpool when using `TaskCreationOptions.RunContinuationsAsynchronously`.

`Tmds.DBus` supports two modes of operations that preserve the ordering.

When the application has a single-threaded `SynchronizationContext`, it can be set on `ConnectionOptions`.
All signals will be emitted on that context. The user is assumed to be using that `SynchronizationContext` while making method calls,
causing the completions to be executed on that context. This casues the `SynchronizationContext` to be used as the single logical thread.

Otherwise, the `SynchronizationContext` should be kept at the default value of `null`.
All signals will be emitted directly from the read loop, and all method continuations will be completed synchronously from that loop.
This causes the read loop to be used as the single logical thread.
The user must ensure the loop is not blocked by not making synchronous calls on the continuation.

If you are writing some re-usable code (like a library), you can either let the user provide a `SynchronizationContext` and set it on `ConnectionOptions`. Or, you can choose to use a `null` `SynchronizationContext` and ensure all method are awaited `ConfigureAwait(false)`.

If your API usage doesn't require ordering to be preserved, you can set the `RunContinuationsAsynchronously` `ConnectionOption` to `true`.

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
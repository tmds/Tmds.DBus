using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests
{
    public class ConnectionTests
    {
        [Fact]
        public async Task MethodAsync()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;
            conn2.AddMethodHandler(new StringOperations());
            var proxy = new StringOperationsProxy(conn1, "servicename");
            var reply = await proxy.ConcatAsync("hello ", "world");
            Assert.Equal("hello world", reply);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SignalAsync(bool setSynchronizationContext)
        {
            if (setSynchronizationContext)
            {
                SynchronizationContext.SetSynchronizationContext(new MySynchronizationContext());
            }
            SynchronizationContext? expectedSynchronizationContext = SynchronizationContext.Current;

            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            var proxy = new HelloWorld(conn1, "servicename");
            var msgTcs = new TaskCompletionSource<(string, SynchronizationContext?)>();
            var exTcs = new TaskCompletionSource<(Exception?, SynchronizationContext?)>();

            await proxy.WatchHelloWorldAsync((ex, msg) =>
                {
                    if (msg is not null)
                    {
                        msgTcs.SetResult((msg, SynchronizationContext.Current));
                    }
                    else
                    {
                        exTcs.SetResult((ex, SynchronizationContext.Current));
                    }
                });

            SendHelloWorld(conn2);

            var msg = await msgTcs.Task;
            Assert.Equal("hello world", msg.Item1);
            Assert.Equal(expectedSynchronizationContext, msg.Item2);

            conn1.Dispose();

            var ex = await exTcs.Task;
            Assert.IsType<DisconnectedException>(ex.Item1);
            Assert.Equal(expectedSynchronizationContext, ex.Item2);

            static void SendHelloWorld(Connection connection)
            {
                using var writer = connection.GetMessageWriter();

                writer.WriteSignalHeader(
                    path: HelloWorldConstants.Path,
                    @interface: HelloWorldConstants.Interface,
                    signature: "s",
                    member: HelloWorldConstants.OnHelloWorld);

                writer.WriteString("hello world");

                MessageBuffer buffer = writer.CreateMessage();

                bool messageSent = connection.TrySendMessage(buffer);
                Assert.True(messageSent);
            }
        }

        sealed class MySynchronizationContext : SynchronizationContext
        { }

        [InlineData("tcp:host=localhost,port=1")]
        [InlineData("unix:path=/does/not/exist")]
        [Theory]
        public async Task UnreachableAddressAsync(string address)
        {
            using (var connection = new Connection(address))
            {
                await Assert.ThrowsAsync<ConnectException>(() => connection.ConnectAsync().AsTask());
            }
        }

        static class HelloWorldConstants
        {
            public const string Path = "/path";
            public const string OnHelloWorld = "OnHelloWorld";
            public const string Interface = "tmds.dbus.tests.HelloWorld";
        }

        class HelloWorld
        {
            protected Connection Connection { get; }
            public string Peer { get; }

            public HelloWorld(Connection connection, string peer)
                => (Connection, Peer) = (connection, peer);

            public ValueTask<IDisposable> WatchHelloWorldAsync(Action<Exception?, string> handler, bool emitOnCapturedContext = true)
                => WatchSignalAsync<string>(Peer, HelloWorldConstants.Interface, HelloWorldConstants.Path, "OnHelloWorld", (Message m, object? s) => m.GetBodyReader().ReadString(), handler, emitOnCapturedContext);

            public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext)
            {
                var rule = new MatchRule
                {
                    Type = MessageType.Signal,
                    Sender = sender,
                    Path = path,
                    Member = signal,
                    Interface = @interface
                };
                return this.Connection.AddMatchAsync(rule, reader,
                                                        (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
                                                        this, handler, emitOnCapturedContext);
            }
            public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext)
            {
                var rule = new MatchRule
                {
                    Type = MessageType.Signal,
                    Sender = sender,
                    Path = path,
                    Member = signal,
                    Interface = @interface
                };
                return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,
                                                                (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext);
            }
        }

        static class StringOperationsConstants
        {
            public const string Path = "/tmds/dbus/tests/stringoperations";
            public const string Concat = "Concat";
            public const string Interface = "tmds.dbus.tests.StringOperations";
        }

        class StringOperationsProxy
        {
            private readonly Connection _connection;
            private readonly string _peer;

            public StringOperationsProxy(Connection connection, string peer)
            {
                _connection = connection;
                _peer = peer;
            }

            public Task<string> ConcatAsync(string lhs, string rhs)
            {
                return _connection.CallMethodAsync(
                    CreateAddMessage(),
                    (Message message, object? state) =>
                    {
                        return message.GetBodyReader().ReadString();
                    });

                MessageBuffer CreateAddMessage()
                {
                    using var writer = _connection.GetMessageWriter();

                    writer.WriteMethodCallHeader(
                        destination: _peer,
                        path: StringOperationsConstants.Path,
                        @interface: StringOperationsConstants.Interface,
                        signature: "ss",
                        member: StringOperationsConstants.Concat);

                    writer.WriteString(lhs);
                    writer.WriteString(rhs);

                    return writer.CreateMessage();
                }
            }
        }

        class StringOperations : IMethodHandler
        {
            public string Path => "/tmds/dbus/tests/stringoperations";

            public const string ConcatMember = "Concat";

            public bool RunMethodHandlerSynchronously(Message message) => true;

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                var request = context.Request;
                switch (request.InterfaceAsString)
                {
                    case StringOperationsConstants.Interface:
                        switch ((request.MemberAsString, request.SignatureAsString))
                        {
                            case (ConcatMember, "ss"):
                                return Concat(context);
                        }
                        break;
                }
                return default;
            }

            private ValueTask Concat(MethodContext context)
            {
                var request = context.Request;
                var reader = request.GetBodyReader();
                string lhs = reader.ReadString();
                string rhs = reader.ReadString();

                string result = lhs + rhs;

                using var writer = context.CreateReplyWriter("s");
                writer.WriteString(result);
                context.Reply(writer.CreateMessage());

                return default;
            }
        }
    }
}
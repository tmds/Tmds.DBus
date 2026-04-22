using System;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests
{
    public class ExceptionContextTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SignalHandlerException_DisconnectConnection(bool disconnectConnection)
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    Assert.True(ctx.DisconnectConnection);
                    ctx.DisconnectConnection = disconnectConnection;
                    exceptionHandlerTcs.TrySetResult(ctx);
                }
            };
            var connections = PairedConnection.CreatePair(options);
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = conn2.UniqueName,
                Path = HelloWorldConstants.Path,
                Member = HelloWorldConstants.OnHelloWorld,
                Interface = HelloWorldConstants.Interface
            };

            await conn1.AddMatchAsync<string>(
                rule,
                reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
                handler: ctx =>
                {
                    if (ctx.HasValue)
                    {
                        throw new InvalidOperationException("Handler error");
                    }
                },
                emitOnCapturedContext: false,
                flags: ObserverFlags.None);

            SendHelloWorldSignal(conn2);

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.SignalHandler, exceptionContext.Source);
            Assert.IsType<InvalidOperationException>(exceptionContext.Exception);
            Assert.Equal("Handler error", exceptionContext.Exception.Message);
            Assert.Equal(disconnectConnection, exceptionContext.DisconnectConnection);

            if (disconnectConnection)
            {
                var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsType<InvalidOperationException>(disconnectException);
                Assert.Equal("Handler error", disconnectException!.Message);
            }
            else
            {
                // Does not throw.
                await AssertConnectionWorks(conn1);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SignalReaderException_DisconnectConnection(bool disconnectConnection)
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    Assert.True(ctx.DisconnectConnection);
                    ctx.DisconnectConnection = disconnectConnection;
                    exceptionHandlerTcs.TrySetResult(ctx);
                }
            };
            var connections = PairedConnection.CreatePair(options);
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = conn2.UniqueName,
                Path = HelloWorldConstants.Path,
                Member = HelloWorldConstants.OnHelloWorld,
                Interface = HelloWorldConstants.Interface
            };

            await conn1.AddMatchAsync<string>(
                rule,
                reader: (Message m, object? s) => throw new InvalidOperationException("Reader error"),
                handler: ctx => { },
                emitOnCapturedContext: false,
                flags: ObserverFlags.None);

            SendHelloWorldSignal(conn2);

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.SignalReader, exceptionContext.Source);
            Assert.IsType<InvalidOperationException>(exceptionContext.Exception);
            Assert.Equal("Reader error", exceptionContext.Exception.Message);
            Assert.Equal(disconnectConnection, exceptionContext.DisconnectConnection);

            if (disconnectConnection)
            {
                var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsType<InvalidOperationException>(disconnectException);
                Assert.Equal("Reader error", disconnectException!.Message);
            }
            else
            {
                // Does not throw.
                await AssertConnectionWorks(conn1);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MethodHandlerException_DisconnectConnection(bool disconnectConnection)
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    Assert.True(ctx.DisconnectConnection);
                    ctx.DisconnectConnection = disconnectConnection;
                    exceptionHandlerTcs.TrySetResult(ctx);
                }
            };
            var connections = PairedConnection.CreatePair(options);
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            conn1.AddMethodHandler(new ThrowingMethodHandler());

            var message = CreateMethodCallMessage(conn2, "/tmds/dbus/tests/throwing", "DoSomething");
            try { await conn2.CallMethodAsync(message); } catch { }

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.MethodHandler, exceptionContext.Source);
            Assert.IsType<InvalidOperationException>(exceptionContext.Exception);
            Assert.Equal("Method handler error", exceptionContext.Exception.Message);
            Assert.Equal(disconnectConnection, exceptionContext.DisconnectConnection);

            if (disconnectConnection)
            {
                var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsType<InvalidOperationException>(disconnectException);
            }
            else
            {
                // Verify the error reply was sent back.
                var ex = await Assert.ThrowsAsync<DBusErrorReplyException>(
                    () => conn2.CallMethodAsync(CreateMethodCallMessage(conn2, "/tmds/dbus/tests/throwing", "DoSomething")));
                Assert.Equal("org.freedesktop.DBus.Error.Failed", ex.ErrorName);
                Assert.Contains("Method handler error", ex.ErrorMessage);
            }
        }

        [Fact]
        public async Task ConnectionClosed_CallsOnException()
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx => exceptionHandlerTcs.TrySetResult(ctx)
            };
            var connections = PairedConnection.CreatePair(options);
            using var conn1 = connections.Item1;
            var conn2 = connections.Item2;

            // Disposing conn2 closes the peer, causing conn1's transport to fail
            conn2.Dispose();

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(DBusConnection.ExceptionSource.ConnectionFailed, exceptionContext.Source);
            Assert.True(exceptionContext.DisconnectConnection);
        }

        [Fact]
        public async Task SignalHandlerException_DisconnectTrue_DoesNotFireConnectionClosed()
        {
            int exceptionHandlerCallCount = 0;
            DBusConnection.ExceptionSource? firstSource = null;
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    exceptionHandlerCallCount++;
                    firstSource ??= ctx.Source;
                    exceptionHandlerTcs.TrySetResult(ctx);
                }
            };
            var connections = PairedConnection.CreatePair(options);
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = conn2.UniqueName,
                Path = HelloWorldConstants.Path,
                Member = HelloWorldConstants.OnHelloWorld,
                Interface = HelloWorldConstants.Interface
            };

            await conn1.AddMatchAsync<string>(
                rule,
                reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
                handler: ctx =>
                {
                    if (ctx.HasValue)
                    {
                        throw new InvalidOperationException("Handler error");
                    }
                },
                emitOnCapturedContext: false,
                flags: ObserverFlags.None);

            SendHelloWorldSignal(conn2);

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.SignalHandler, exceptionContext.Source);

            // Wait for disconnect to fully complete
            await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));

            // The exception handler should only have been called once (for SignalHandler),
            // not a second time for ConnectionFailed.
            Assert.Equal(1, exceptionHandlerCallCount);
            Assert.Equal(DBusConnection.ExceptionSource.SignalHandler, firstSource);
        }

        [Fact]
        public async Task NoOnException_MethodHandlerException_Disconnects()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            conn1.AddMethodHandler(new ThrowingMethodHandler());

            var message = CreateMethodCallMessage(conn2, "/tmds/dbus/tests/throwing", "DoSomething");
            try { await conn2.CallMethodAsync(message); } catch { }

            var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsType<InvalidOperationException>(disconnectException);
        }

        [Fact]
        public async Task NoOnException_SignalHandlerException_Disconnects()
        {
            // Default behavior when no OnException is set
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            var rule = new MatchRule
            {
                Type = MessageType.Signal,
                Sender = conn2.UniqueName,
                Path = HelloWorldConstants.Path,
                Member = HelloWorldConstants.OnHelloWorld,
                Interface = HelloWorldConstants.Interface
            };

            await conn1.AddMatchAsync<string>(
                rule,
                reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
                handler: ctx =>
                {
                    if (ctx.HasValue)
                    {
                        throw new InvalidOperationException("Handler error");
                    }
                },
                emitOnCapturedContext: false,
                flags: ObserverFlags.EmitOnConnectionFailed);

            SendHelloWorldSignal(conn2);

            var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsType<InvalidOperationException>(disconnectException);
        }

        static class HelloWorldConstants
        {
            public const string Path = "/path";
            public const string OnHelloWorld = "OnHelloWorld";
            public const string Interface = "tmds.dbus.tests.HelloWorld";
        }

        static void SendHelloWorldSignal(DBusConnection connection)
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

        class ThrowingMethodHandler : IPathMethodHandler
        {
            public string Path => "/tmds/dbus/tests/throwing";
            public bool HandlesChildPaths => false;

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                throw new InvalidOperationException("Method handler error");
            }
        }

        static MessageBuffer CreateMethodCallMessage(DBusConnection connection, string path, string member)
        {
            using var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                path: path,
                @interface: "tmds.dbus.tests",
                member: member);
            return writer.CreateMessage();
        }

        static async Task AssertConnectionWorks(DBusConnection connection)
        {
            try
            {
                await connection.ReleaseNameAsync("name");
            }
            catch (DBusErrorReplyException)
            { }
        }
    }
}

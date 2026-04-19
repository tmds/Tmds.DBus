using System;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests
{
    public class ExceptionContextTests
    {
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

        [Fact]
        public async Task SignalHandlerException_DisconnectConnectionTrue_Disconnects()
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    // DisconnectConnection defaults to true — leave it.
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
                flags: ObserverFlags.EmitOnConnectionFailed);

            SendHelloWorldSignal(conn2);

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.SignalHandler, exceptionContext.Source);
            Assert.IsType<InvalidOperationException>(exceptionContext.Exception);
            Assert.Equal("Handler error", exceptionContext.Exception.Message);

            var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsType<InvalidOperationException>(disconnectException);
            Assert.Equal("Handler error", disconnectException!.Message);
        }

        [Fact]
        public async Task SignalHandlerException_DisconnectConnectionFalse_DoesNotDisconnect()
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    ctx.DisconnectConnection = false;
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

            // Signal triggers the exception
            SendHelloWorldSignal(conn2);

            // Wait for the exception handler to be called
            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.SignalHandler, exceptionContext.Source);
            Assert.False(exceptionContext.DisconnectConnection);

            // Connection is still alive — a new observer can receive signals
            var valueTcs = new TaskCompletionSource<string>();
            await conn1.AddMatchAsync<string>(
                rule,
                reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
                handler: ctx =>
                {
                    if (ctx.HasValue)
                    {
                        valueTcs.TrySetResult(ctx.Value);
                    }
                },
                emitOnCapturedContext: false,
                flags: ObserverFlags.None);

            SendHelloWorldSignal(conn2);

            var value = await valueTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("hello world", value);
        }

        [Fact]
        public async Task SignalReaderException_DisconnectConnectionFalse_DoesNotDisconnect()
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    ctx.DisconnectConnection = false;
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

            // Signal triggers the reader exception
            SendHelloWorldSignal(conn2);

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.SignalReader, exceptionContext.Source);
            Assert.IsType<InvalidOperationException>(exceptionContext.Exception);
            Assert.Equal("Reader error", exceptionContext.Exception.Message);

            // Connection is still alive — a new observer can receive signals
            var valueTcs = new TaskCompletionSource<string>();
            await conn1.AddMatchAsync<string>(
                rule,
                reader: (Message m, object? s) => m.GetBodyReader().ReadString(),
                handler: ctx =>
                {
                    if (ctx.HasValue)
                    {
                        valueTcs.TrySetResult(ctx.Value);
                    }
                },
                emitOnCapturedContext: false,
                flags: ObserverFlags.None);

            SendHelloWorldSignal(conn2);

            var value = await valueTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("hello world", value);
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

        [Fact]
        public async Task MethodHandlerException_DisconnectConnectionFalse_DoesNotDisconnect()
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
                    ctx.DisconnectConnection = false;
                    exceptionHandlerTcs.TrySetResult(ctx);
                }
            };
            var connections = PairedConnection.CreatePair(options);
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            conn1.AddMethodHandler(new ThrowingMethodHandler());

            var message = CreateMethodCallMessage(conn2, "/tmds/dbus/tests/throwing", "DoSomething");
            var ex = await Assert.ThrowsAsync<DBusErrorReplyException>(() => conn2.CallMethodAsync(message));
            Assert.Equal("org.freedesktop.DBus.Error.Failed", ex.ErrorName);
            Assert.Contains("Method handler error", ex.ErrorMessage);

            var exceptionContext = await exceptionHandlerTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(DBusConnection.ExceptionSource.MethodHandler, exceptionContext.Source);
            Assert.False(exceptionContext.DisconnectConnection);

            // Connection is still alive — another method call gets a reply
            await Assert.ThrowsAsync<DBusErrorReplyException>(
                () => conn2.CallMethodAsync(CreateMethodCallMessage(conn2, "/tmds/dbus/tests/throwing", "DoSomething")));
        }

        [Fact]
        public async Task MethodHandlerException_DisconnectConnectionTrue_Disconnects()
        {
            var exceptionHandlerTcs = new TaskCompletionSource<DBusConnection.ExceptionContext>();
            var options = new DBusConnectionOptions("conn1-address")
            {
                OnException = ctx =>
                {
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
            Assert.True(exceptionContext.DisconnectConnection);

            var disconnectException = await conn1.DisconnectedAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsType<InvalidOperationException>(disconnectException);
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
        public async Task NoOnException_SignalHandlerException_StillDisconnects()
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
    }
}

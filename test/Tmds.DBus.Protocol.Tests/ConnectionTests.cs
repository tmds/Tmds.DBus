using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Win32.SafeHandles;
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

        [Fact]
        public async Task DisconnectedException()
        {
            var streams = PairedMessageStream.CreatePair();
            using var conn1 = new Connection("conn1-address");
            conn1.Connect(streams.Item1);
            using var conn2 = new Connection("conn2-address");
            conn2.Connect(streams.Item2);

            // Close the stream at one end.
            ((PairedMessageStream)streams.Item2).Close();
            await Task.Yield();

            var proxy = new StringOperationsProxy(conn1, "servicename");
            await Assert.ThrowsAsync<DisconnectedException>(() => proxy.ConcatAsync("hello ", "world"));
        }

        [Fact]
        public async Task DisposeTriggersRequestAborted()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            var handler = new WaitForCancellationHandler();
            conn2.AddMethodHandler(handler);

            Task pendingCall = conn1.CallMethodAsync(CreateMessage());

            await handler.WaitForRequestReceivedAsync().WaitAsync(timeout);

            conn2.Dispose();

            await Assert.ThrowsAsync<DisconnectedException>(() => pendingCall);

            await handler.WaitForCancelledAsync().WaitAsync(timeout);

            MessageBuffer CreateMessage()
            {
                using var writer = conn1.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    path: handler.Path,
                    @interface: "org.any",
                    member: "Any");
                return writer.CreateMessage();
            }
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

        [Fact]
        public async Task ConnectionRespondsToDBusPeerPing()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            await conn1.CallMethodAsync(CreateMessage());

            MessageBuffer CreateMessage()
            {
                using var writer = conn1.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    @interface: "org.freedesktop.DBus.Peer",
                    member: "Ping");
                return writer.CreateMessage();
            }
        }

        [Fact]
        public async Task ConnectionRespondsToDBusPeerGetMachineId()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            string machineId = await conn1.CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadString(), null);
            Assert.Equal(32, machineId.Length);
            Assert.True(Guid.TryParse(machineId, out _));

            MessageBuffer CreateMessage()
            {
                using var writer = conn1.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    @interface: "org.freedesktop.DBus.Peer",
                    member: "GetMachineId");
                return writer.CreateMessage();
            }
        }

        [Theory, MemberData(nameof(IntrospectionTestData))]
        public async Task Introspection(string path, string? expectedXml)
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            conn2.AddMethodHandlers(new []
            {
                new IntrospectionTestMethodHandler("/root/handler1", interfaceXml:
                    """
                    <interface name="handler1"/>

                    """),
                new IntrospectionTestMethodHandler("/root/handler2", interfaceXml: null),
            });

            if (expectedXml is null)
            {
                await Assert.ThrowsAsync<DBusException>(() => conn1.CallMethodAsync(CreateMessage(path), (Message m, object? s) => m.GetBodyReader().ReadString(), null));
            }
            else
            {
                // Validate the expected XML.
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(expectedXml);
                Assert.EndsWith("\n", expectedXml);

                string actualXml = await conn1.CallMethodAsync(CreateMessage(path), (Message m, object? s) => m.GetBodyReader().ReadString(), null);

                Assert.Equal(expectedXml, actualXml);
            }

            MessageBuffer CreateMessage(string path)
            {
                using var writer = conn1.GetMessageWriter();
                writer.WriteMethodCallHeader(
                    path: path,
                    @interface: "org.freedesktop.DBus.Introspectable",
                    member: "Introspect");
                return writer.CreateMessage();
            }
        }

        public static IEnumerable<object[]> IntrospectionTestData
        {
            get
            {
                yield return new object[]
                {
                    "/no_such_path",
                    null!
                };
                yield return new object[]
                {
                    "/root/handler1",
                    """
                    <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
                    <node>
                    <interface name="handler1"/>
                    <interface name="org.freedesktop.DBus.Introspectable">
                      <method name="Introspect">
                        <arg type="s" name="xml_data" direction="out"/>
                      </method>
                    </interface>
                    <interface name="org.freedesktop.DBus.Peer">
                      <method name="Ping"/>
                      <method name="GetMachineId">
                        <arg type="s" name="machine_uuid" direction="out"/>
                      </method>
                    </interface>
                    </node>

                    """
                };
                yield return new object[]
                {
                    "/root/handler2",
                    """
                    <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
                    <node>
                    <interface name="org.freedesktop.DBus.Introspectable">
                      <method name="Introspect">
                        <arg type="s" name="xml_data" direction="out"/>
                      </method>
                    </interface>
                    <interface name="org.freedesktop.DBus.Peer">
                      <method name="Ping"/>
                      <method name="GetMachineId">
                        <arg type="s" name="machine_uuid" direction="out"/>
                      </method>
                    </interface>
                    </node>

                    """
                };
                yield return new object[]
                {
                    "/root",
                    """
                    <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
                    <node>
                    <node name="handler1"/>
                    <node name="handler2"/>
                    </node>

                    """
                };
            }
        }

        private class IntrospectionTestMethodHandler : IMethodHandler
        {
            private readonly string? _interfaceXml;

            public IntrospectionTestMethodHandler(string path, string? interfaceXml)
            {
                Path = path;
                _interfaceXml = interfaceXml;
            }

            public string Path { get; }

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                if (context.IsDBusIntrospectRequest && _interfaceXml is not null)
                {
                    context.ReplyIntrospectXml([ Encoding.UTF8.GetBytes(_interfaceXml) ]);
                }

                return default;
            }

            public bool RunMethodHandlerSynchronously(Message message)
                => true;
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
                                                        this, handler, emitOnCapturedContext, ObserverFlags.EmitOnDispose);
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
                                                                (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext, ObserverFlags.EmitOnDispose);
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

        class WaitForCancellationHandler : IMethodHandler
        {
            public string Path => "/";

            private readonly TaskCompletionSource _cancelled = new();
            private readonly TaskCompletionSource _requestReceived = new();

            public async ValueTask HandleMethodAsync(MethodContext context)
            {
                _requestReceived.TrySetResult();
                try
                {
                    while (true)
                    {
                        await Task.Delay(int.MaxValue, context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    _cancelled.SetResult();

                    throw;
                }
            }

            public Task WaitForRequestReceivedAsync() => _requestReceived.Task;

            public Task WaitForCancelledAsync() => _cancelled.Task;

            public bool RunMethodHandlerSynchronously(Message message)
                => true;
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
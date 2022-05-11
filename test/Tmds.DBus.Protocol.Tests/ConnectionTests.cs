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

            public ValueTask<bool> TryHandleMethodAsync(Connection connection, Message message)
            {
                switch (message.InterfaceAsString)
                {
                    case StringOperationsConstants.Interface:
                        switch ((message.MemberAsString, message.SignatureAsString))
                        {
                            case (ConcatMember, "ss"):
                                Concat(connection, message);
                                return ValueTask.FromResult(true);
                        }
                        break;
                }

                return ValueTask.FromResult(true);
            }

            private void Concat(Connection connection, Message message)
            {
                var reader = message.GetBodyReader();

                string lhs = reader.ReadString();
                string rhs = reader.ReadString();

                string result = lhs + rhs;

                connection.TrySendMessage(CreateResponseMessage(connection, message, result));

                static MessageBuffer CreateResponseMessage(Connection connection, Message message, string result)
                {
                    using var writer = connection.GetMessageWriter();

                    writer.WriteMethodReturnHeader(
                        replySerial: message.Serial,
                        destination: message.Sender,
                        signature: "s"
                    );

                    writer.WriteString(result);

                    return writer.CreateMessage();
                }
            }
        }
    }
}
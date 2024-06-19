using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;
using XunitSkip;

namespace Tmds.DBus.Protocol.Tests
{
    public class TransportTests
    {
        [InlineData(DBusDaemonProtocol.Tcp)]
        [InlineData(DBusDaemonProtocol.Unix)]
        [InlineData(DBusDaemonProtocol.UnixAbstract)]
        [SkippableTheory]
        public async Task TransportAsync(DBusDaemonProtocol protocol)
        {
            if (DBusDaemon.IsSELinux && protocol == DBusDaemonProtocol.Tcp)
            {
                throw new SkipTestException("Cannot provide SELinux context to DBus daemon over TCP");
            }
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync(protocol);
                var connection = new Connection(dbusDaemon.Address!);
                await connection.ConnectAsync();

                Assert.StartsWith(":", connection.UniqueName);
            }
        }

        [Fact]
        public async Task TryMultipleAddressesAsync()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();

                string address = "unix:path=/does/not/exist;"
                                 + dbusDaemon.Address;

                var connection = new Connection(address);
                await connection.ConnectAsync();

                Assert.StartsWith(":", connection.UniqueName);
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public async Task SendSafehandle(bool explicitDispose)
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();

                using var conn2 = new Connection(dbusDaemon.Address!); ;
                await conn2.ConnectAsync();
                conn2.AddMethodHandler(new SafeHandleOperations());

                using var conn1 = new Connection(new ClientConnectionOptions(dbusDaemon.Address!) { AutoConnect = true });

                var wrappedHandle = new WrappedHandle(File.OpenHandle(Path.GetTempFileName()));
                // This is the first call on a closed AutoConnect method, it will first need to connect.
                Task pendingCall = conn1.CallMethodAsync(CreateMessage(wrappedHandle));
                if (explicitDispose)
                {
                    // Dispose the handle before the call with the handle is made.
                    wrappedHandle.Dispose();
                }
                // The call was succesful because the handle was already reffed.
                await pendingCall;

                // Verify the handle was unreffed and disposed.
                Assert.True(wrappedHandle.IsReleased);

                MessageBuffer CreateMessage(SafeHandle handle)
                {
                    using var writer = conn1.GetMessageWriter();
                    writer.WriteMethodCallHeader(
                        destination: conn2.UniqueName,
                        path: "/tmds/dbus/tests/safehandleoperations",
                        member: "ReceiveHandle",
                        signature: "h");
                    writer.WriteHandle(handle);
                    return writer.CreateMessage();
                }
            }
        }

        sealed class WrappedHandle : SafeHandleMinusOneIsInvalid
        {
            private readonly SafeHandle _innerHandle;

            public WrappedHandle(SafeHandle handle) : base(ownsHandle: true)
            {
                bool success = false;
                handle.DangerousAddRef(ref success);
                _innerHandle = handle;
                SetHandle(handle.DangerousGetHandle());
                handle.Dispose();
            }

            public bool IsReleased { get; private set; }

            protected override bool ReleaseHandle()
            {
                _innerHandle.DangerousRelease();
                IsReleased = true;
                return true;
            }
        }

        class SafeHandleOperations : IMethodHandler
        {
            public string Path => "/tmds/dbus/tests/safehandleoperations";

            public bool RunMethodHandlerSynchronously(Message message) => true;

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                var request = context.Request;
                switch ((request.MemberAsString, request.SignatureAsString))
                {
                    case ("ReceiveHandle", "h"):
                        return ReceiveHandle(context);
                }
                return default;
            }

            private ValueTask ReceiveHandle(MethodContext context)
            {
                var request = context.Request;
                var reader = request.GetBodyReader();
                using SafeHandle? handle = reader.ReadHandle<SafeFileHandle>();
                Assert.NotNull(handle);
                Assert.True(!handle.IsInvalid);
                Assert.True(!handle.IsClosed);

                using var writer = context.CreateReplyWriter(null);
                context.Reply(writer.CreateMessage());
                return default;
            }
        }
    }
}
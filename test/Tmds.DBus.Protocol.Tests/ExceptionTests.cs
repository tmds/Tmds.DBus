using System;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests
{
    public class ExceptionTests
    {
        [Fact]
        public async Task ObserverDisposed()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            TaskCompletionSource<Exception> tcs = new();

            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object? state) => "", (Exception? ex, string s, object? s1, object? s2) =>
                {
                    tcs.SetResult(ex!);
                });

            disposable.Dispose();

            Exception ex = await tcs.Task;
            Assert.True(MatchActionException.IsObserverDisposed(ex));
            Assert.True(MatchActionException.IsDisposed(ex));
        }

        [Fact]
        public async Task ConnectionDisposed()
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            TaskCompletionSource<Exception> tcs = new();

            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object? state) => "", (Exception? ex, string s, object? s1, object? s2) =>
                {
                    tcs.SetResult(ex!);
                });

            conn1.Dispose();

            Exception ex = await tcs.Task;
            Assert.True(MatchActionException.IsConnectionDisposed(ex));
            Assert.True(MatchActionException.IsDisposed(ex));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanOptOutObserverDisposedEmit(bool optIn)
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            Exception? exception = null;
            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object? state) => "", (Exception? ex, string s, object? s1, object? s2) =>
                {
                    exception ??= ex;
                }, null, null, synchronizationContext: null, optIn ? AddMatchFlags.EmitOnObserverDispose : AddMatchFlags.None);

            disposable.Dispose();

            if (optIn)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CanOptOutConnectionDisposedEmit(bool optIn)
        {
            var connections = PairedConnection.CreatePair();
            using var conn1 = connections.Item1;
            using var conn2 = connections.Item2;

            Exception? exception = null;
            TaskCompletionSource<Exception> tcs = new();

            var disposable = await conn1.AddMatchAsync(
                new MatchRule(), (Message message, object? state) => "", (Exception? ex, string s, object? s1, object? s2) =>
                {
                    exception ??= ex;
                }, null, null, synchronizationContext: null, optIn ? AddMatchFlags.EmitOnConnectionDispose : AddMatchFlags.None);

            conn1.Dispose();

            if (optIn)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }
    }
}
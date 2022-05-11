using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Protocol.Tests
{
    class PairedConnection
    {
        public static (Connection, Connection) CreatePair()
        {
            var streams = PairedMessageStream.CreatePair();
            var conn1 = new Connection("conn1-address");
            conn1.Connect(streams.Item1);
            var conn2 = new Connection("conn2-address");
            conn2.Connect(streams.Item2);
            return (conn1, conn2);
        }
    }

    class PairedMessageStream : IMessageStream
    {
        SemaphoreSlim _readSemaphore;
        ConcurrentQueue<MessageBuffer?> _readQueue;
        SemaphoreSlim _writeSemaphore;
        ConcurrentQueue<MessageBuffer?> _writeQueue;

        public static Tuple<IMessageStream, IMessageStream> CreatePair()
        {
            var sem1 = new SemaphoreSlim(0);
            var sem2 = new SemaphoreSlim(0);
            var queue1 = new ConcurrentQueue<MessageBuffer?>();
            var queue2 = new ConcurrentQueue<MessageBuffer?>();
            return Tuple.Create<IMessageStream, IMessageStream>(
                new PairedMessageStream(queue1, queue2, sem1, sem2),
                new PairedMessageStream(queue2, queue1, sem2, sem1)
            );
        }

        private PairedMessageStream(ConcurrentQueue<MessageBuffer?> readQueue, ConcurrentQueue<MessageBuffer?> writeQueue,
                                    SemaphoreSlim readSemaphore, SemaphoreSlim writeSemaphore)
        {
            _readSemaphore = readSemaphore;
            _writeSemaphore = writeSemaphore;
            _writeQueue = writeQueue;
            _readQueue = readQueue;
        }

        public async void ReceiveMessages<T>(IMessageStream.MessageReceivedHandler<T> handler, T state)
        {
            MessagePool pool = new();
            try
            {
                while (true)
                {
                    await _readSemaphore.WaitAsync();
                    if (_readQueue.TryDequeue(out MessageBuffer? messageBuffer))
                    {
                        if (messageBuffer is null)
                        {
                            throw new IOException("Connection closed by peer");
                        }
                        ReadOnlySequence<byte> data = messageBuffer.AsReadOnlySequence();
                        Message? message = Message.TryReadMessage(pool, ref data, messageBuffer.Handles);
                        if (message is null)
                        {
                            throw new ProtocolException("Cannot parse message.");
                        }
                        if (data.Length != 0)
                        {
                            throw new ProtocolException("Message buffer contains more than one message.");
                        }
                        handler(closeReason: null, message, state);
                    }
                }
            }
            catch (Exception ex)
            {
                handler(closeReason: ex, null!, state);
            }
        }

        public ValueTask<bool> TrySendMessageAsync(MessageBuffer message)
        {
            _writeQueue.Enqueue(message);
            _writeSemaphore.Release();
            return ValueTask.FromResult(true);
        }

        public void Close(Exception closeReason)
        {
            TrySendMessageAsync(null!); // Use null as EOF.
        }
    }
}
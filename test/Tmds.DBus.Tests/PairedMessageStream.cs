using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Tests
{
    class PairedMessageStream : IMessageStream
    {
        SemaphoreSlim _readSemaphore;
        ConcurrentQueue<Message> _readQueue;
        SemaphoreSlim _writeSemaphore;
        ConcurrentQueue<Message> _writeQueue;
        
        public static Tuple<IMessageStream, IMessageStream> CreatePair()
        {
            var sem1 = new SemaphoreSlim(0);
            var sem2 = new SemaphoreSlim(0);
            var queue1 = new ConcurrentQueue<Message>();
            var queue2 = new ConcurrentQueue<Message>();
            return Tuple.Create<IMessageStream, IMessageStream>(
                new PairedMessageStream(queue1, queue2, sem1, sem2),
                new PairedMessageStream(queue2, queue1, sem2, sem1)
            );
        }
        
        private PairedMessageStream(ConcurrentQueue<Message> readQueue, ConcurrentQueue<Message> writeQueue,
                                    SemaphoreSlim readSemaphore, SemaphoreSlim writeSemaphore)
        {
            _readSemaphore = readSemaphore;
            _writeSemaphore = writeSemaphore;
            _writeQueue = writeQueue;
            _readQueue = readQueue;
        }
        
        public async Task<Message> ReceiveMessageAsync()
        {
            await _readSemaphore.WaitAsync();
            Message message;
            _readQueue.TryDequeue(out message);
            return message;
        }
        
        public Task SendMessageAsync(Message message)
        {
            _writeQueue.Enqueue(message);
            _writeSemaphore.Release();
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {}
    }
}
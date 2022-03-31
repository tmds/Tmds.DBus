namespace Tmds.DBus.Protocol;

public sealed class MessageBuffer : IDisposable
{
    private readonly MessagePool _messagePool;
    private readonly Sequence<byte> _sequence;
    private UnixFdCollection? _handles;

    internal int HandleCount => _handles?.Count ?? 0;

    internal uint Serial { get; set; }

    internal MessageFlags MessageFlags { get; set; }

    internal long Length => _sequence.Length;

    internal MessageBuffer(MessagePool messagePool, Sequence<byte> sequence)
    {
        _messagePool = messagePool;
        _sequence = sequence;
    }

    public void Dispose() => ReturnToPool();

    internal void ReturnToPool()
    {
        _sequence.Reset();
        _handles?.DisposeHandles();
        _handles = null; // TODO (perf): pool
        _messagePool.Return(this);
    }

    // APIs for writing
    internal IBufferWriter<byte> Writer => _sequence;

    internal Span<byte> GetSpan(int sizeHint) => _sequence.GetSpan(sizeHint);

    internal void Advance(int count) => _sequence.Advance(count);

    internal void AddHandle(SafeHandle handle)
    {
        if (_handles is null)
        {
            _handles = new(isRawHandleCollection: false);
        }
        _handles.AddHandle(handle);
    }

    // APIs for reading
    internal ReadOnlySequence<byte> AsReadOnlySequence() => _sequence.AsReadOnlySequence;

    internal UnixFdCollection? Handles => _handles;

    internal Message GetMessage()
    {
        var sequence = AsReadOnlySequence();
        bool messageRead = Message.TryReadMessage(ref sequence, out Message message, Handles);
        Debug.Assert(messageRead);
        return message;
    }
}
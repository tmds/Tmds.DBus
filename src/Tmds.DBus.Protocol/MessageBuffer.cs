using System.Collections.ObjectModel;

namespace Tmds.DBus.Protocol;

public sealed class MessageBuffer
{
    private readonly MessagePool _messagePool;
    private readonly Sequence<byte> _sequence;
    private List<SafeHandle>? _handles;
    private ReadOnlyCollection<SafeHandle>? _readonlyCollection;

    internal int HandleCount => _handles?.Count ?? 0;

    internal uint Serial { get; set; }

    internal long Length => _sequence.Length;

    internal MessageBuffer(MessagePool messagePool, Sequence<byte> sequence)
    {
        _messagePool = messagePool;
        _sequence = sequence;
    }

    internal void ReturnToPool()
    {
        _sequence.Reset();
        _messagePool.Return(this);
        // TODO: dispose handles.
        // TODO: return to pool...
    }

    internal IBufferWriter<byte> Writer => _sequence;

    internal Span<byte> GetSpan(int sizeHint) => _sequence.GetSpan(sizeHint);

    internal void Advance(int count) => _sequence.Advance(count);

    internal ReadOnlySequence<byte> AsReadOnlySequence() => _sequence.AsReadOnlySequence;

    internal Message GetMessage()
    {
        var sequence = AsReadOnlySequence();
        bool messageRead = Message.TryReadMessage(ref sequence, out Message message, null);
        Debug.Assert(messageRead);
        return message;
    }

    internal IReadOnlyList<SafeHandle>? Handles =>
        _readonlyCollection ?? (_readonlyCollection = _handles?.AsReadOnly());
}
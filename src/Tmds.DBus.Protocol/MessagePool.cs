namespace Tmds.DBus.Protocol;

class MessagePool
{
    public static readonly MessagePool Shared = new MessagePool(Environment.ProcessorCount * 2);

    private const int MinimumSpanLength = 512;

    private readonly int _maxSize;
    private readonly Stack<MessageBuffer> _pool = new Stack<MessageBuffer>();
    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create(80 * 1024, 100);

    internal MessagePool(int maxSize)
    {
        _maxSize = maxSize;
    }

    public MessageBuffer Rent()
    {
        lock (_pool)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
        }

        var sequence = new Sequence<byte>(_arrayPool) { MinimumSpanLength = MinimumSpanLength };

        return new MessageBuffer(this, sequence);
    }

    internal void Return(MessageBuffer value)
    {
        lock (_pool)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Push(value);
            }
        }
    }
}

#if !NET9_0_OR_GREATER

namespace Tmds.DBus.Protocol;

// Polyfill for System.Threading.Lock for use with the `lock` keyword.
sealed class Lock
{
    public bool IsHeldByCurrentThread => Monitor.IsEntered(this);
}

#endif

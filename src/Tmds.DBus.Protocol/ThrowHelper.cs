using System.Diagnostics.CodeAnalysis;

static class ThrowHelper
{
    public static void ThrowIfDisposed(bool condition, object instance)
    {
        if (condition)
        {
            ThrowObjectDisposedException(instance);
        }
    }

    private static void ThrowObjectDisposedException(object instance)
    {
        throw new ObjectDisposedException(instance?.GetType().FullName);
    }

    public static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    public static void ThrowNotSupportedException()
    {
        throw new NotSupportedException();
    }
}
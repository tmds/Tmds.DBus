using System.Collections;

namespace Tmds.DBus.Protocol;

sealed class UnixFdCollection : IReadOnlyList<SafeHandle>, IDisposable
{
    private readonly List<(IntPtr RawHandle, SafeHandle? Handle)>? _handles;
    private readonly List<(IntPtr RawHandle, bool OwnsHandle)>? _rawHandles;

    internal bool IsRawHandleCollection => _rawHandles is not null;

    internal UnixFdCollection(bool isRawHandleCollection = true)
    {
        if (isRawHandleCollection)
        {
            _rawHandles = new();
        }
        else
        {
            _handles = new();
        }
    }

    internal void AddHandle(IntPtr handle) => _rawHandles!.Add((handle, true));

    internal void AddHandle(SafeHandle handle) => _handles!.Add((handle.DangerousGetHandle(), handle));

    public int Count => _rawHandles is not null ? _rawHandles.Count : _handles!.Count;

    public SafeHandle this[int index] => _handles![index].Handle!;

    public IntPtr DangerousGetHandle(int index)
    {
        if (_rawHandles is not null)
            return _rawHandles[index].RawHandle;

        return _handles![index].RawHandle;
    }

    public T? RemoveHandle<T>(int index) where T : SafeHandle
    {
        if (_rawHandles is not null)
        {
            (IntPtr rawHandle, bool ownsHandle) = _rawHandles[index];
            if (!ownsHandle)
            {
                return null;
            }
            _rawHandles[index] = (rawHandle, false);
            return (T)Activator.CreateInstance(typeof(T), new object[] { rawHandle, true });
        }
        else
        {
            (IntPtr rawHandle, SafeHandle? handle) = _handles![index];
            if (handle is null)
            {
                return null;
            }
            if (handle is not T)
            {
                throw new ArgumentException($"Requested handle type {typeof(T).FullName} does not matched stored type {handle.GetType().FullName}.");
            }
            _handles[index] = (rawHandle, null);
            return (T)handle;
        }
    }

    public IEnumerator<SafeHandle> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    public void DisposeHandles(int count = -1)
    {
        if (count != 0)
        {
            DisposeHandles(true, count);
        }
    }

    public void Dispose()
    {
        DisposeHandles(true);
    }

    ~UnixFdCollection()
    {
        DisposeHandles(false);
    }

    private void DisposeHandles(bool disposing, int count = -1)
    {
        if (count == -1)
        {
            count = Count;
        }

        if (disposing)
        {
            if (_handles is not null)
            {
                for (int i = 0; i < count; i++)
                {
                    var handle = _handles[i];
                    handle.Handle?.Dispose();
                }
                _handles.RemoveRange(0, count);
            }
        }
        if (_rawHandles is not null)
        {
            for (int i = 0; i < count; i++)
            {
                var handle = _rawHandles[i];
                if (handle.OwnsHandle)
                {
                    close(handle.RawHandle.ToInt32());
                }
            }
            _rawHandles.RemoveRange(0, count);
        }
    }

    [DllImport("libc")]
    private static extern void close(int fd);

    internal void MoveTo(UnixFdCollection handles, int count)
    {
        if (handles.IsRawHandleCollection != IsRawHandleCollection)
        {
            throw new ArgumentException("Handle collections are not compatible.");
        }
        if (handles.IsRawHandleCollection)
        {
            for (int i = 0; i < count; i++)
            {
                handles._rawHandles!.Add(_rawHandles![i]);
            }
            _rawHandles!.RemoveRange(0, count);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                handles._handles!.Add(_handles![i]);
            }
            _handles!.RemoveRange(0, count);
        }
    }
}
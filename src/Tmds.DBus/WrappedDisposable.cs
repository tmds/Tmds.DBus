// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    class WrappedDisposable : IDisposable
    {
        private object _gate = new object();
        private bool _disposed;
        private IDisposable _disposable;
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _disposable?.Dispose();
            }
        }
        public IDisposable Disposable
        {
            set
            {
                lock (_gate)
                {
                    if (_disposable != null)
                    {
                        throw new InvalidOperationException("Already set");
                    }
                    _disposable = value;
                    if (_disposed)
                    {
                        _disposable.Dispose();
                    }
                }
            }
        }
        public bool IsDisposed
        {
            get
            {
                lock (_gate) { return _disposed; }
            }
        }
    }
}

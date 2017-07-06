// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Runtime.InteropServices;

namespace Tmds.DBus
{
    public class CloseSafeHandle : SafeHandle
    {
        public CloseSafeHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(new IntPtr(-1), ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        public override bool IsInvalid
        {
            get { return handle == new IntPtr(-1); }
        }

        protected override bool ReleaseHandle()
        {
            return close(handle.ToInt32()) == 0;
        }

        [DllImport("libc", SetLastError = true)]
        internal static extern int close(int fd);
    }
}
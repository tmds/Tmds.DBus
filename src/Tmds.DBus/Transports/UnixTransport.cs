// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Transports
{
    using SizeT = System.UIntPtr;
    using SSizeT = System.IntPtr;
    internal class UnixTransport : Transport
    {
        private static readonly byte[] _oneByteArray = new[] { (byte)0 };
        private static bool s_bsdCredSupported = true;
        private static PropertyInfo s_safeHandleProperty;
        private unsafe struct msghdr
        {
            public IntPtr msg_name; //optional address
            public uint msg_namelen; //size of address
            public IOVector* msg_iov; //scatter/gather array
            public SizeT msg_iovlen; //# elements in msg_iov
            public IntPtr msg_control; //ancillary data, see below
            public SizeT msg_controllen; //ancillary data buffer len
            public int msg_flags; //flags on received message
        }

        unsafe struct IOVector
        {
            public IOVector(IntPtr bbase, int length)
            {
                this.Base = (void*)bbase;
                this.length = (SizeT)length;
            }

            //public IntPtr Base;
            public void* Base;

            public SizeT length;
            public int Length
            {
                get
                {
                    return (int)length;
                }
                set
                {
                    length = (SizeT)value;
                }
            }
        }

        private struct cmsghdr
        {
            public uint cmsg_len; //data byte count, including header
            public int cmsg_level; //originating protocol
            public int cmsg_type; //protocol-specific type
        }

        private unsafe struct cmsgcred
        {
            const int CMGROUP_MAX = 16;

            public int cmcred_pid; //PID of sending process
            public uint cmcred_uid; //real UID of sending process
            public uint cmcred_euid; //effective UID of sending process
            public uint cmcred_gid; //real GID of sending process
            public short cmcred_ngroups; //number or groups
            public fixed uint cmcred_groups[CMGROUP_MAX]; //groups
        }

        private struct cmsg
        {
            public cmsghdr hdr;
            public cmsgcred cred;
        }

        public UnixTransport(AddressEntry entry) :
            base(entry)
        {}

        protected override Task<Stream> OpenAsync (AddressEntry entry, CancellationToken cancellationToken)
        {
            string path;
            bool abstr;

            if (entry.Properties.TryGetValue("path", out path))
                abstr = false;
            else if (entry.Properties.TryGetValue("abstract", out path))
                abstr = true;
            else
                throw new ArgumentException("No path specified for UNIX transport");

            return OpenAsync(path, abstr, cancellationToken);
        }

        private async Task<Stream> OpenAsync(string path, bool abstr, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path");
            }

            if (abstr)
            {
                path = (char)'\0' + path;
            }

            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var registration = cancellationToken.Register(() => ((IDisposable)socket).Dispose());
            var endPoint = new Tmds.DBus.Transports.UnixDomainSocketEndPoint(path);
            try
            {
                await socket.ConnectAsync(endPoint);
                var stream = new NetworkStream(socket, true);
                try
                {
                    var bsdAuthenticated = TryWriteBsdCred(socket);
                    registration.Dispose();
                    if (!bsdAuthenticated)
                    {
                        await stream.WriteAsync(_oneByteArray, 0, 1, cancellationToken);
                    }
                    await DoSaslAuthenticationAsync(stream, cancellationToken);
                    return stream;
                }
                catch (Exception e)
                {
                    stream.Dispose();
                    throw new ConnectionException($"Unable to authenticate: {e.Message}", e);
                }
            }
            catch (System.Exception e)
            {
                socket.Dispose();
                throw new ConnectionException($"Socket error: {e.Message}", e);
            }
            finally
            {
                registration.Dispose();
            }
        }

        private static unsafe bool TryWriteBsdCred(Socket socket)
        {
            if (!s_bsdCredSupported)
            {
                return false;
            }
            byte buf = 0;

            IOVector iov = new IOVector ();
            iov.Base = &buf;
            iov.Length = 1;

            msghdr msg = new msghdr ();
            msg.msg_iov = &iov;
            msg.msg_iovlen = (SizeT)1;

            cmsg cm = new cmsg ();
            msg.msg_control = (IntPtr)(&cm);
            msg.msg_controllen = (SizeT)sizeof (cmsg);
            cm.hdr.cmsg_len = (uint)sizeof (cmsg);
            cm.hdr.cmsg_level = 0xffff; //SOL_SOCKET
            cm.hdr.cmsg_type = 0x03; //SCM_CREDS

            // Issue https://github.com/dotnet/corefx/issues/6807
            s_safeHandleProperty = s_safeHandleProperty ?? typeof(Socket).GetTypeInfo().GetDeclaredProperty("SafeHandle");
            var socketSafeHandle = (SafeHandle)s_safeHandleProperty.GetValue(socket, null);
            int sockFd = (int)socketSafeHandle.DangerousGetHandle();
            do
            {
                var rv = (int)Interop.sendmsg(sockFd, new IntPtr(&msg), 0);

                if (rv == 1)
                {
                    return true;
                }
                else
                {
                    var errno = Marshal.GetLastWin32Error();
                    switch (errno)
                    {
                        case 4:  // EINTR
                            continue;
                        case 22: // EINVAL
                            s_bsdCredSupported = false;
                            return false;
                        default:
                            throw new SocketException();
                    }
                }
            } while (true);
        }

    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tmds.DBus.Transports
{
    // this netstandard method is not available on mono 4.8
    internal class SocketUtils
    {
        public static Task ConnectAsync(Socket socket, EndPoint ep)
        {
            var tcs = new TaskCompletionSource<bool>();
            var args = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = ep,
                UserToken = tcs
            };
            try
            {
                args.Completed += OnConnectCompleted;
                if (!socket.ConnectAsync(args))
                {
                    OnConnectCompleted(null, args);
                }
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            return tcs.Task;
        }

        private static void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            var tcs = (TaskCompletionSource<bool>)e.UserToken;
            var errorCode = e.SocketError;
            if (errorCode == SocketError.Success)
            {
                tcs.SetResult(true);
            }
            else
            {
                tcs.SetException(new SocketException((int)errorCode));
            }
        }
    }
}
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class DBusObjectProxy : IDBusObject
    {
        private readonly Connection _connection;
        private readonly IProxyFactory _factory;
        public readonly string _serviceName;

        protected DBusObjectProxy(Connection connection, IProxyFactory factory, string serviceName, ObjectPath objectPath)
        {
            _connection = connection;
            _serviceName = serviceName;
            ObjectPath = objectPath;
            _factory = factory;
        }
        public ObjectPath ObjectPath { get; }

        internal protected async Task<IDisposable> WatchNonVoidSignalAsync<T>(string iface, string member, Action<Exception> error, Action<T> action, ReadMethodDelegate<T> readValue, bool isPropertiesChanged)
        {
            var synchronizationContext = _connection.CaptureSynchronizationContext();
            var wrappedDisposable = new WrappedDisposable(synchronizationContext);
            SignalHandler handler = (msg, ex) =>
            {
                if (ex != null)
                {
                    if (error == null)
                    {
                        return;
                    }
                    wrappedDisposable.Call(error, ex, disposes: true);
                    return;
                }

                if (!SenderMatches(msg))
                {
                    return;
                }
                var reader = new MessageReader(msg, _factory);
                if (isPropertiesChanged)
                {
                    var eventIface = reader.ReadString();
                    if (eventIface != iface)
                    {
                        return;
                    }
                    reader.SetSkipNextStructPadding();
                }
                var value = readValue(reader);
                wrappedDisposable.Call(action, value);
            };

            if (isPropertiesChanged)
            {
                wrappedDisposable.Disposable = await _connection.WatchSignalAsync(ObjectPath, "org.freedesktop.DBus.Properties", "PropertiesChanged", handler).ConfigureAwait(false);
            }
            else
            {
                wrappedDisposable.Disposable = await _connection.WatchSignalAsync(ObjectPath, iface, member, handler).ConfigureAwait(false);
            }

            return wrappedDisposable;
        }

        internal protected async Task<IDisposable> WatchVoidSignalAsync(string iface, string member, Action<Exception> error, Action action)
        {
            var synchronizationContext = _connection.CaptureSynchronizationContext();
            var wrappedDisposable = new WrappedDisposable(synchronizationContext);
            SignalHandler handler = (msg, ex) =>
            {
                if (ex != null)
                {
                    if (error == null)
                    {
                        return;
                    }
                    wrappedDisposable.Call(error, ex, disposes: true);
                    return;
                }

                if (!SenderMatches(msg))
                {
                    return;
                }
                wrappedDisposable.Call(action);
            };

            wrappedDisposable.Disposable = await _connection.WatchSignalAsync(ObjectPath, iface, member, handler).ConfigureAwait(false);

            return wrappedDisposable;
        }

        internal protected async Task<T> CallNonVoidMethodAsync<T>(string iface, string member, Signature? inSignature, MessageWriter writer, ReadMethodDelegate<T> readValue)
        {
            var reader = await SendMethodReturnReaderAsync(iface, member, inSignature, writer).ConfigureAwait(false);
            return readValue(reader);
        }

        internal protected async Task<T> CallGenericOutMethodAsync<T>(string iface, string member, Signature? inSignature, MessageWriter writer)
        {
            var reader = await SendMethodReturnReaderAsync(iface, member, inSignature, writer).ConfigureAwait(false);
            return reader.ReadVariantAsType<T>();
        }

        internal protected Task CallVoidMethodAsync(string iface, string member, Signature? inSigStr, MessageWriter writer)
        {
            return SendMethodReturnReaderAsync(iface, member, inSigStr, writer);
        }

        private async Task<MessageReader> SendMethodReturnReaderAsync(string iface, string member, Signature? inSignature, MessageWriter writer)
        {
            var callMessage = new Message(
                new Header(MessageType.MethodCall)
                {
                    Path = ObjectPath,
                    Interface = iface,
                    Member = member,
                    Destination = _serviceName,
                    Signature = inSignature
                },
                writer?.ToArray(),
                writer?.UnixFds
            );

            var reply = await _connection.CallMethodAsync(callMessage).ConfigureAwait(false);
            return new MessageReader(reply, _factory);
        }

        private bool SenderMatches(Message message)
        {
            return string.IsNullOrEmpty(message.Header.Sender) ||
                 string.IsNullOrEmpty(_serviceName) ||
                 (_serviceName[0] != ':' && message.Header.Sender[0] == ':') ||
                 _serviceName == message.Header.Sender;
        }
    }
}

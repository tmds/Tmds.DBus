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
        private readonly IDBusConnection _connection;
        private readonly IProxyFactory _factory;
        public readonly string _serviceName;

        protected DBusObjectProxy(IDBusConnection connection, IProxyFactory factory, string serviceName, ObjectPath objectPath)
        {
            _connection = connection;
            _serviceName = serviceName;
            ObjectPath = objectPath;
            _factory = factory;
        }
        public ObjectPath ObjectPath { get; }

        internal protected async Task<IDisposable> WatchNonVoidSignalAsync<T>(string iface, string member, Action<T> action, ReadMethodDelegate<T> readValue, bool isPropertiesChanged)
        {
            var wrappedDisposable = new WrappedDisposable();
            var synchronizationContext = SynchronizationContext.Current;
            SignalHandler handler = msg =>
            {
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
                if (synchronizationContext != null)
                {
                    synchronizationContext.Post(o =>
                    {
                        if (!wrappedDisposable.IsDisposed)
                        {
                            action(value);
                        }
                    }, null);
                }
                else
                {
                    if (!wrappedDisposable.IsDisposed)
                    {
                        action(value);
                    }
                }
            };

            if (isPropertiesChanged)
            {
                wrappedDisposable.Disposable = await _connection.WatchSignalAsync(ObjectPath, "org.freedesktop.DBus.Properties", "PropertiesChanged", handler);
            }
            else
            {
                wrappedDisposable.Disposable = await _connection.WatchSignalAsync(ObjectPath, iface, member, handler);
            }

            return wrappedDisposable;
        }

        internal protected async Task<IDisposable> WatchVoidSignalAsync<T>(string iface, string member, Action action)
        {
            var wrappedDisposable = new WrappedDisposable();
            var synchronizationContext = SynchronizationContext.Current;
            SignalHandler handler = msg =>
            {
                if (!SenderMatches(msg))
                {
                    return;
                }
                if (synchronizationContext != null)
                {
                    synchronizationContext.Post(o =>
                    {
                        if (!wrappedDisposable.IsDisposed)
                        {
                            action();
                        }
                    }, null);
                }
                else
                {
                    if (!wrappedDisposable.IsDisposed)
                    {
                        action();
                    }
                }
            };

            wrappedDisposable.Disposable = await _connection.WatchSignalAsync(ObjectPath, iface, member, handler);

            return wrappedDisposable;
        }

        internal protected async Task<T> CallNonVoidMethodAsync<T>(string iface, string member, Signature? inSignature, MessageWriter writer, ReadMethodDelegate<T> readValue)
        {
            var reader = await SendMethodReturnReaderAsync(iface, member, inSignature, writer);
            return readValue(reader);
        }

        internal protected async Task<T> CallGenericOutMethodAsync<T>(string iface, string member, Signature? inSignature, MessageWriter writer, ReadMethodDelegate<object> readValue)
        {
            var reader = await SendMethodReturnReaderAsync(iface, member, inSignature, writer);
            return (T)readValue(reader);
        }

        internal protected Task CallVoidMethodAsync(string iface, string member, Signature? inSigStr, MessageWriter writer)
        {
            return SendMethodReturnReaderAsync(iface, member, inSigStr, writer);
        }

        private async Task<MessageReader> SendMethodReturnReaderAsync(string iface, string member, Signature? inSignature, MessageWriter writer)
        {
            var callMessage = new Message()
            {
                Header = new Header(MessageType.MethodCall)
                {
                    Path = ObjectPath,
                    Interface = iface,
                    Member = member,
                    Destination = _serviceName,
                    Signature = inSignature
                },
                Body = writer?.ToArray()
            };

            var reply = await _connection.CallMethodAsync(callMessage);
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

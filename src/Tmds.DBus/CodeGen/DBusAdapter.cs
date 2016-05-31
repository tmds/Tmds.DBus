// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class DBusAdapter
    {
        internal delegate Task<Message> MethodCallHandler(object o, Message methodCall, IProxyFactory factory);

        private enum State
        {
            Registering,
            Registered,
            Unregistered
        }

        private readonly object _gate = new object();
        private readonly IDBusConnection _connection;
        private readonly IProxyFactory _factory;
        private readonly ObjectPath _objectPath;
        private readonly SynchronizationContext _synchronizationContext;
        protected internal readonly Dictionary<string, MethodCallHandler> _methodHandlers;
        protected internal readonly object _object;

        private State _state;
        private List<IDisposable> _signalWatchers;

        protected DBusAdapter(IDBusConnection connection, ObjectPath objectPath, object o, IProxyFactory factory, SynchronizationContext synchronizationContext)
        {
            _connection = connection;
            _objectPath = objectPath;
            _object = o;
            _state = State.Registering;
            _factory = factory;
            _synchronizationContext = synchronizationContext;
            _methodHandlers = new Dictionary<string, MethodCallHandler>();
        }

        public void Unregister()
        {
            lock (_gate)
            {
                if (_state == State.Unregistered)
                {
                    return;
                }
                _state = State.Unregistered;
                if (_signalWatchers != null)
                {
                    foreach (var disposable in _signalWatchers)
                    {
                        disposable.Dispose();
                    }
                    _signalWatchers = null;
                }
            }
        }

        public void CompleteRegistration()
        {
            lock (_gate)
            {
                if (_state == State.Registering)
                {
                    _state = State.Registered;
                }
                else if (_state == State.Unregistered)
                {
                    throw new InvalidOperationException("The object has been unregistered");
                }
                else if (_state == State.Registered)
                {
                    throw new InvalidOperationException("The object has already been registered");
                }
            }
        }

        public async Task WatchSignalsAsync(CancellationToken cancellationToken)
        {
            var tasks = StartWatchingSignals(cancellationToken);
            IEnumerable<IDisposable> signalDisposables = null;

            try
            {
                Task.WaitAll(tasks, cancellationToken);
                signalDisposables = tasks.Select(task => task.Result);

                if (signalDisposables.Contains(null))
                {
                    throw new InvalidOperationException("One or more Watch-methods returned a null IDisposable");
                }
            }
            catch
            {
                foreach (var task in tasks)
                {
                    try
                    {
                        var disposable = await task;
                        disposable?.Dispose();
                    }
                    finally
                    { }
                }
                throw;
            }

            lock (_gate)
            {
                if (_state == State.Registering)
                {
                    _signalWatchers = new List<IDisposable>();
                    _signalWatchers.AddRange(signalDisposables);
                }
                else if (_state == State.Unregistered)
                {
                    foreach (var disposable in signalDisposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        static protected internal string GetMethodLookupKey(string iface, string member, Signature? signature)
        {
            return $"{iface}.{member}.{signature?.Value}";
        }

        static protected internal string GetPropertyLookupKey(string iface, string member, Signature? signature)
        {
            return $"org.freedesktop.DBus.Properties.{iface}.{member}.{signature?.Value}";
        }

        static protected internal string GetPropertyAddKey(string iface, string member, Signature? signature)
        {
            return $"org.freedesktop.DBus.Properties.{iface}.{member}.s{signature?.Value}";
        }

        public Task<Message> HandleMethodCall(Message methodCall, CancellationToken cancellationToken)
        {
            var key = GetMethodLookupKey(methodCall.Header.Interface, methodCall.Header.Member, methodCall.Header.Signature);
            MethodCallHandler handler = null;
            if (!_methodHandlers.TryGetValue(key, out handler))
            {
                if (methodCall.Header.Interface == "org.freedesktop.DBus.Properties")
                {
                    MessageReader reader = new MessageReader(methodCall, null);
                    var interf = reader.ReadString();
                    key = GetPropertyLookupKey(interf, methodCall.Header.Member, methodCall.Header.Signature);
                    _methodHandlers.TryGetValue(key, out handler);
                }
            }
            if (handler != null)
            {
                if (_synchronizationContext == null)
                {
                    return handler(_object, methodCall, _factory);
                }
                else
                {
                    var tcs = new TaskCompletionSource<Message>();
                    _synchronizationContext.Post(async _ => {
                        try
                        {
                            var reply = await handler(_object, methodCall, _factory);
                            tcs.SetResult(reply);
                        }
                        catch (Exception e)
                        {
                            tcs.SetException(e);
                        }
                    }, null);
                    return tcs.Task;
                }
            }
            else
            {
                var errorMessage = String.Format("Method \"{0}\" with signature \"{1}\" on interface \"{2}\" doesn't exist",
                                               methodCall.Header.Member,
                                               methodCall.Header.Signature?.Value,
                                               methodCall.Header.Interface);

                var replyMessage = MessageHelper.ConstructErrorReply(methodCall, "org.freedesktop.DBus.Error.UnknownMethod", errorMessage);

                return Task.FromResult(replyMessage);
            }
        }

        protected internal virtual Task<IDisposable>[] StartWatchingSignals(CancellationToken cancellationToken)
        {
            return Array.Empty<Task<IDisposable>>();
        }

        protected internal void EmitVoidSignal(string interfaceName, string signalName)
        {
            EmitNonVoidSignal(interfaceName, signalName, null, null);
        }

        protected internal void EmitNonVoidSignal(string iface, string member, Signature? inSigStr, MessageWriter writer)
        {
            if (!IsRegistered)
            {
                return;
            }

            Message signalMsg = new Message()
            {
                Header = new Header(MessageType.Signal)
                {
                    Path = _objectPath,
                    Interface = iface,
                    Member = member,
                    Signature = inSigStr
                },
                Body = writer.ToArray()
            };

            _connection.EmitSignal(signalMsg);
        }

        protected internal async Task<Message> CreateNonVoidReply<T>(Message methodCall, Task<T> resultTask, Action<MessageWriter, T> writeResult, Signature? outSignature)
        {
            uint serial = methodCall.Header.Serial;

            T result = await resultTask;
            MessageWriter retWriter = new MessageWriter();
            writeResult(retWriter, result);

            Message replyMsg = new Message()
            {
                Header = new Header(MessageType.MethodReturn)
                {
                    Signature = outSignature
                },
                Body = retWriter.ToArray()
            };
            return replyMsg;
        }

        protected internal async Task<Message> CreateVoidReply(Message methodCall, Task task)
        {
            uint serial = methodCall.Header.Serial;
            await task;
            var replyMsg = new Message()
            {
                Header = new Header(MessageType.MethodReturn)
            };
            return replyMsg;
        }

        private bool IsRegistered
        {
            get
            {
                lock (_gate)
                {
                    return _state == State.Registered;
                }
            }
        }
    }
}

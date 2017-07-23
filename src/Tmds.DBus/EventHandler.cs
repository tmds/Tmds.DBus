using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    public static class EventHandler
    {
        private class Disposable : IDisposable
        {
            public Disposable(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }
            public void Dispose()
            {
                _disposeAction();
            }
            private Action _disposeAction;
        }

        private static IDisposable Add(object o, string eventName, object handler)
        {
            var eventInfo = o.GetType().GetEvent(eventName);
            var addMethod = eventInfo.GetAddMethod();
            var removeMethod = eventInfo.GetRemoveMethod();
            addMethod.Invoke(o, new object[] { handler });
            Action disposeAction = () => removeMethod.Invoke(o, new object[] { handler });
            return new Disposable(disposeAction);
        }

        public static Task<IDisposable> AddAsync<T>(object o, string eventName, Action<T> handler)
        {
            return Task.FromResult(Add(o, eventName, handler));
        }

        public static Task<IDisposable> AddAsync(object o, string eventName, Action handler)
        {
            return Task.FromResult(Add(o, eventName, handler));
        }
    }
}
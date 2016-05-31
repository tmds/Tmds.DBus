using System.Threading;

namespace Tmds.DBus.Tests
{
    class ObservableAction
    {
        private int _counter;
        public int NumberOfCalls => Volatile.Read(ref _counter);
        public void Action()
        {
            Interlocked.Increment(ref _counter);
        }
    }
}
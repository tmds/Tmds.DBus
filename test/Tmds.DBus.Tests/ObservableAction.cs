using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Tests
{
    class ObservableAction
    {
        private int _counter;
        private int NumberOfCalls => Volatile.Read(ref _counter);
        
        public void Action()
        {
            Interlocked.Increment(ref _counter);
        }

        public async Task AssertNumberOfCallsAsync(int expected)
        {
            // wait at least 1 second
            for (int i = 0; i < 1000; i++)
            {
                if (NumberOfCalls == expected)
                {
                    break;
                }
                await Task.Delay(1);
            }
            Assert.Equal(expected, NumberOfCalls);
        }
    }
}
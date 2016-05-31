using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    class StringOperations : IStringOperations
    {
        public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/stringoperations");
        public Task<string> ConcatAsync(string s1, string s2, CancellationToken cancellationToken)
        {
            return Task.FromResult($"{s1}{s2}");
        }

        public ObjectPath ObjectPath { get { return Path; } }
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    class StringOperations : IStringOperations
    {
        public static readonly ObjectPath Path = new ObjectPath("/tmds/dbus/tests/stringoperations");
        private ObjectPath _path;
        public StringOperations()
        {
            _path = Path;
        }
        public StringOperations(ObjectPath path)
        {
            _path = path;
        }
        public Task<string> ConcatAsync(string s1, string s2)
        {
            return Task.FromResult($"{s1}{s2}");
        }

        public ObjectPath ObjectPath { get { return _path; } }
    }
}
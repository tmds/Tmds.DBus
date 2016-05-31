using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus.Tests
{
    [DBusInterface("tmds.dbus.tests.StringOperations")]
    public interface IStringOperations : IDBusObject
    {
        Task<string> ConcatAsync(string s1, string s2, CancellationToken cancellationToken);
    }
}
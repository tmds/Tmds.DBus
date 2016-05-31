// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    [DBusInterface(DBusConnection.DBusInterface)]
    public interface IDBus : IDBusObject
    {
        Task<string[]> ListActivatableNamesAsync(CancellationToken cancellationToken);
		Task<bool> NameHasOwnerAsync(string name, CancellationToken cancellationToken);
        Task<ServiceStartResult> StartServiceByNameAsync(string name, uint flags, CancellationToken cancellationToken);
        Task<string> GetNameOwnerAsync(string name, CancellationToken cancellationToken);
        Task<string[]> ListNamesAsync(CancellationToken cancellationToken);
    }
}

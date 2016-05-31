using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace NetworkManager
{
    [DBusInterface("org.freedesktop.NetworkManager")]
    public interface INetworkManager : IDBusObject
    {
        Task<ObjectPath[]> GetDevicesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<IDictionary<string, object>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));

        Task<object> GetAsync(string prop, CancellationToken cancellationToken = default(CancellationToken));
        Task SetAsync(string prop, object val, CancellationToken cancellationToken = default(CancellationToken));
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("NetworkManager Sample");
            Task.Run(async () =>
            {
                using (var connection = new Connection(Address.System))
                {
                    await connection.ConnectAsync();
                    var objectPath = new ObjectPath("/org/freedesktop/NetworkManager");
                    var service = "org.freedesktop.NetworkManager";
                    var networkManager = connection.CreateProxy<INetworkManager>(service, objectPath);
                    Console.WriteLine("Devices:");

                    var devices = await networkManager.GetDevicesAsync();
                    foreach (var device in devices)
                    {
                        Console.WriteLine($"* {device}");
                    }
                    Console.WriteLine("Properties:");
                    var properties = await networkManager.GetAllAsync();
                    foreach (var prop in properties)
                    {
                        Console.WriteLine($"* {prop.Key}={prop.Value}");
                    }
                }
            }).Wait();
        }
    }
}
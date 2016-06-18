using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace NetworkManager
{
    public enum NetworkManagerState : uint
    {
        Unknown = 0,
        ASleep = 10,
        Disconnected = 20,
        Disconnecting = 30,
        Connecting = 40,
        ConnectedLocal = 50,
        ConnectedSite = 60,
        ConnectedGlobal = 70
    }
    public enum NetworkManagerConnectivity : uint
    {
        Unknown = 0,
        None = 1,
        Portal = 2,
        Limited = 3,
        Full = 4
    }

    [Dictionary]
    public class NetworkManagerProperties : IEnumerable<KeyValuePair<string,object>>
    {
        public bool NetworkingEnabled;
        public bool WirelessEnabled;
        public ObjectPath[] ActiveConnections;
        public string Version;
        public NetworkManagerState State;
        public NetworkManagerConnectivity Connectivity;

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string,object>> GetEnumerator()
        {
            yield return new KeyValuePair<string, object>(nameof(NetworkingEnabled), NetworkingEnabled);
            yield return new KeyValuePair<string, object>(nameof(WirelessEnabled), WirelessEnabled);
            yield return new KeyValuePair<string, object>(nameof(ActiveConnections), ActiveConnections);
            yield return new KeyValuePair<string, object>(nameof(Version), Version);
            yield return new KeyValuePair<string, object>(nameof(State), State);
            yield return new KeyValuePair<string, object>(nameof(Connectivity), Connectivity);
        }
    }

    [DBusInterface("org.freedesktop.NetworkManager")]
    public interface INetworkManager : IDBusObject
    {
        Task<ObjectPath[]> GetDevicesAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task<NetworkManagerProperties> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));

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
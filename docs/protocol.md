# Tmds.DBus.Protocol

The `Tmds.DBus.Protocol` packages provides a D-Bus protocol API.

The following example shows how you can use it to expose and consume a D-Bus service that adds two integers.

Create a console application:

```
dotnet new console -o example
cd example
```

Add a `NuGet.Config` file
```
dotnet new nugetconfig
```

Add the `tmds` feed into the file:
```xml
<add key="tmds" value="https://www.myget.org/F/tmds/api/v3/index.json" />
```

Now add the `Tmds.DBus.Protocol` package:
```
dotnet add package --prerelease Tmds.DBus.Protocol
```

Update `Program.cs`:
```cs
using Tmds.DBus.Protocol;

class Program
{
    static async Task Main()
    {
        string peerName = await StartAddServiceAsync();

        var connection = Connection.Session;

        var addProxy = new AddProxy(connection, peerName);

        int i = 10;
        int j = 20;
        int sum = await addProxy.AddAsync(i, j);

        Console.WriteLine($"The sum of {i} and {j} is {sum}.");
    }

    private async static Task<string> StartAddServiceAsync()
    {
        var connection = new Connection(Address.Session!);

        await connection.ConnectAsync();

        connection.AddMethodHandler(new AddImplementation());

        return connection.UniqueName ?? "";
    }
}

class AddProxy
{
    private const string Interface = "org.example.Adder";
    private const string Path = "/org/example/Adder";

    private readonly Connection _connection;
    private readonly string _peer;

    public AddProxy(Connection connection, string peer)
    {
        _connection = connection;
        _peer = peer;
    }

    public Task<int> AddAsync(int i, int j)
    {
        return _connection.CallMethodAsync(
            CreateAddMessage(),
            (Message message, object? state) =>
            {
                return message.GetBodyReader().ReadInt32();
            });

        MessageBuffer CreateAddMessage()
        {
            using var writer = _connection.GetMessageWriter();

            writer.WriteMethodCallHeader(
                destination: _peer,
                path: Path,
                @interface: Interface,
                signature: "ii",
                member: "Add");

            writer.WriteInt32(i);
            writer.WriteInt32(j);

            return writer.CreateMessage();
        }
    }
}

class AddImplementation : IMethodHandler
{
    public string Path => "/org/example/Adder";

    public async ValueTask<bool> TryHandleMethodAsync(Connection connection, Message message)
    {
        switch ((message.Member, message.Signature))
        {
            case ("Add", "ii"):
                Add(connection, message);
                return true;
        }

        return false;
    }

    bool RunMethodHandlerSynchronously(Message message) => true;

    private void Add(Connection connection, Message message)
    {
        var reader = message.GetBodyReader();

        int i = reader.ReadInt32();
        int j = reader.ReadInt32();

        int sum = i + j;

        connection.TrySendMessage(CreateResponseMessage(connection, message, sum));

        static MessageBuffer CreateResponseMessage(Connection connection, Message message, int sum)
        {
            using var writer = connection.GetMessageWriter();

            writer.WriteMethodReturnHeader(
                replySerial: message.Serial,
                destination: message.Sender,
                signature: "i"
            );

            writer.WriteInt32(sum);

            return writer.CreateMessage();
        }
    }
}
```

Now run the example:
```
$ dotnet run
The sum of 10 and 20 is 30.
```

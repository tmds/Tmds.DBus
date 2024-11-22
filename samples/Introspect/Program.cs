using System;
using Tmds.DBus.Protocol;

if (args.Length != 3)
{
    Console.WriteLine("Usage: --session/--system <servicename> <objectpath>");
    return -1;
}
bool sessionNotSystem = args[0] != "--system";
var service = args[1];
var objectPath = args[2];

using var connection = new Connection(sessionNotSystem ? Address.Session! : Address.System!);
await connection.ConnectAsync();

var xml = await connection.CallMethodAsync(CreateMessage(objectPath), (Message m, object? s) => m.GetBodyReader().ReadString(), null);
MessageBuffer CreateMessage(string path)
{
    using var writer = connection.GetMessageWriter();
    writer.WriteMethodCallHeader(
        destination: service,
        path: path,
        @interface: "org.freedesktop.DBus.Introspectable",
        member: "Introspect");
    return writer.CreateMessage();
}

Console.WriteLine(xml);
return 0;

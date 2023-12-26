using Tmds.DBus.Protocol;

string address = Address.Session ?? throw new ArgumentNullException(nameof(address));

await foreach (DisposableMessage dmsg in Connection.MonitorBusAsync(address))
{
    using var _ = dmsg;
    Message msg = dmsg.Message;

    Console.WriteLine($"{msg.MessageType} {msg.SenderAsString} -> {msg.DestinationAsString}");
}

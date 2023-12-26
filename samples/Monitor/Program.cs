using Tmds.DBus.Protocol;
using System.Threading.Channels;

string address = Address.Session ?? throw new ArgumentNullException(nameof(address));

await foreach (var dmsg in MonitorMessagesAsync(address))
{
    using var _ = dmsg;
    Message msg = dmsg.Message;

    Console.WriteLine($"{msg.MessageType} {msg.SenderAsString} -> {msg.DestinationAsString}");
}

IAsyncEnumerable<DisposableMessage> MonitorMessagesAsync(string address)
{
    var channel = Channel.CreateUnbounded<DisposableMessage>();

    WriteMessagesToChannel(address, channel.Writer);

    return channel.Reader.ReadAllAsync();

    static async void WriteMessagesToChannel(string address, ChannelWriter<DisposableMessage> writer)
    {
        try
        {
            using var connection = new Connection(address);
            await connection.ConnectAsync();

            await connection.BecomeMonitorAsync(
                (Exception? ex, DisposableMessage message) =>
                {
                    if (ex is not null)
                    {
                        writer.TryComplete(ex);
                        return;
                    }

                    if (!writer.TryWrite(message))
                    {
                        message.Dispose();
                    }
                }
            );

            Exception? ex = await connection.DisconnectedAsync();
            writer.TryComplete(ex);
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
    }
}
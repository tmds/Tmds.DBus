namespace Tmds.DBus.Protocol;

public interface IMethodHandler
{
    // Path that is handled by this method handler.
    string Path { get; }

    // The message argument is only valid during the call. It must not be stored to extend its lifetime.
    ValueTask HandleMethodAsync(MethodContext context);

    // Controls whether to wait for the handler method to finish executing before reading more messages.
    bool RunMethodHandlerSynchronously(Message message);
}

public class MethodContext
{
    internal MethodContext(Connection connection, Message request)
    {
        Connection = connection;
        Request = request;
    }

    public Message Request { get; }
    public Connection Connection { get; }

    public bool ReplySent { get; private set; }

    public bool NoReplyExpected => (Request.MessageFlags & MessageFlags.NoReplyExpected) != 0;

    public MessageWriter CreateReplyWriter(string signature)
    {
        var writer = Connection.GetMessageWriter();
        writer.WriteMethodReturnHeader(
            replySerial: Request.Serial,
            destination: Request.Sender,
            signature: signature
        );
        return writer;
    }

    public void Reply(MessageBuffer message)
    {
        if (ReplySent || NoReplyExpected)
        {
            message.Dispose();
            if (ReplySent)
            {
                throw new InvalidOperationException("A reply has already been sent.");
            }
        }

        ReplySent = true;
        Connection.TrySendMessage(message);
    }

    public void ReplyError(string? errorName = null,
                           string? errorMsg = null)
    {
        using var writer = Connection.GetMessageWriter();
        writer.WriteError(
            replySerial: Request.Serial,
            destination: Request.Sender,
            errorName: errorName,
            errorMsg: errorMsg
        );
        Reply(writer.CreateMessage());
    }
}
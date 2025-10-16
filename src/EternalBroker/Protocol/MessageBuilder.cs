namespace Broker.Protocol;

public class MessageBuilder
{
    public MessageType MessageType { get; set; }

    public Message? Message { get; set; }

    public ReadOnlyMemory<byte> Build()
    {
        int messageLength = Message?.Payload.Length ?? 0;

        byte[] serializedMessage = new byte[messageLength + 8];
        BitConverter.GetBytes((int)MessageType).CopyTo(serializedMessage, 0);
        BitConverter.GetBytes(messageLength).CopyTo(serializedMessage, 4);

        if (Message is { Payload.Length: > 0 })
        {
            Message.Payload.CopyTo(serializedMessage.AsMemory(8));
        }

        return serializedMessage.AsMemory();
    }
}
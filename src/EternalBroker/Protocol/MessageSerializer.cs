namespace Broker.Protocol;

public class MessageSerializer
{
    public ReadOnlyMemory<byte> Serialize(ProtocolMessage message)
    {
        byte[] serializedMessage = new byte[message.Payload.Length + 12];
        BitConverter.GetBytes((int)message.MessageType).CopyTo(serializedMessage, 0);
        BitConverter.GetBytes(message.Payload.Length).CopyTo(serializedMessage, 4);
        BitConverter.GetBytes(message.TopicCode).CopyTo(serializedMessage, 8);

        if (message.Payload is { Length: > 0 })
        {
            message.Payload.CopyTo(serializedMessage.AsMemory(12));
        }

        return serializedMessage.AsMemory();
    }
}
namespace Broker.Protocol;

public class MessageSerializer
{
    public ReadOnlyMemory<byte> Serialize(ProtocolMessage message)
    {
        byte[] serializedMessage = new byte[message.Payload.Length + 8];
        BitConverter.GetBytes((int)message.MessageType).CopyTo(serializedMessage, 0);
        BitConverter.GetBytes(message.Payload.Length).CopyTo(serializedMessage, 4);

        if (message.Payload is { Length: > 0 })
        {
            message.Payload.CopyTo(serializedMessage.AsMemory(8));
        }

        return serializedMessage.AsMemory();
    }
}
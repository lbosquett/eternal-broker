using System.Net.Sockets;

namespace Broker.Protocol;

public class ApplicationMessageSender(Socket socket)
{
    private readonly MessageSerializer _serializer = new();

    public async Task SendMessageAsync(ProtocolMessage message, CancellationToken token)
    {
        ReadOnlyMemory<byte> serialized = _serializer.Serialize(message);
        await socket.SendAsync(serialized, token);
    }
}
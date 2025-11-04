using System.Threading.Channels;
using Broker.Protocol;

namespace Broker.Server;

public class ServerApplicationMessageListener(Guid clientKey, Channel<ProtocolMessage> channel) : IMessageListener
{
    public bool TryReceiveMessage(ProtocolMessage message)
    {
        return channel.Writer.TryWrite(message);
    }
}
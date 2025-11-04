namespace Broker.Protocol;

public interface IMessageListener
{
    public bool TryReceiveMessage(ProtocolMessage message);
}
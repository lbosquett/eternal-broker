namespace Broker.Protocol;

public interface IMessageListener
{
    public Task ReceiveMessageAsync(ProtocolMessage message, CancellationToken cancellationToken);
}
using Broker.Protocol;

namespace Broker.Server;

internal class ServerApplicationMessageListener(ClientContext clientContext) : IMessageListener
{
    private readonly ApiHandler _apiHandler = new();

    public async Task ReceiveMessageAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        switch (message.MessageType)
        {
            case MessageType.Api:
                await _apiHandler.Handle(message, clientContext, cancellationToken);
                break;
            case MessageType.Publish:
                throw new NotImplementedException();
        }
    }
}
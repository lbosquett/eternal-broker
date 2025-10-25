using System.Text.Json;
using Broker.Protocol;
using Broker.Protocol.Api;

namespace Broker.Server;

public class ApiHandler
{
    public ApiHandler()
    {

    }

    public void Handle(ProtocolMessage protocolMessage)
    {
        if (protocolMessage.MessageType != MessageType.Api) throw new InvalidOperationException();

        JsonApiMessage? jsonApiMessage = JsonSerializer.Deserialize<JsonApiMessage>(protocolMessage.Payload.Span);
        if (jsonApiMessage == null) throw new InvalidOperationException();
    }
}
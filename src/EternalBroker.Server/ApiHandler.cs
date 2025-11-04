using System.Text.Json;
using Broker.Protocol;
using Broker.Protocol.Api;
using Broker.Serialization.Json;
using Broker.Server.Api;

namespace Broker.Server;

internal class ApiHandler
{
    private readonly TopicController _topicController = new();

    private ProtocolMessage BuildFromResponse(JsonApiMessageResponse response)
    {
        byte[] serializedResponse =
            JsonSerializer.SerializeToUtf8Bytes(response, JsonApiMessageContext.Default.JsonApiMessageResponse);
        return new ProtocolMessage(MessageType.Api, new ReadOnlyMemory<byte>(serializedResponse));
    }

    internal async Task Handle(ReceivedProtocolMessage receivedProtocolMessage, ClientHandler clientHandler, CancellationToken cancellationToken)
    {
        if (receivedProtocolMessage.MessageType != MessageType.Api) throw new InvalidOperationException();

        JsonApiMessageRequest? jsonApiMessage =
            JsonSerializer.Deserialize<JsonApiMessageRequest>(receivedProtocolMessage.Payload.Span,
                JsonApiMessageContext.Default.JsonApiMessageRequest);
        if (jsonApiMessage == null) throw new InvalidOperationException();

        switch (jsonApiMessage.Path)
        {
            case "/topics/list":
            {
                IEnumerable<Topic> topics = _topicController.ListTopics();
                var response = new JsonApiMessageResponse(true, topics);
                await clientHandler.SendMessageAsync(BuildFromResponse(response), cancellationToken);
            }
                break;
            case "/topics/create":
            {
                string topicName = ((JsonElement)jsonApiMessage.Parameters["topic"]).GetString() ?? throw new InvalidOperationException();
                _topicController.CreateTopic(topicName);

                var response = new JsonApiMessageResponse(true, null);
                await clientHandler.SendMessageAsync(BuildFromResponse(response), cancellationToken);
            }
                break;
        }
    }
}
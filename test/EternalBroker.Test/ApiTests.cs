using System.Net;
using System.Text.Json;
using Broker.Client;
using Broker.Protocol;
using Broker.Protocol.Api;
using Broker.Serialization.Json;
using Broker.Server;
using Xunit;

namespace EternalBroker.Test;

public class ApiTests
{
    [Fact]
    public async Task CreateTopic()
    {
        ProtocolMessage? received = null;
        MessageServer server = new();
        server.MessageReceived += message =>
        {
            received = message;
        };
        server.Run(new MessageServerOptions() { Port = Constants.TestPort }, CancellationToken.None);

        MessageClient client = new();
        await client.ConnectAsync(IPAddress.Loopback, Constants.TestPort, CancellationToken.None);
        await client.CreateTopicAsync("foo1", CancellationToken.None);

        await client.CloseConnectionAsync();
        await server.StopAsync();

        Assert.NotNull(received);

        JsonApiMessageRequest? deserialized = JsonSerializer.Deserialize<JsonApiMessageRequest>(received.Payload.Span,
            JsonApiMessageContext.Default.JsonApiMessageRequest);
        Assert.NotNull(deserialized);
    }
}
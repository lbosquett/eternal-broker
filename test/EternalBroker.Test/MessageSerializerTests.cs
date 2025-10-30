using Broker.Protocol;
using Xunit;

namespace EternalBroker.Test;

public class MessageSerializerTests
{
    [Fact]
    public void BuildPingMessage()
    {
        // setup
        MessageSerializer serializer = new MessageSerializer();

        // act
        ReadOnlyMemory<byte> result = serializer.Serialize(new ProtocolMessage(MessageType.Ping, ReadOnlyMemory<byte>.Empty));

        // assert
        Assert.False(result.IsEmpty);
        Assert.True(result is { Length: 8 });

        int messageTypeResult = BitConverter.ToInt32(result.Span.ToArray(), 0);
        int messageLengthResult = BitConverter.ToInt32(result.Span.ToArray(), 4);

        Assert.True(messageTypeResult == (int)MessageType.Ping);
        Assert.True(messageLengthResult == 0);
    }
}
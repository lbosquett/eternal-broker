using Broker.Protocol;
using Xunit;

namespace EternalBroker.Test;

public class MessageSerializerTests
{
    [Fact]
    public void BuildMessage()
    {
        // setup
        MessageSerializer serializer = new MessageSerializer();

        // act
        ReadOnlyMemory<byte> result = serializer.Serialize(new ProtocolMessage(MessageType.Publish, ReadOnlyMemory<byte>.Empty));

        // assert
        Assert.False(result.IsEmpty);
        Assert.Equal(8, result.Length);

        int messageTypeResult = BitConverter.ToInt32(result.Span.ToArray(), 0);
        int messageLengthResult = BitConverter.ToInt32(result.Span.ToArray(), 4);

        Assert.True(messageTypeResult == (int)MessageType.Publish);
        Assert.True(messageLengthResult == 0);
    }
}
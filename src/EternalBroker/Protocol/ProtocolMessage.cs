namespace Broker.Protocol;

public record ProtocolMessage(Guid Sender,
    MessageType MessageType,
    int TopicCode,
    ReadOnlyMemory<byte> Payload);

namespace Broker.Protocol;

public record ProtocolMessage(Guid sender,
    MessageType MessageType,
    int TopicCode,
    ReadOnlyMemory<byte> Payload);

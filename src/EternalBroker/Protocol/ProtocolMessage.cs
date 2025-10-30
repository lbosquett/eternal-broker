namespace Broker.Protocol;

public record ProtocolMessage(MessageType MessageType,
    ReadOnlyMemory<byte> Payload);
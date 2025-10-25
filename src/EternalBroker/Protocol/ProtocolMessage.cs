namespace Broker.Protocol;

public record ProtocolMessage(Guid Sender,
    MessageType MessageType,
    ReadOnlyMemory<byte> Payload);

namespace Broker.Protocol;

public record ReceivedProtocolMessage(Guid Sender,
    MessageType MessageType,
    ReadOnlyMemory<byte> Payload);

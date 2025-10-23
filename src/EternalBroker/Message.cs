namespace Broker;

public record Message(ReadOnlyMemory<byte> Payload);

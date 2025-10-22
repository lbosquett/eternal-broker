namespace Broker;

public record Message(string? Topic, ReadOnlyMemory<byte> Payload);

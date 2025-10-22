namespace Broker.Server;

public interface IMessageFactory
{
    Message Create(int topicCode, ReadOnlyMemory<byte> payload);
}
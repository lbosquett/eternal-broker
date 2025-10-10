namespace Broker.Protocol;

public class PublishMessage
{
    public string Topic { get; set; }

    public byte[] Content { get; set; }
}
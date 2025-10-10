namespace Broker.Server;

public class MessageServerOptions
{
    public int Port { get; init; }

    public static MessageServerOptions Default => new()
    {
        Port = 24242
    };
}
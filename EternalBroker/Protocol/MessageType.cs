namespace Broker.Protocol;

public enum MessageType
{
    None = 0,
    Publish = 1,
    Subscribe = 2,
    
    Ok = 100,
    Error = 101,
    
    Ping = 1000,
    Pong = 1001
}
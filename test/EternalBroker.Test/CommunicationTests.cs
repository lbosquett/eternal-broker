using System.Net;
using System.Net.Sockets;
using Broker.Protocol;
using Broker.Server;
using Xunit;

namespace EternalBroker.Test;

public class CommunicationTests
{
    [Fact]
    public async Task SendPingMessage()
    {
        MessageServer server = new MessageServer();
        server.Run(new MessageServerOptions() { Port = 7800 }, CancellationToken.None);

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, 7800);
        NetworkStream stream = client.GetStream();

        MessageBuilder builder = new MessageBuilder();
        builder.MessageType = MessageType.Ping;
        ReadOnlyMemory<byte> serializedMessage = builder.Build();

        // send ping
        stream.Write(serializedMessage.Span);

        // wait for pong
        byte[] received = new byte[8];
        await stream.FlushAsync();
        int read = stream.Read(received, 0, received.Length);

        client.Client.Shutdown(SocketShutdown.Send);
        await stream.FlushAsync();
        stream.Close();
        client.Close();

        await server.StopAsync();

        Assert.Equal(received.Length, read);
    }
}
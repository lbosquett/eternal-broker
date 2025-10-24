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

        MessageSerializer serializer = new MessageSerializer();
        ReadOnlyMemory<byte> serializedMessage = serializer.Serialize(new ProtocolMessage(Guid.NewGuid(), MessageType.Ping, 0, ReadOnlyMemory<byte>.Empty));

        // send ping
        stream.Write(serializedMessage.Span.Slice(0, 1));
        stream.Flush();
        Thread.Sleep(100);

        stream.Write(serializedMessage.Span.Slice(1, 1));
        stream.Flush();
        Thread.Sleep(100);

        stream.Write(serializedMessage.Span.Slice(2));
        stream.Flush();
        Thread.Sleep(100);

        // wait for pong
        byte[] received = new byte[12];
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
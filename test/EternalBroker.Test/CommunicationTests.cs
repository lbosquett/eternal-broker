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
        server.Run(new MessageServerOptions() { Port = Constants.TestPort }, CancellationToken.None);

        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, Constants.TestPort);
        NetworkStream stream = client.GetStream();

        MessageSerializer serializer = new MessageSerializer();
        ReadOnlyMemory<byte> serializedMessage = serializer.Serialize(new ProtocolMessage(MessageType.Ping, ReadOnlyMemory<byte>.Empty));

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
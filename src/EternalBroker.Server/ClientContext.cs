using System.Net.Sockets;
using System.Threading.Channels;

namespace Broker.Server;

internal class ClientContext
{
    internal Guid ClientKey { get; } = Guid.NewGuid();
    public Socket Client { get; }
    internal MessageServer Server { get; }
    internal ClientHandler ClientHandler { get; }

    internal ClientContext(MessageServer server, Socket client)
    {
        Client = client;
        Server = server;
        ClientHandler = new ClientHandler(client, new ServerApplicationMessageListener(this));
    }
}
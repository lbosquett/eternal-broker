using System.Net;
using System.Net.Sockets;
using Broker.Protocol;

namespace Broker.Server;

public class MessageServer
{
    private Task? _serverTask;
    private TcpListener? _listener;

    public Task Run(CancellationToken cancellationToken)
    {
        if (_serverTask != null)
            throw new InvalidOperationException("server already running");


        return _serverTask = Task.Run(() => ServerTask(cancellationToken), cancellationToken);
    }

    private void ServerTask(CancellationToken cancellationToken)
    {
        _listener = new TcpListener(IPAddress.Any, 24242);
        _listener.Start(200);
        
        List<TcpClient> clients = new();
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = _listener.AcceptTcpClient();
            NetworkStream stream = client.GetStream();

            byte[] messageHeader = new byte[8];
            byte[] messageBody = new byte[1024];
            while (client.Connected && !cancellationToken.IsCancellationRequested)
            {
                int read = stream.Read(messageHeader, 0, 8);

                // simple dumb protocol
                // 4 bytes for message type
                // 4 bytes for message length
                // ... the message
                MessageType messageType = (MessageType)BitConverter.ToInt32(messageHeader);
                int messageLength = BitConverter.ToInt32(messageHeader, 4);

                // for now there is a limit, but why not let it exceed?
                if (messageLength > messageBody.Length)
                {
                    client.Close();
                }

                read = stream.Read(messageBody, 0, messageLength);
                if (read != messageLength) throw new InvalidOperationException("well...");

                switch (messageType)
                {
                    case MessageType.Subscribe:
                        break;
                    case MessageType.Publish:
                        break;
                }
            }
        }
    }
}
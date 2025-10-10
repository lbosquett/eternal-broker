using System.Net;
using System.Net.Sockets;
using Broker.Protocol;

namespace Broker.Server;

public class MessageServer
{
    private Task? _serverTask;
    private CancellationTokenSource? _cts;
    private TcpListener? _listener;

    public Task Run(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_serverTask != null)
            throw new InvalidOperationException("server already running");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = ServerTask(options, cancellationToken);

        return _serverTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null || _serverTask is null)
            throw new InvalidOperationException("server already stopping or not started");

        await _cts.CancelAsync();
        _cts.Dispose();

        try
        {
            await _serverTask;
        }
        catch (OperationCanceledException e)
        {
            /* ignore */
        }
    }

    private async Task ServerTask(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_cts is null) throw new InvalidOperationException("cancellation token not created");

        _listener = new TcpListener(IPAddress.Any, options.Port);
        _listener.Start(200);

        List<TcpClient> clients = new();
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
            NetworkStream stream = client.GetStream();

            byte[] messageHeader = new byte[8];
            byte[] messageBody = new byte[1024];
            while (client.Connected && !cancellationToken.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(messageHeader, 0, 8, _cts.Token);
                if (read <= 8) throw new Exception("unexpected end of stream");

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

                read = await stream.ReadAsync(messageBody, 0, messageLength, _cts.Token);
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
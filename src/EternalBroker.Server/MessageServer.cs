using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Broker.Server;

public class MessageServer
{
    private Task? _serverTask;
    private CancellationTokenSource? _cts;
    private TcpListener? _listener;
    private Channel<Message> _messageChannel = Channel.CreateBounded<Message>(32);

    private readonly ConcurrentDictionary<Guid, MessageServerClient> _clients = new();

    public Task? ServerTask => _serverTask;

    public void Run(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_serverTask != null)
            throw new InvalidOperationException("server already running");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = RunServerTask(options, cancellationToken);
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

    private async Task RunServerTask(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_cts is null) throw new InvalidOperationException("cancellation token not created");

        _listener = new TcpListener(IPAddress.Any, options.Port);
        _listener.Start(64);

        // TODO: move to a event based method
        while (!_cts.IsCancellationRequested)
        {
            Socket client = await _listener.AcceptSocketAsync(_cts.Token);

            var clientKey = Guid.NewGuid();
            var messageServerClient = new MessageServerClient(clientKey, client);

            _clients.TryAdd(clientKey, messageServerClient);
            messageServerClient.ReceiveMessageLoop();
        }
    }
}
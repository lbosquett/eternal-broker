using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Broker.Server;

public class MessageServer
{
    private Task? _listenerTask;
    private CancellationTokenSource? _cts;

    private TcpListener? _listener;
    private readonly ConcurrentDictionary<Guid, ClientContext> _clients = new();

    internal Channel<SendMessageRequest> MessageChannel { get; } =  Channel.CreateBounded<SendMessageRequest>(32);

    public Task? ListenerTask => _listenerTask;

    public void Run(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_listenerTask != null)
            throw new InvalidOperationException("server already running");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenerTask = StartListenerTask(options);
    }

    public async Task StopAsync()
    {
        if (_cts is null
            || _listenerTask is null)
            throw new InvalidOperationException("server already stopping or not started");

        await _cts.CancelAsync();
        _cts.Dispose();

        try
        {
            await _listenerTask;
        }
        catch (OperationCanceledException)
        {
            /* ignore */
        }
    }

    private Task StartListenerTask(MessageServerOptions options)
    {
        if (_cts is null) throw new InvalidOperationException("cancellation token not created");

        _listener = new TcpListener(IPAddress.Any, options.Port);
        _listener.Start(64);

        return Task.Run(async () =>
        {
            // TODO: move to a event based method
            // TODO: add a task for each client (?)
            while (!_cts.IsCancellationRequested)
            {
                Socket client = await _listener.AcceptSocketAsync(_cts.Token);

                var clientContext = new ClientContext(this, client);
                _clients.TryAdd(clientContext.ClientKey, clientContext);

                clientContext.ClientHandler.Listen(_cts.Token);
            }
        });
    }
}
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Broker.Protocol;

namespace Broker.Server;

public class MessageServer
{
    private Task? _listenerTask;
    private Task? _messageConsumerTask;

    private CancellationTokenSource? _cts;

    private TcpListener? _listener;

    // todo: add capacity to config
    private readonly Channel<ProtocolMessage> _messageChannel = Channel.CreateBounded<ProtocolMessage>(32);

    private readonly ConcurrentDictionary<Guid, ClientHandler> _clients = new();
    private readonly ApiHandler _apiHandler = new();

    // hooks
    public event Action<ProtocolMessage>? MessageReceived;

    public Task? ListenerTask => _listenerTask;

    public void Run(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_listenerTask != null)
            throw new InvalidOperationException("server already running");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _messageConsumerTask = StartMessageConsumerTask(options, cancellationToken);
        _listenerTask = StartListenerTask(options);
    }

    private async Task StartMessageConsumerTask(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_cts is null) throw new InvalidOperationException("cancellation token not created");
        while (!_cts.IsCancellationRequested)
        {
            await foreach (ProtocolMessage message in _messageChannel.Reader.ReadAllAsync(_cts.Token))
            {
                // todo: implement
                // todo: handle exceptions, send back to client?
                switch (message.MessageType)
                {
                    // case MessageType.Publish:
                    //     break;
                    // case MessageType.Api:
                    //     _clients.TryGetValue(message.Sender, out ClientHandler? apiClientHandler);
                    //     if (apiClientHandler is null) throw new InvalidOperationException();
                    //     await _apiHandler.Handle(message, apiClientHandler);
                    //     break;
                }

                MessageReceived?.Invoke(message);
            }
        }
    }

    public async Task StopAsync()
    {
        if (_cts is null
            || _listenerTask is null
            || _messageConsumerTask is null)
            throw new InvalidOperationException("server already stopping or not started");

        await _cts.CancelAsync();
        _cts.Dispose();

        try
        {
            await _listenerTask;
            await _messageConsumerTask;
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

                var clientKey = Guid.NewGuid();
                var messageServerClient = new ClientHandler(clientKey, client, _messageChannel, _cts.Token);
                _clients.TryAdd(clientKey, messageServerClient);

                messageServerClient.ReceiveMessageLoop();
            }
        });
    }
}
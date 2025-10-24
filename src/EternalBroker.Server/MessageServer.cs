using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Broker.Protocol;

namespace Broker.Server;

public class MessageServer
{
    private Task? _listenerTask;
    private Task? _senderTask;

    private CancellationTokenSource? _cts;

    private TcpListener? _listener;

    // todo: add capacity to config
    private readonly Channel<ProtocolMessage> _messageChannel = Channel.CreateBounded<ProtocolMessage>(32);

    private readonly ConcurrentDictionary<Guid, MessageServerClient> _clients = new();
    private readonly ConcurrentDictionary<int, string> _topics = new();

    public Task? ListenerTask => _listenerTask;

    public void Run(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_listenerTask != null)
            throw new InvalidOperationException("server already running");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenerTask = StartListenerTask(options);
        _senderTask = StartSenderTask(options, cancellationToken);
    }

    private async Task StartSenderTask(MessageServerOptions options, CancellationToken cancellationToken)
    {
        if (_cts is null) throw new InvalidOperationException("cancellation token not created");
        while (!_cts.IsCancellationRequested)
        {
            await foreach (ProtocolMessage message in _messageChannel.Reader.ReadAllAsync(_cts.Token))
            {
                // todo: implement
                switch (message.MessageType)
                {
                    case MessageType.Ping:
                        if (_clients.TryGetValue(message.Sender, out var client))
                        {
                            var pong = new ProtocolMessage(message.Sender, MessageType.Ping, 0,
                                ReadOnlyMemory<byte>.Empty);
                            await client.SendMessageAsync(pong);
                        }
                        break;
                    case MessageType.Publish:
                        break;
                }
            }
        }
    }

    public async Task StopAsync()
    {
        if (_cts is null
            || _listenerTask is null
            || _senderTask is null)
            throw new InvalidOperationException("server already stopping or not started");

        await _cts.CancelAsync();
        _cts.Dispose();

        try
        {
            await _listenerTask;
            await _senderTask;
        }
        catch (OperationCanceledException)
        {
            /* ignore */
        }
    }

    private async Task StartListenerTask(MessageServerOptions options)
    {
        if (_cts is null) throw new InvalidOperationException("cancellation token not created");

        _listener = new TcpListener(IPAddress.Any, options.Port);
        _listener.Start(64);

        // TODO: move to a event based method
        // TODO: add a task for each client (?)
        while (!_cts.IsCancellationRequested)
        {
            Socket client = await _listener.AcceptSocketAsync(_cts.Token);

            var clientKey = Guid.NewGuid();
            var messageServerClient = new MessageServerClient(clientKey, client, _messageChannel, _cts.Token);

            _clients.TryAdd(clientKey, messageServerClient);
            messageServerClient.ReceiveMessageLoop();
        }
    }
}
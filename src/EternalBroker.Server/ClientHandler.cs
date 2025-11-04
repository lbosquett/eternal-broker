using System.Net.Sockets;
using Broker.Protocol;

namespace Broker.Server;

internal class ClientHandler
{
    private readonly ApplicationMessageListener _applicationMessageListener;
    private readonly ApplicationMessageSender _applicationMessageSender;
    private Task? _listenerTask;

    internal ClientHandler(Socket client, IMessageListener messageListener)
    {
        _applicationMessageListener = new ApplicationMessageListener(client, messageListener);
        _applicationMessageSender = new ApplicationMessageSender(client);
    }

    internal void Listen(CancellationToken cancellationToken)
    {
        if (_listenerTask is not null) throw new InvalidOperationException();

        _listenerTask = _applicationMessageListener.ReceiveLoop(cancellationToken);
    }

    internal async Task SendMessageAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        await _applicationMessageSender.SendMessageAsync(message, cancellationToken);
    }

    // todo: implement on disconnect event
}
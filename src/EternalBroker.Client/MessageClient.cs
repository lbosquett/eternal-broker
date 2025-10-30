using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using Broker.Protocol;
using Broker.Protocol.Api;
using Broker.Serialization.Json;

namespace Broker.Client;

public class MessageClient
{
    private TcpClient? _tcpClient;
    private readonly MessageSerializer _messageSerializer = new();
    private IPAddress? _address;
    private int _port;
    private Task? _receiverTask;
    private Task? _senderTask;
    private CancellationTokenSource? _senderReceiverCombinedToken;
    private Channel<ProtocolMessage>? _receiverMessageChannel;
    private Channel<ProtocolMessage>? _senderMessageChannel;

    public async Task ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
    {
        if (_receiverTask is not null
            || _senderTask is not null
            || _tcpClient is not null)
        {
            throw new InvalidOperationException();
        }

        _tcpClient = new TcpClient();
        _address = address;
        _port = port;
        await _tcpClient.ConnectAsync(_address, _port, cancellationToken);

        // TODO: bounded?
        _receiverMessageChannel = Channel.CreateUnbounded<ProtocolMessage>();
        _senderMessageChannel = Channel.CreateUnbounded<ProtocolMessage>();

        _senderReceiverCombinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiverTask = Task.Run(ReceiverTask, _senderReceiverCombinedToken.Token);
        _senderTask = Task.Run(SenderTask, _senderReceiverCombinedToken.Token);
    }

    private void ReceiverTask()
    {
        // TODO: implement
        throw new NotImplementedException();
    }

    private void SenderTask()
    {
        // TODO: implement
        throw new NotImplementedException();
    }

    public async Task CreateTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        if (_senderMessageChannel is null) throw new InvalidOperationException();

        // TODO: create a channel to send messages
        var request = new JsonApiMessageRequest("/topics/create", new Dictionary<string, object>()
        {
            ["topic"] = topicName
        });
        byte[] serializedApiRequest =
            JsonSerializer.SerializeToUtf8Bytes(request, JsonApiMessageContext.Default.JsonApiMessageRequest);
        ProtocolMessage message = new(MessageType.Api, new ReadOnlyMemory<byte>(serializedApiRequest));

        // send
        await _senderMessageChannel.Writer.WriteAsync(message, cancellationToken);

        // get the response
        // TODO: implement notification (send -> receive -> complete)
    }

    public async Task CloseConnectionAsync()
    {
        if (_tcpClient is null
            || _senderReceiverCombinedToken is null) throw new InvalidOperationException();

        if (_senderTask != null) await _senderTask;
        if (_receiverTask != null) await _receiverTask;
        await _senderReceiverCombinedToken.CancelAsync();

        _tcpClient.Close();
        _tcpClient.Dispose();
    }
}
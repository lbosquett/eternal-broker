using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using Broker.Protocol;

namespace Broker.Server;

internal class ClientHandler
{
    private readonly Guid _clientKey;
    private readonly Socket _client;
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly SocketAsyncEventArgs _receiveEventArgs;
    private readonly MessageParser _parser = new();
    private readonly MessageSerializer _serializer = new();
    private readonly ManualResetEventSlim _canReceive = new();
    private readonly Channel<ProtocolMessage> _messageChannel;
    private readonly CancellationToken _cancellationToken;

    internal ClientHandler(Guid clientKey, Socket client,
        Channel<ProtocolMessage> messageChannel,
        CancellationToken cancellationToken)
    {
        _clientKey = clientKey;
        _client = client;
        _memoryOwner = MemoryPool<byte>.Shared.Rent(8192);
        _messageChannel = messageChannel;
        _cancellationToken = cancellationToken;

        _receiveEventArgs = new SocketAsyncEventArgs();
        _receiveEventArgs.SetBuffer(_memoryOwner.Memory);
        _receiveEventArgs.Completed += SocketCompletedReceiveEvent;
    }

    public void ReceiveMessageLoop()
    {
        _canReceive.Set();

        while (!_cancellationToken.IsCancellationRequested
               && _client.Connected)
        {
            _canReceive.Wait(_cancellationToken);

            bool pending = _client.ReceiveAsync(_receiveEventArgs);
            if (pending)
            {
                _canReceive.Reset();
                continue;
            }

            ReceiveMessage(_receiveEventArgs);
        }
    }

    private void SocketCompletedReceiveEvent(object? sender, SocketAsyncEventArgs e)
    {
        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            ReceiveMessage(e);
        }
    }

    private void ReceiveMessage(SocketAsyncEventArgs e)
    {
        if (e is { BytesTransferred: 0, SocketError: SocketError.Success })
        {
            if (_client.Connected)
                _client.Shutdown(SocketShutdown.Both);
            return;
        }

        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            Memory<byte> buffer = _memoryOwner.Memory;
            int consumed = 0;
            while (_parser.TryParseMessage(ref buffer, _clientKey, e.BytesTransferred, out ProtocolMessage? message))
            {
                if (message == null) throw new InvalidOperationException("unexpected message to be null");

                consumed += message.Payload.Length + 8;
                bool messageSent = _messageChannel.Writer.TryWrite(message);

                if (!messageSent)
                {
                    // todo: handle cases when server is handling messages slowly
                    // - signal server to process messages?
                    // - discard message?
                    // - buffering?
                    throw new NotImplementedException();
                }
            }

            // compact
            if (buffer.Length < 1024)
            {
                int toCopy = e.BytesTransferred - consumed;
                _memoryOwner.Memory.Slice(_parser.StartMessagePosition, toCopy)
                    .CopyTo(_memoryOwner.Memory);

                buffer = _memoryOwner.Memory.Slice(_parser.StartMessagePosition);
            }

            e.SetBuffer(buffer);
        }
        _canReceive.Set();
    }

    public async Task SendMessageAsync(ProtocolMessage pong)
    {
        await _client.SendAsync(_serializer.Serialize(pong), _cancellationToken);
    }
}
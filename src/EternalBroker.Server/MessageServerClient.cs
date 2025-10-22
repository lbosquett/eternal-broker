using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Broker.Server;

internal class MessageServerClient
{
    private readonly Guid _clientKey;
    private readonly Socket _client;
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly SocketAsyncEventArgs _eventArgs;
    private readonly MessageParser _parser;
    private readonly Channel<Message> _messageChannel;
    private readonly CancellationToken _cancellationToken;

    internal MessageServerClient(Guid clientKey, Socket client, Channel<Message> messageChannel,
        IMessageFactory messageFactory,
        CancellationToken cancellationToken)
    {
        _clientKey = clientKey;
        _client = client;
        _memoryOwner = MemoryPool<byte>.Shared.Rent(8192);
        _parser = new MessageParser(messageFactory);
        _messageChannel = messageChannel;
        _cancellationToken = cancellationToken;

        _eventArgs = new SocketAsyncEventArgs();
        _eventArgs.SetBuffer(_memoryOwner.Memory);
        _eventArgs.Completed += SocketCompletedEvent;
    }

    public void ReceiveMessageLoop()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            bool async = _client.ReceiveAsync(_eventArgs);
            if (async) break;

            ReceiveMessage(_eventArgs);
        }
    }

    private void SocketCompletedEvent(object? sender, SocketAsyncEventArgs e)
    {
        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            ReceiveMessage(e);
        }
    }

    private void ReceiveMessage(SocketAsyncEventArgs e)
    {
        if (e.LastOperation == SocketAsyncOperation.Receive)
        {
            Memory<byte> buffer = _memoryOwner.Memory;
            int consumed = 0;
            while (_parser.TryParseMessage(ref buffer, e.BytesTransferred, out Message? message))
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
        ReceiveMessageLoop();
    }
}
using System.Buffers;
using System.Net.Sockets;

namespace Broker.Server;

internal class MessageServerClient
{
    private readonly Guid _clientKey;
    private readonly Socket _client;
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly SocketAsyncEventArgs _eventArgs;
    private readonly MessageParser _parser;

    internal MessageServerClient(Guid clientKey, Socket client)
    {
        _clientKey = clientKey;
        _client = client;
        _memoryOwner = MemoryPool<byte>.Shared.Rent(8192);
        _parser = new MessageParser();

        _eventArgs = new SocketAsyncEventArgs();
        _eventArgs.SetBuffer(_memoryOwner.Memory);
        _eventArgs.Completed += SocketCompletedEvent;
    }

    public void ReceiveMessageLoop()
    {
        while (true)
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
            while (_parser.TryParseMessage(ref buffer, e.BytesTransferred, out Message message))
            {
                if (message == null) throw new InvalidOperationException("unexpected message to be null");

                // todo: emit message
                consumed += message.Payload.Length + 8;
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
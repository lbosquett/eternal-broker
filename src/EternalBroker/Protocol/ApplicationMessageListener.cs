using System.Buffers;
using System.Net.Sockets;
using Broker.Server;

namespace Broker.Protocol;

public class ApplicationMessageListener
{
    private readonly MessageParser _messageParser = new();

    private readonly Socket _client;
    private readonly IMessageListener _messageListener;
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly ManualResetEventSlim _canReceive = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SocketAsyncEventArgs _receiveEventArgs;

    public ApplicationMessageListener(Socket client, IMessageListener messageListener)
    {
        _memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

        _receiveEventArgs = new SocketAsyncEventArgs();
        _receiveEventArgs.SetBuffer(_memoryOwner.Memory);
        _receiveEventArgs.Completed += SocketCompletedReceiveEvent;

        _client = client;
        _messageListener = messageListener;
    }

    public void ReceiveLoop()
    {
        _canReceive.Set();

        while (!_cts.IsCancellationRequested
               && _client.Connected)
        {
            _canReceive.Wait(_cts.Token);

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
            while (_messageParser.TryParseMessage(ref buffer, e.BytesTransferred,
                       out ProtocolMessage? message))
            {
                if (message == null) throw new InvalidOperationException("unexpected message to be null");

                consumed += message.Payload.Length + 8;
                bool messageSent = _messageListener.TryReceiveMessage(message);

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
                _memoryOwner.Memory.Slice(_messageParser.StartMessagePosition, toCopy)
                    .CopyTo(_memoryOwner.Memory);

                buffer = _memoryOwner.Memory.Slice(_messageParser.StartMessagePosition);
            }

            e.SetBuffer(buffer);
        }
        _canReceive.Set();
    }
}
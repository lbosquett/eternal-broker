using System.Buffers;
using System.Net.Sockets;
using Broker.Server;

namespace Broker.Protocol;

public class ApplicationMessageListener(Socket client, IMessageListener messageListener)
{
    private readonly MessageParser _messageParser = new();

    private readonly IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

    public async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        // TODO: move back to SocketReceiveAsyncArgs

        Memory<byte> buffer = _memoryOwner.Memory;
        while (!cancellationToken.IsCancellationRequested
               && client.Connected)
        {
            int transferred = await client.ReceiveAsync(buffer, cancellationToken);

            if (transferred == 0)
            {
                if (client.Connected)
                    client.Shutdown(SocketShutdown.Both);
                return;
            }

            int consumed = 0;
            while (_messageParser.TryParseMessage(ref buffer, transferred,
                       out ProtocolMessage? message))
            {
                if (message == null) throw new InvalidOperationException("unexpected message to be null");

                consumed += message.Payload.Length + 8;
                await messageListener.ReceiveMessageAsync(message, cancellationToken);
            }

            // compact
            if (buffer.Length < 1024)
            {
                int toCopy = transferred - consumed;
                _memoryOwner.Memory.Slice(_messageParser.StartMessagePosition, toCopy)
                    .CopyTo(_memoryOwner.Memory);

                buffer = _memoryOwner.Memory.Slice(_messageParser.StartMessagePosition);
            }
        }
    }
}
using Broker.Protocol;

namespace Broker.Server;

public class MessageParser
{
    public int StartMessagePosition { get; private set; }

    private int _currentFrameLength;

    public bool TryParseMessage(ref Memory<byte> buffer,
        Guid clientKey,
        int transferred,
        out ReceivedProtocolMessage? message)
    {
        _currentFrameLength += transferred - StartMessagePosition;
        Memory<byte> frame = buffer.Slice(StartMessagePosition, _currentFrameLength);
        int endPosition = StartMessagePosition + _currentFrameLength;

        // empty
        if (frame.Length == 0)
        {
            // parsed all messages
            // opportunity to reset to the beginning position
            message = null;
            return false;
        }

        // message header
        if (frame.Length < 8)
        {
            buffer = buffer.Slice(endPosition);
            message = null;
            return false;
        }

        // todo: check message version
        // type | length | payload
        var messageType = (MessageType)BitConverter.ToInt32(buffer.Span.Slice(0, 4));
        int messageLength = BitConverter.ToInt32(buffer.Span.Slice(4, 4));

        // we don't have enough data yet
        // 12 = version + topic + length 
        if (messageLength + 8 > frame.Length)
        {
            buffer = buffer.Slice(endPosition);
            message = null;
            return false;
        }

        message = new ReceivedProtocolMessage(clientKey, messageType, frame.Slice(8, messageLength));
        StartMessagePosition = StartMessagePosition + messageLength + 8;
        _currentFrameLength = 0;
        return true;
    }
}
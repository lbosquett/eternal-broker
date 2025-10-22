namespace Broker.Server;

public class MessageParser
{
    public int StartMessagePosition { get; private set; }

    private int _currentFrameLength;
    private readonly IMessageFactory _messageFactory;

    public MessageParser(IMessageFactory messageFactory)
    {
        _messageFactory = messageFactory;
    }
    

    public bool TryParseMessage(ref Memory<byte> buffer, int transferred,
        out Message? message)
    {
        _currentFrameLength += transferred;
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
        // 0.1 = | length | topic | message
        int messageVersion = BitConverter.ToInt32(buffer.Span.Slice(0, 4));
        int topicCode = BitConverter.ToInt32(buffer.Span.Slice(4, 4));
        int messageLength = BitConverter.ToInt32(buffer.Span.Slice(8, 4));

        // we don't have enough data yet
        // 12 = version + topic + length 
        if (messageLength + 12 > frame.Length)
        {
            buffer = buffer.Slice(endPosition);
            message = null;
            return false;
        }

        message = _messageFactory.Create(topicCode, frame.Slice(12, messageLength));
        StartMessagePosition = StartMessagePosition + messageLength + 8;
        _currentFrameLength = 0;
        return true;
    }
}
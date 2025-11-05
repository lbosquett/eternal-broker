using Broker.Protocol;

namespace Broker.Server;

internal record SendMessageRequest(ClientHandler ClientHandler, ProtocolMessage ProtocolMessage);

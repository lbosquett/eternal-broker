namespace Broker.Protocol.Api;

public record JsonApiMessageRequest(string Path, IDictionary<string, object> Parameters);
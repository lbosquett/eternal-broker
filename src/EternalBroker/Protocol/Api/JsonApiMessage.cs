namespace Broker.Protocol.Api;

public record JsonApiMessage(string Path, IDictionary<string, object> Parameters);
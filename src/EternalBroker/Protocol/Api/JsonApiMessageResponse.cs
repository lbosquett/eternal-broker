namespace Broker.Protocol.Api;

public record JsonApiMessageResponse(bool Success, object? Content);

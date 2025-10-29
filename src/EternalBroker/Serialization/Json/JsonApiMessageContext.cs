using System.Text.Json.Serialization;
using Broker.Protocol.Api;

namespace Broker.Serialization.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonApiMessageRequest))]
[JsonSerializable(typeof(JsonApiMessageResponse))]
[JsonSerializable(typeof(Topic))]
public partial class JsonApiMessageContext : JsonSerializerContext
{
    
}
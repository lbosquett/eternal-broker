using System.Text.Json.Serialization;
using Broker.Protocol.Api;

namespace Broker.Server.Serialization.Json;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonApiMessage))]
internal partial class JsonApiMessageContext : JsonSerializerContext
{
    
}
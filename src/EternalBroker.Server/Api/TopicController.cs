namespace Broker.Server.Api;

public class TopicController
{
    public Task<IEnumerable<Topic>> ListTopics()
    {
        return Task.FromResult(Enumerable.Empty<Topic>());
    }
}
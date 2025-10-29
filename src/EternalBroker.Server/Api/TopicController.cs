using System.Collections.Concurrent;

namespace Broker.Server.Api;

public class TopicController
{
    // TODO: persistent topics with a db or yaml, etc
    // TODO: check concurrent requests
    private ConcurrentBag<Topic> _topics = new();

    public void CreateTopic(string name)
    {
        _topics.Add(new Topic(Guid.NewGuid(), name));
    }

    public IEnumerable<Topic> ListTopics() => _topics;
}
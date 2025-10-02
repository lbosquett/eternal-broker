namespace Broker.Server;

static class Program
{
    static async Task Main(string[] args)
    {
        var server = new MessageServer();
        await server.Run(CancellationToken.None);
    }
}

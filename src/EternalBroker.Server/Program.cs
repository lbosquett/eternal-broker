namespace Broker.Server;

static class Program
{
    static async Task Main(string[] args)
    {
        // todo: args to options

        var server = new MessageServer();
        await server.Run(MessageServerOptions.Default, CancellationToken.None);
    }
}

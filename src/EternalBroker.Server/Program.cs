namespace Broker.Server;

static class Program
{
    static async Task Main(string[] args)
    {
        // todo: args to options

        var server = new MessageServer();
        server.Run(MessageServerOptions.Default, CancellationToken.None);

        if (server.ListenerTask == null) throw new InvalidOperationException("unable to start server");

        await server.ListenerTask;
    }
}

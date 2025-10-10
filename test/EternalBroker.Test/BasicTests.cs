using Broker.Server;
using Xunit;

namespace EternalBroker.Test;

public class BasicTests
{
    [Fact]
    public async Task Initialize()
    {
        CancellationTokenSource cts = new CancellationTokenSource();

        MessageServer server = new();
        _ = server.Run(new MessageServerOptions() { Port = 7800 }, cts.Token);
        await server.StopAsync();
    }
}
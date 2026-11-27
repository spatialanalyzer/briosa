using System.Reflection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Briosa.Cli;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        if (args is ["--version"])
        {
            Console.WriteLine(typeof(Program).Assembly.GetName().Version);
            return;
        }

        if (args is ["mcp"])
        {
            var options = new McpServerOptions
            {
                ServerInfo = new Implementation { Name = "Briosa.Cli.Spike", Version = "0.0.0" }
            };
            var transport = new StdioServerTransport(options);
            await using var transportLifetime = transport.ConfigureAwait(false);
            var server = McpServer.Create(transport, options);
            await using var serverLifetime = server.ConfigureAwait(false);
            await server.RunAsync().ConfigureAwait(false);
            return;
        }

        if (args is ["verify", var descriptorPath, var address])
        {
            var proof = new DescriptorProof();
            await proof.VerifyAsync(descriptorPath, address).ConfigureAwait(false);
            return;
        }

        await Console.Error.WriteLineAsync(
            $"Usage: {Assembly.GetExecutingAssembly().GetName().Name} --version | mcp | verify PROTOSET ADDRESS")
            .ConfigureAwait(false);
        Environment.ExitCode = 2;
    }
}

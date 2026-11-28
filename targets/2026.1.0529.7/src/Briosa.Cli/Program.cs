using System.Text;
using Briosa.Cli.CommandLine;
using Briosa.Cli.Engine;

namespace Briosa.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();

        // The first Ctrl+C stops waiting for the server; the envelope then says that
        // completion is unknown. A second one ends the process as usual.
        void OnCancel(object? sender, ConsoleCancelEventArgs e)
        {
            if (!cancellation.IsCancellationRequested)
            {
                e.Cancel = true;
                cancellation.Cancel();
            }
        }

        Console.CancelKeyPress += OnCancel;
        try
        {
            using var standardOutput = Console.OpenStandardOutput();
            using var standardError = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false));
            return await CliApplication.RunAsync(args,
                new CliHost(standardOutput, standardError, BridgeEnvironment.Default),
                cancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            Console.CancelKeyPress -= OnCancel;
        }
    }
}

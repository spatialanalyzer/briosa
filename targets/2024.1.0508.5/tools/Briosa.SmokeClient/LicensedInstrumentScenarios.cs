using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Briosa;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;

internal static class LicensedInstrumentScenarios
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The opt-in probe emits structural results only, never fixture paths, values, or raw exception text.")]
    public static async Task<int> RunAsync(string[] arguments)
    {
        try
        {
            if (!arguments.Contains("--confirm-licensed-sa2024", StringComparer.Ordinal))
            {
                throw new InvalidOperationException();
            }

            var scenario = Option(arguments, "--licensed-instrument");
            var address = new Uri(Option(arguments, "--address"));
            if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
            {
                throw new InvalidOperationException();
            }

            var seconds = int.Parse(Option(arguments, "--timeout-seconds"), CultureInfo.InvariantCulture);
            if (seconds is < 1 or > 600)
            {
                throw new InvalidOperationException();
            }

            var json = await File.ReadAllTextAsync(Option(arguments, "--request-file")).ConfigureAwait(false);
            IMessage request = scenario switch
            {
                "run-crib-sheet" => JsonParser.Default.Parse<RunCribSheetRequest>(json),
                "project-objects" => JsonParser.Default.Parse<ProjectObjectsRequest>(json),
                "stop-projection" => JsonParser.Default.Parse<StopProjectionRequest>(json),
                _ => throw new InvalidOperationException()
            };
            using var channel = GrpcChannel.ForAddress(address);
            var deadline = DateTime.UtcNow.AddSeconds(seconds);
            var discovery = new DiscoveryService.DiscoveryServiceClient(channel);
            var info = await discovery.GetServerInfoAsync(new GetServerInfoRequest(), deadline: deadline).ResponseAsync.ConfigureAwait(false);
            if (info.Version?.SpatialAnalyzerTarget != "2024.1.0508.5")
            {
                throw new InvalidOperationException();
            }

            // One reviewed operation per invocation. Never retry or synthesize cleanup after an unknown outcome.
            var client = new InstrumentOperations.InstrumentOperationsClient(channel);
            var execution = request switch
            {
                RunCribSheetRequest crib => (await client.RunCribSheetAsync(crib, deadline: deadline).ResponseAsync.ConfigureAwait(false)).Execution,
                ProjectObjectsRequest project => (await client.ProjectObjectsAsync(project, deadline: deadline).ResponseAsync.ConfigureAwait(false)).Execution,
                StopProjectionRequest stop => (await client.StopProjectionAsync(stop, deadline: deadline).ResponseAsync.ConfigureAwait(false)).Execution,
                _ => throw new InvalidOperationException()
            };
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                scenario,
                spatial_analyzer_target = "2024.1.0508.5",
                state = execution.State.ToString(),
                mp_result_code = execution.HasMpResultCode ? (int?)execution.MpResultCode : null
            }));
            return execution.State == MpExecutionState.Succeeded ? 0 : 1;
        }
        catch (RpcException exception)
        {
            var trailer = exception.Trailers.FirstOrDefault(entry => entry.Key == "briosa-operation-error-bin");
            OperationError? error = null;
            if (trailer is not null)
            {
                try { error = OperationError.Parser.ParseFrom(trailer.ValueBytes); }
                catch (InvalidProtocolBufferException) { }
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                diagnostic = "licensed-instrument-rpc-failure",
                status = exception.StatusCode.ToString(),
                disposition = error?.ExecutionDisposition.ToString(),
                mp_result_code = error?.MpExecution is { HasMpResultCode: true } execution ? (int?)execution.MpResultCode : null
            }));
            return 1;
        }
        catch (Exception)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { diagnostic = "licensed-instrument-probe-failed" }));
            return 1;
        }
    }

    private static string Option(string[] arguments, string key)
    {
        var index = Array.IndexOf(arguments, key);
        return index >= 0 && index + 1 < arguments.Length
            ? arguments[index + 1]
            : throw new ArgumentException("A required probe option is missing.", nameof(arguments));
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Briosa;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;

internal static class LicensedReportingFrameScenarios
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "The opt-in probe emits structural outcomes only, never fixture values or raw exceptions.")]
    public static async Task<int> RunAsync(string[] arguments)
    {
        try
        {
            if (!arguments.Contains("--confirm-licensed-sa2026", StringComparer.Ordinal))
                throw new InvalidOperationException();

            var scenario = Option(arguments, "--licensed-reporting-frame");
            var address = new Uri(Option(arguments, "--address"));
            if (!address.IsLoopback || address.Scheme != Uri.UriSchemeHttp)
                throw new InvalidOperationException();

            var seconds = int.Parse(Option(arguments, "--timeout-seconds"), CultureInfo.InvariantCulture);
            if (seconds is < 1 or > 600)
                throw new InvalidOperationException();

            var json = await File.ReadAllTextAsync(Option(arguments, "--request-file")).ConfigureAwait(false);
            IMessage request = scenario switch
            {
                "object" => JsonParser.Default.Parse<GetObjectReportingFrameRequest>(json),
                "relationship" => JsonParser.Default.Parse<GetRelationshipReportingFrameRequest>(json),
                _ => throw new InvalidOperationException()
            };

            using var channel = GrpcChannel.ForAddress(address);
            var deadline = DateTime.UtcNow.AddSeconds(seconds);
            var discovery = new DiscoveryService.DiscoveryServiceClient(channel);
            var info = await discovery.GetServerInfoAsync(new GetServerInfoRequest(), deadline: deadline)
                .ResponseAsync.ConfigureAwait(false);
            if (info.Version?.SpatialAnalyzerTarget != "2026.1.0529.7")
                throw new InvalidOperationException();

            // One read-only MP command per invocation; an uncertain outcome is never replayed.
            MpExecutionDetails execution;
            CollectionObjectName frame;
            if (request is GetObjectReportingFrameRequest objectRequest)
            {
                var client = new AnalysisOperations.AnalysisOperationsClient(channel);
                var result = await client.GetObjectReportingFrameAsync(objectRequest, deadline: deadline)
                    .ResponseAsync.ConfigureAwait(false);
                execution = result.Execution;
                frame = result.ReportingFrame;
            }
            else
            {
                var client = new RelationshipOperations.RelationshipOperationsClient(channel);
                var result = await client.GetRelationshipReportingFrameAsync(
                    (GetRelationshipReportingFrameRequest)request, deadline: deadline)
                    .ResponseAsync.ConfigureAwait(false);
                execution = result.Execution;
                frame = result.ReportingFrame;
            }

            var passed = execution.State == MpExecutionState.Succeeded &&
                frame.ObjectType == ObjectType.Frame &&
                !string.IsNullOrWhiteSpace(frame.CollectionName) &&
                !string.IsNullOrWhiteSpace(frame.ObjectName);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                scenario,
                spatial_analyzer_target = "2026.1.0529.7",
                state = execution.State.ToString(),
                mp_result_code = execution.HasMpResultCode ? (int?)execution.MpResultCode : null,
                frame_type_verified = passed
            }));
            return passed ? 0 : 1;
        }
        catch (RpcException exception)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                diagnostic = "licensed-reporting-frame-rpc-failure",
                status = exception.StatusCode.ToString()
            }));
            return 1;
        }
        catch (Exception)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { diagnostic = "licensed-reporting-frame-probe-failed" }));
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

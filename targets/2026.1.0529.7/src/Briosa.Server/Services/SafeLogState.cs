using System.Collections;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Briosa.Server.Operations;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.Async;

namespace Briosa.Server.Services;

internal sealed class SafeLogState : IReadOnlyList<KeyValuePair<string, object?>>
{
    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdefABCDEF");
    private readonly List<KeyValuePair<string, object?>> _values;
    private SafeLogState(EventId eventId, List<KeyValuePair<string, object?>> values)
    {
        EventId = eventId;
        _values = values;
    }
    public EventId EventId { get; }
    public int Count => _values.Count;
    public KeyValuePair<string, object?> this[int index] => _values[index];
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public override string ToString() => EventId.Name + " " + string.Join(" ", _values.Where(pair => pair.Key != "{OriginalFormat}")
        .Select(pair => $"{pair.Key}={pair.Value}"));

    public static SafeLogState Create<TState>(string category, EventId eventId, TState state, string instanceId)
    {
        var trusted = category.StartsWith("Briosa.Server.", StringComparison.Ordinal) &&
            eventId.Id is 1000 or 1201 or 1300 or 1301 or 1302 or 1400 or 1401 or 1500 or 2000 or 2001 or 2002 or 2003 or 2004 or 2005;
        var name = trusted ? eventId.Id switch
        {
            1000 => "ControlPlaneReady",
            1201 => "WorkerTransition",
            1300 => "ExecutionDispatched",
            1301 => "ExecutionResolved",
            1302 => "LifecycleRejected",
            1400 => "ApplicationTransition",
            1401 => "LogSinkDegraded",
            1500 => "ReadinessNotReady",
            2000 => "PolicyLoaded",
            2001 => "RequestStarted",
            2002 => "PolicyAllowed",
            2003 => "PolicyRejected",
            2004 => "RpcCompleted",
            2005 => "RpcFailed",
            _ => "BriosaEvent"
        } : "FrameworkEvent";
        var values = new List<KeyValuePair<string, object?>>
        {
            new("ServerInstanceId", instanceId),
            new("SpatialAnalyzerTarget", SpatialAnalyzerApi.TargetVersion),
            new("{OriginalFormat}", name)
        };
        if (trusted && state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            foreach (var (key, value) in properties)
            {
                if (AllowedProperty(key) && (key == "PolicyFingerprint" ? SafeFingerprint(value) : SafeValue(value)))
                    values.Add(new(key, value));
            }
        }
        if (Activity.Current is { } activity)
        {
            values.Add(new("TraceId", activity.TraceId.ToHexString()));
            values.Add(new("SpanId", activity.SpanId.ToHexString()));
        }
        return new SafeLogState(new EventId(eventId.Id, name), values);
    }

    private static bool SafeValue(object? value) => value is null or bool or int or long or double or Guid or Enum ||
        value is string text && text.Length <= 256 &&
        text.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.' or ',' or '/');

    private static bool SafeFingerprint(object? value) => value is string text &&
        text.Length == 71 && text.StartsWith("sha256:", StringComparison.Ordinal) &&
        !text.AsSpan(7).ContainsAnyExcept(HexDigits);

    private static bool AllowedProperty(string name) => name is
        "CorrelationId" or "ActorCategory" or "Endpoint" or "OperationId" or "Effect" or
        "ExecutionScope" or "RiskFlags" or "PolicyDecision" or "AllowCount" or "DenyCount" or
        "TargetIsolationMode" or "PolicyFingerprint" or "Generation" or "RequestDurationMilliseconds" or
        "SdkDurationMilliseconds" or "ExecutionDisposition" or "MpOutcome" or "OutputRetrievalOutcome" or
        "MpResultCode" or "MpResultRetrieved" or "GrpcStatus" or "DiagnosticCode" or "ReplaySafety" or
        "WorkerState" or "RecoveryCount" or "Termination" or "ConnectionState" or "ExecutionReadinessState" or
        "StatusCode" or "ActivatedSdkIdentitySource" or "ActivatedSdkIdentityMatchState" or
        "ConnectedSaIdentitySource" or "ConnectedSaIdentityMatchState" or "AdmissionMilliseconds" or
        "QueueMilliseconds" or "ExchangeMilliseconds" or "Rpc" or "ApplicationState" or "Ownership" or
        "DroppedRecords" or "SinkFailures";
}

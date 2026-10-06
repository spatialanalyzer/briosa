using System.Globalization;
using Briosa.Worker.Control;
using Google.Protobuf;
using Grpc.Core;

namespace Briosa.LicensedProbes;

/// <summary>
/// Binds one public RPC to its shipped command builder. The same request drives
/// the public path (generated client) and the worker path (shipped builder plus
/// an explicit variant), so both paths start from identical inputs.
/// </summary>
internal abstract class ProbeOperation
{
    protected ProbeOperation(string fullyQualifiedMethod, bool isDestructive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedMethod);
        FullyQualifiedMethod = fullyQualifiedMethod;
        IsDestructive = isDestructive;
    }

    public string FullyQualifiedMethod { get; }

    // Carried from the shipped descriptor's risk flags.
    public bool IsDestructive { get; }

    public abstract WorkerMpCommand Build(IMessage request);

    public abstract Task<ProbeOutcome> InvokePublicAsync(CallInvoker invoker, IMessage request, CallOptions options);

    public abstract IReadOnlyDictionary<string, string> ObserveWorker(IMessage request, IReadOnlyList<WorkerMpOutputValue> outputs);
}

internal sealed class ProbeOperation<TRequest, TResult> : ProbeOperation
    where TRequest : class, IMessage<TRequest>
    where TResult : class, IMessage<TResult>
{
    private readonly Func<TRequest, WorkerMpCommand> _build;
    private readonly Func<CallInvoker, TRequest, CallOptions, AsyncUnaryCall<TResult>> _call;
    private readonly Func<TResult, MpExecutionDetails?> _execution;
    private readonly Func<TRequest, TResult, IReadOnlyDictionary<string, string>>? _observePublic;
    private readonly Func<TRequest, IReadOnlyList<WorkerMpOutputValue>, IReadOnlyDictionary<string, string>>? _observeWorker;

    public ProbeOperation(
        string fullyQualifiedMethod,
        bool isDestructive,
        Func<TRequest, WorkerMpCommand> build,
        Func<CallInvoker, TRequest, CallOptions, AsyncUnaryCall<TResult>> call,
        Func<TResult, MpExecutionDetails?> execution,
        Func<TRequest, TResult, IReadOnlyDictionary<string, string>>? observePublic = null,
        Func<TRequest, IReadOnlyList<WorkerMpOutputValue>, IReadOnlyDictionary<string, string>>? observeWorker = null)
        : base(fullyQualifiedMethod, isDestructive)
    {
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _call = call ?? throw new ArgumentNullException(nameof(call));
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        _observePublic = observePublic;
        _observeWorker = observeWorker;
    }

    public override WorkerMpCommand Build(IMessage request) => _build(Typed(request));

    public override async Task<ProbeOutcome> InvokePublicAsync(CallInvoker invoker, IMessage request, CallOptions options)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        var typed = Typed(request);
        try
        {
            using var call = _call(invoker, typed, options);
            var result = await call.ResponseAsync.ConfigureAwait(false);
            return PublicOutcomes.FromSuccess(
                _execution(result),
                Observe(() => _observePublic?.Invoke(typed, result)));
        }
        catch (RpcException exception)
        {
            return PublicOutcomes.FromRpcException(exception);
        }
    }

    public override IReadOnlyDictionary<string, string> ObserveWorker(IMessage request, IReadOnlyList<WorkerMpOutputValue> outputs) =>
        Observe(() => _observeWorker?.Invoke(Typed(request), outputs));

    private static TRequest Typed(IMessage request) =>
        request as TRequest ?? throw new ArgumentException("The request does not match the probe operation.", nameof(request));

    // Observation code reads typed outputs; a contradictory shape is recorded, never thrown.
    private static IReadOnlyDictionary<string, string> Observe(Func<IReadOnlyDictionary<string, string>?> observe)
    {
        try
        {
            return observe() ?? ProbeOutcome.EmptyObservations;
        }
        catch (InvalidOperationException)
        {
            return Observations.Of(("observation", "unreadable-output-shape"));
        }
        catch (ArgumentException)
        {
            return Observations.Of(("observation", "unreadable-output-shape"));
        }
    }
}

/// <summary>Builds deterministic structural observation maps.</summary>
internal static class Observations
{
    public static IReadOnlyDictionary<string, string> Of(params (string Key, string Value)[] entries)
    {
        var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in entries)
        {
            map[key] = value;
        }

        return map;
    }

    public static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Flag(bool value) => value ? "true" : "false";
}

/// <summary>Classifies public RPC results without retaining values or raw exception text.</summary>
internal static class PublicOutcomes
{
    public const string ErrorTrailerName = "briosa-operation-error-bin";

    public static ProbeOutcome FromSuccess(MpExecutionDetails? execution, IReadOnlyDictionary<string, string> observations)
    {
        if (execution is null || execution.State != MpExecutionState.Succeeded)
        {
            return ProbeOutcome.Unknown("grpc:OK", "success-without-succeeded-execution");
        }

        return FromExecution(ProbeOutcomeKind.Succeeded, "grpc:OK", execution, null, null, null, observations);
    }

    public static ProbeOutcome FromRpcException(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var transport = "grpc:" + exception.StatusCode.ToString();
        var trailer = exception.Trailers.FirstOrDefault(entry => entry.Key == ErrorTrailerName);
        OperationError? error = null;
        if (trailer is not null)
        {
            try
            {
                error = OperationError.Parser.ParseFrom(trailer.ValueBytes);
            }
            catch (InvalidProtocolBufferException)
            {
            }
        }

        if (error is null)
        {
            // Without a typed disposition the outcome cannot be classified safely.
            return ProbeOutcome.Unknown(transport, "untyped-rpc-failure");
        }

        var kind = Classify(error);
        return FromExecution(
            kind, transport, error.MpExecution, error.DiagnosticCode, error.Kind.ToString(),
            error.ExecutionDisposition.ToString(), ProbeOutcome.EmptyObservations);
    }

    internal static ProbeOutcomeKind Classify(OperationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error.ExecutionDisposition == ExecutionDisposition.StartedOutcomeUnknown ||
            error.ExecutionDisposition == ExecutionDisposition.Unspecified)
        {
            return ProbeOutcomeKind.Indeterminate;
        }

        if (error.ExecutionDisposition == ExecutionDisposition.NotStarted)
        {
            return error.Kind == OperationFailureKind.SdkArgumentRejected
                ? ProbeOutcomeKind.ArgumentRejected
                : ProbeOutcomeKind.NotStarted;
        }

        // The shipped server reports ExecuteStepRejected and MpResultRetrievalFailure
        // as StartedOutcomeUnknown (handled above). If a Completed disposition ever
        // carried them, their kinds are still completion-unknown, so no step accepts
        // them and the session stops without replay.
        return error.Kind switch
        {
            OperationFailureKind.SdkArgumentRejected => ProbeOutcomeKind.ArgumentRejected,
            OperationFailureKind.ExecuteStepRejected => ProbeOutcomeKind.ExecuteStepRejected,
            OperationFailureKind.MpFailure => ProbeOutcomeKind.MpFailed,
            OperationFailureKind.MpResultRetrievalFailure => ProbeOutcomeKind.MpResultUnavailable,
            OperationFailureKind.OutputRetrievalFailure => ProbeOutcomeKind.OutputRetrievalFailed,
            // Completed only when an output getter threw after MP success.
            OperationFailureKind.SdkCallFaulted => ProbeOutcomeKind.OutputRetrievalFailed,
            _ => ProbeOutcomeKind.Indeterminate
        };
    }

    private static ProbeOutcome FromExecution(
        ProbeOutcomeKind kind,
        string transport,
        MpExecutionDetails? execution,
        string? diagnosticCode,
        string? failureKind,
        string? disposition,
        IReadOnlyDictionary<string, string> observations)
    {
        bool? executeStepReturned = execution?.State switch
        {
            MpExecutionState.ArgumentRejected => null,
            MpExecutionState.ExecuteStepRejected => false,
            MpExecutionState.Succeeded or MpExecutionState.Failed or MpExecutionState.ResultUnavailable => true,
            _ => null
        };
        bool? mpResultRetrieved = execution?.State switch
        {
            MpExecutionState.Succeeded or MpExecutionState.Failed => true,
            MpExecutionState.ResultUnavailable => false,
            _ => null
        };
        var outputs = execution is null
            ? []
            : execution.OutputRetrievals
                .Select(output => new OutputObservation(output.FieldName, output.State == OutputRetrievalState.Retrieved))
                .ToList();
        return new ProbeOutcome(
            kind,
            transport,
            executeStepReturned,
            mpResultRetrieved,
            execution is { HasMpResultCode: true } ? execution.MpResultCode : null,
            outputs,
            SafeCode.OrNull(diagnosticCode),
            failureKind,
            disposition,
            observations);
    }
}

/// <summary>
/// Classifies worker control responses into the same structural outcome and the
/// same execution disposition that the shipped server reports for them.
/// </summary>
internal static class WorkerOutcomes
{
    public static ProbeOutcome FromResponse(
        WorkerExecutionResponse response,
        Func<IReadOnlyList<WorkerMpOutputValue>, IReadOnlyDictionary<string, string>> observe)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(observe);
        if (response.Status != WorkerExecutionResponseStatus.Completed || response.Execution is null)
        {
            return new ProbeOutcome(ProbeOutcomeKind.NotStarted, "worker:unavailable", null, null, null, [],
                SafeCode.OrNull(response.DiagnosticCode), null, "NotStarted", ProbeOutcome.EmptyObservations);
        }

        var execution = response.Execution;
        var outputs = execution.OutputValues
            .Select(output => new OutputObservation(output.Name, output.Retrieved))
            .ToList();
        var kind = execution switch
        {
            WorkerArgumentsRejected => ProbeOutcomeKind.ArgumentRejected,
            WorkerExecuteRejected => ProbeOutcomeKind.ExecuteStepRejected,
            WorkerMpResultUnavailable => ProbeOutcomeKind.MpResultUnavailable,
            WorkerMpOutputsUnavailable => ProbeOutcomeKind.OutputRetrievalFailed,
            WorkerSdkCallFaulted { Phase: WorkerSdkCallPhase.BeforeExecute } => ProbeOutcomeKind.NotStarted,
            WorkerSdkCallFaulted { Phase: WorkerSdkCallPhase.OutputGetter } => ProbeOutcomeKind.OutputRetrievalFailed,
            WorkerSdkCallFaulted => ProbeOutcomeKind.Indeterminate,
            WorkerMpResultAvailable { ResultCode: not 2 } => ProbeOutcomeKind.MpFailed,
            WorkerMpResultAvailable when outputs.Any(output => !output.Retrieved) => ProbeOutcomeKind.OutputRetrievalFailed,
            WorkerMpResultAvailable => ProbeOutcomeKind.Succeeded,
            _ => ProbeOutcomeKind.Indeterminate
        };
        var observations = kind == ProbeOutcomeKind.Succeeded
            ? observe(execution.OutputValues)
            : ProbeOutcome.EmptyObservations;
        return new ProbeOutcome(
            kind,
            "worker:completed",
            execution is WorkerArgumentsRejected or WorkerSdkCallFaulted { Phase: WorkerSdkCallPhase.BeforeExecute }
                ? null
                : execution.ExecuteStepReturned,
            execution.ExecuteStepReturned ? execution.MpResultRetrieved : null,
            execution.MpResultCode,
            outputs,
            SafeCode.OrNull(execution.DiagnosticCode),
            null,
            DispositionOf(kind),
            observations);
    }

    // Mirrors the shipped server's GrpcOperationOutcomeMapper: a rejected argument
    // or an SDK fault before ExecuteStep never started; ExecuteStep false after the
    // setters, a missing MP result after ExecuteStep true, or an SDK fault in either
    // call leaves completion unknown; an MP code means completed.
    internal static string DispositionOf(ProbeOutcomeKind kind) => kind switch
    {
        ProbeOutcomeKind.ArgumentRejected or ProbeOutcomeKind.NotStarted => "NotStarted",
        ProbeOutcomeKind.Succeeded or ProbeOutcomeKind.MpFailed or ProbeOutcomeKind.OutputRetrievalFailed => "Completed",
        _ => ProbeOutcome.StartedOutcomeUnknown
    };
}

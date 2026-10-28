using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal transport exception must always contain typed lifecycle detail.")]
internal sealed class SdkLifecycleException : Exception
{
    private SdkLifecycleException(
        Grpc.Core.StatusCode statusCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance)
        : base(diagnosticCode)
    {
        StatusCode = statusCode;
        Detail = new global::Briosa.SpatialAnalyzerSdkLifecycleError
        {
            Kind = kind,
            DiagnosticCode = diagnosticCode,
            State = state,
            RecoveryGuidance = recoveryGuidance
        };
    }

    public Grpc.Core.StatusCode StatusCode { get; }

    public global::Briosa.SpatialAnalyzerSdkLifecycleError Detail { get; }

    public static SdkLifecycleException InvalidArgument(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state) =>
        new(Grpc.Core.StatusCode.InvalidArgument, kind, diagnosticCode, state,
            global::Briosa.LifecycleRecoveryGuidance.RefreshState);

    public static SdkLifecycleException FailedPrecondition(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.FailedPrecondition, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SdkLifecycleException NotFound(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.NotFound, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SdkLifecycleException Aborted(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Aborted, kind, diagnosticCode, state, recoveryGuidance);

    public static SdkLifecycleException Unavailable(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Unavailable, kind, diagnosticCode, state,
            recoveryGuidance);

    // The caller stopped waiting (cancellation or its gRPC deadline). gRPC does not
    // deliver a response to such a caller, so the detail serves in-process callers and
    // the audit. The diagnostic says whether the request was withdrawn before
    // acceptance or continues detached. A cancellation token does not say why it was
    // cancelled, so the kind is CALLER_CANCELLED; the gRPC service reports
    // CALLER_DEADLINE_EXCEEDED instead once the call's deadline has passed.
    public static SdkLifecycleException Cancelled(
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Cancelled,
            global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.CallerCancelled,
            diagnosticCode, state, recoveryGuidance);

    // The same caller interruption, ended by the call's gRPC deadline: DEADLINE_EXCEEDED
    // with the same diagnostic, state, and guidance. Any other failure is the
    // lifecycle's own outcome and stays as it is.
    public SdkLifecycleException AsCallerDeadlineExceeded() =>
        Detail.Kind == global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.CallerCancelled
            ? new SdkLifecycleException(Grpc.Core.StatusCode.DeadlineExceeded,
                global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind.CallerDeadlineExceeded,
                Detail.DiagnosticCode, Detail.State, Detail.RecoveryGuidance)
            : this;

    public static SdkLifecycleException DeadlineExceeded(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.DeadlineExceeded, kind, diagnosticCode, state,
            recoveryGuidance);
}

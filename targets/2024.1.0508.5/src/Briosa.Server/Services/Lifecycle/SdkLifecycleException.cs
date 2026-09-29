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

    public static SdkLifecycleException DeadlineExceeded(
        global::Briosa.SpatialAnalyzerSdkLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerSdkLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.DeadlineExceeded, kind, diagnosticCode, state,
            recoveryGuidance);
}

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Briosa.Server.Services;

[SuppressMessage(
    "Design",
    "CA1032:Implement standard exception constructors",
    Justification = "This internal transport exception must always contain typed lifecycle detail.")]
internal sealed class SpatialAnalyzerLifecycleException : Exception
{
    private SpatialAnalyzerLifecycleException(
        Grpc.Core.StatusCode statusCode,
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance)
        : base(diagnosticCode)
    {
        StatusCode = statusCode;
        Detail = new global::Briosa.SpatialAnalyzerLifecycleError
        {
            Kind = kind,
            DiagnosticCode = diagnosticCode,
            State = state,
            RecoveryGuidance = recoveryGuidance
        };
    }

    public Grpc.Core.StatusCode StatusCode { get; }

    public global::Briosa.SpatialAnalyzerLifecycleError Detail { get; }

    public static SpatialAnalyzerLifecycleException InvalidArgument(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state) =>
        new(Grpc.Core.StatusCode.InvalidArgument, kind, diagnosticCode, state,
            global::Briosa.LifecycleRecoveryGuidance.RefreshState);

    public static SpatialAnalyzerLifecycleException NotFound(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.NotFound, kind, diagnosticCode, state, recoveryGuidance);

    public static SpatialAnalyzerLifecycleException FailedPrecondition(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.FailedPrecondition, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SpatialAnalyzerLifecycleException Aborted(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Aborted, kind, diagnosticCode, state, recoveryGuidance);

    public static SpatialAnalyzerLifecycleException Unavailable(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.Unavailable, kind, diagnosticCode, state,
            recoveryGuidance);

    public static SpatialAnalyzerLifecycleException DeadlineExceeded(
        global::Briosa.SpatialAnalyzerLifecycleFailureKind kind,
        string diagnosticCode,
        global::Briosa.SpatialAnalyzerLifecycleState state,
        global::Briosa.LifecycleRecoveryGuidance recoveryGuidance) =>
        new(Grpc.Core.StatusCode.DeadlineExceeded, kind, diagnosticCode, state,
            recoveryGuidance);
}

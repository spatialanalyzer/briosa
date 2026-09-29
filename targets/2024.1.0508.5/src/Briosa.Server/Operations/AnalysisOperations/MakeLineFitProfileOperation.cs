using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class MakeLineFitProfileOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.make_line_fit_profile", "Make Line Fit Profile",
        "briosa.AnalysisOperations", "MakeLineFitProfile", "/briosa.AnalysisOperations/MakeLineFitProfile",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.MakeLineFitProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Fit Profile Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasFitProfileName ? request.FitProfileName : string.Empty), "SetStringArg"),
                new("Reverse Normal Vector after fit?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(request.HasReverseNormalVectorAfterFit && request.ReverseNormalVectorAfterFit), "SetBoolArg"),
                new("Make Cardinal Points?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasMakeCardinalPoints || request.MakeCardinalPoints), "SetBoolArg"),
                new("Cardinal Pt.1: Point A?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt1PointA || request.CardinalPt1PointA), "SetBoolArg"),
                new("Cardinal Pt.2: Point B?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt2PointB || request.CardinalPt2PointB), "SetBoolArg"),
                new("Cardinal Pt.3: Mid Point?", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasCardinalPt3MidPoint || request.CardinalPt3MidPoint), "SetBoolArg")
            ], []);
    }

    public static Api.MakeLineFitProfileResult CreateResult(SuccessfulOperationExecution completed) => new() { Execution = completed.Details };
}

using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetLinePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_line_properties", "Set Line Properties",
        "briosa.AnalysisOperations", "SetLineProperties", "/briosa.AnalysisOperations/SetLineProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetLinePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Line Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.LineName, "line_name"), "SetCollectionObjectNameArg2"),
                new("Begin Coordinate", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.BeginCoordinate, "begin_coordinate"), "SetVectorArg"),
                new("End Coordinate", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.EndCoordinate, "end_coordinate"), "SetVectorArg"),
                new("Length (optional)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasLength ? request.Length : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.SetLinePropertiesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}

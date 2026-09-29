using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtLineMidpointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_line_midpoint", "Construct a Point at line MidPoint",
        "briosa.ConstructionOperations", "ConstructPointAtLineMidpoint", "/briosa.ConstructionOperations/ConstructPointAtLineMidpoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtLineMidpointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Line Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.LineName, "line_name", WorkerObjectTypeValue.Line), "SetCollectionObjectNameArg2"),
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointAtLineMidpointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

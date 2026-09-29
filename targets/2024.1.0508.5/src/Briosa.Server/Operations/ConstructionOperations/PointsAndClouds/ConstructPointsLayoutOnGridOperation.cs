using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointsLayoutOnGridOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_points_layout_on_grid", "Construct Points Layout on Grid",
        "briosa.ConstructionOperations", "ConstructPointsLayoutOnGrid", "/briosa.ConstructionOperations/ConstructPointsLayoutOnGrid",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointsLayoutOnGridRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.GroupName, "group_name"), "SetCollectionObjectNameArg2"),
            new("Point Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointPrefix ? request.PointPrefix : "p"), "SetStringArg"),
            new("X Min", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasXMin ? request.XMin : 0), "SetDoubleArg"),
            new("X Max", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasXMax ? request.XMax : 100), "SetDoubleArg"),
            new("X Count", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasXCount ? request.XCount : 10), "SetIntegerArg"),
            new("Y Min", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasYMin ? request.YMin : 0), "SetDoubleArg"),
            new("Y Max", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasYMax ? request.YMax : 50), "SetDoubleArg"),
            new("Y Count", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasYCount ? request.YCount : 10), "SetIntegerArg"),
            new("Z Min", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasZMin ? request.ZMin : 0), "SetDoubleArg"),
            new("Z Max", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasZMax ? request.ZMax : 0), "SetDoubleArg"),
            new("Z Count", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasZCount ? request.ZCount : 1), "SetIntegerArg")
        ], []);
    }

    public static Api.ConstructPointsLayoutOnGridResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

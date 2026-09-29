using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointFitToPointsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_fit_to_points", "Construct Point (Fit to Points)",
        "briosa.ConstructionOperations", "ConstructPointFitToPoints", "/briosa.ConstructionOperations/ConstructPointFitToPoints",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointFitToPointsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
            new("Resulting Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultingPointName, "resulting_point_name"), "SetPointNameArg")
        ], []);
    }

    public static Api.ConstructPointFitToPointsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

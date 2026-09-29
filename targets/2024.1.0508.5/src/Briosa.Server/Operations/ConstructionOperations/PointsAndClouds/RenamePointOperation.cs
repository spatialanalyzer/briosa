using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class RenamePointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.rename_point", "Rename Point",
        "briosa.ConstructionOperations", "RenamePoint", "/briosa.ConstructionOperations/RenamePoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenamePointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Original Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.OriginalPointName, "original_point_name"), "SetPointNameArg"),
            new("New Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.NewPointName, "new_point_name"), "SetPointNameArg"),
            new("Overwrite if exists?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.OverwriteIfExists), "SetBoolArg")
        ], []);
    }

    public static Api.RenamePointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

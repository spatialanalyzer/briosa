using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreateHiddenPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_hidden_point", "Create Hidden Point",
        "briosa.ConstructionOperations", "CreateHiddenPoint", "/briosa.ConstructionOperations/CreateHiddenPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.CreateHiddenPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("End A Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.EndAPointName, "end_a_point_name"), "SetPointNameArg"),
            new("End B Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.EndBPointName, "end_b_point_name"), "SetPointNameArg"),
            new("Hidden Point Rod Index", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasHiddenPointRodIndex ? request.HiddenPointRodIndex : 0), "SetIntegerArg"),
            new("Overwrite existing point?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasOverwriteExistingPoint && request.OverwriteExistingPoint), "SetBoolArg"),
            new("Point Name To Create", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointNameToCreate, "point_name_to_create"), "SetPointNameArg")
        ], []);
    }

    public static Api.CreateHiddenPointResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

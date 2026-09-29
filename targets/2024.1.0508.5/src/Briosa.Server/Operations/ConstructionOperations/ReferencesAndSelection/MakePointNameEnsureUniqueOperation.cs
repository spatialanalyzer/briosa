using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class MakePointNameEnsureUniqueOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.make_point_name_ensure_unique", "Make a Point Name - Ensure Unique",
        "briosa.ConstructionOperations", "MakePointNameEnsureUnique", "/briosa.ConstructionOperations/MakePointNameEnsureUnique",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("resultant_point_name", "Point Name", WorkerMpValueKind.PointName)];

    public static WorkerMpCommand CreateCommand(Api.MakePointNameEnsureUniqueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointName, "point_name"), "SetPointNameArg"),
            new("Use Number Suffix?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.UseNumberSuffix), "SetBoolArg")
        ], [new("Point Name", WorkerMpValueKind.PointName, "GetPointNameArg")]);
    }

    public static Api.MakePointNameEnsureUniqueResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            ResultantPointName = PointNameMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerPointNameValue>()),
            Execution = completed.Details
        };
}

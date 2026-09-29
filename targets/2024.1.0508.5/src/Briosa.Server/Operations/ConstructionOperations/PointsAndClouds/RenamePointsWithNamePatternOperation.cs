using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class RenamePointsWithNamePatternOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.rename_points_with_name_pattern", "Rename Points with Name Pattern",
        "briosa.ConstructionOperations", "RenamePointsWithNamePattern", "/briosa.ConstructionOperations/RenamePointsWithNamePattern",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RenamePointsWithNamePatternRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point Names", WorkerMpValueKind.PointNameList,
                PointNameMapper.RequiredList(request.PointNames, "point_names"), "SetPointNameRefListArg"),
            new("Name Pattern", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasNamePattern ? request.NamePattern : "NewName_%d"), "SetStringArg"),
            new("Start Value", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.HasStartValue ? request.StartValue : 1), "SetIntegerArg")
        ], []);
    }

    public static Api.RenamePointsWithNamePatternResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

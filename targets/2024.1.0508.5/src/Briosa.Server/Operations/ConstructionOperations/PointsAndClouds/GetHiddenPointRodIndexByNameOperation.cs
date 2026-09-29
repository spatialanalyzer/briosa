using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class GetHiddenPointRodIndexByNameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.get_hidden_point_rod_index_by_name", "Get Hidden Point Rod Index by Name",
        "briosa.ConstructionOperations", "GetHiddenPointRodIndexByName", "/briosa.ConstructionOperations/GetHiddenPointRodIndexByName",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("hidden_point_rod_index", "Hidden Point Rod Index", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetHiddenPointRodIndexByNameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Hidden Point Rod Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasHiddenPointRodName ? request.HiddenPointRodName : string.Empty), "SetStringArg")
        ], [new("Hidden Point Rod Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.GetHiddenPointRodIndexByNameResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            HiddenPointRodIndex = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
}

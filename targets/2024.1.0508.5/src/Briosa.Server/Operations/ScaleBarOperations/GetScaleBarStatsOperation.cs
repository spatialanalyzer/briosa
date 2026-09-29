using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ScaleBarOperations;

internal static class GetScaleBarStatsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "scale_bar_operations.get_scale_bar_stats", "Get Scale Bar Stats",
        "briosa.ScaleBarOperations", "GetScaleBarStats", "/briosa.ScaleBarOperations/GetScaleBarStats",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("nominal_length", "Nominal Length", WorkerMpValueKind.FloatingPoint),
        new("actual_length", "Actual Length", WorkerMpValueKind.FloatingPoint),
        new("deviation", "Deviation", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetScaleBarStatsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Scale Bar Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ScaleBarName, "scale_bar_name"), "SetCollectionObjectNameArg2")],
            [
                new("Nominal Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Actual Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetScaleBarStatsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            NominalLength = values[0].RequireValue<WorkerDoubleValue>().Value,
            ActualLength = values[1].RequireValue<WorkerDoubleValue>().Value,
            Deviation = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}

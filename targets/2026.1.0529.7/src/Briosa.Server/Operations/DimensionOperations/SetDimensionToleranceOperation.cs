using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.DimensionOperations;

internal static class SetDimensionToleranceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "dimension_operations.set_dimension_tolerance", "Set Dimension Tolerance",
        "briosa.DimensionOperations", "SetDimensionTolerance", "/briosa.DimensionOperations/SetDimensionTolerance",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetDimensionToleranceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Dimension Name", WorkerMpValueKind.CollectionItemName,
                    CollectionItemNameMapper.Required(request.DimensionName, "dimension_name"), "SetCollectionObjectNameArg2"),
                new("Enable Nominal", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.EnableNominal), "SetBoolArg"),
                new("Enable High", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.EnableHigh), "SetBoolArg"),
                new("Enable Low", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.EnableLow), "SetBoolArg"),
                new("Nominal", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.Nominal), "SetDoubleArg"),
                new("High Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.HighTolerance), "SetDoubleArg"),
                new("Low Tolerance", WorkerMpValueKind.FloatingPoint, new WorkerDoubleValue(request.LowTolerance), "SetDoubleArg")
            ], []);
    }

    public static Api.SetDimensionToleranceResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

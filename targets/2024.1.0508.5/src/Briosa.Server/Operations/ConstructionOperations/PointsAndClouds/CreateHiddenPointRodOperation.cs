using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class CreateHiddenPointRodOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.create_hidden_point_rod", "Create Hidden Point Rod",
        "briosa.ConstructionOperations", "CreateHiddenPointRod", "/briosa.ConstructionOperations/CreateHiddenPointRod",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("hidden_point_rod_index", "Hidden Point Rod Index", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.CreateHiddenPointRodRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Hidden Point Rod Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasHiddenPointRodName ? request.HiddenPointRodName : string.Empty), "SetStringArg"),
            new("A to B (Target to Target) Distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasAToBDistance ? request.AToBDistance : 0), "SetDoubleArg"),
            new("A to C (Target to Tip) Distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasAToCDistance ? request.AToCDistance : 0), "SetDoubleArg"),
            new("A to B Inter-point Tolerance (0.0 for none)", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasInterPointTolerance ? request.InterPointTolerance : 0), "SetDoubleArg")
        ], [new("Hidden Point Rod Index", WorkerMpValueKind.WholeNumber, "GetIntegerArg")]);
    }

    public static Api.CreateHiddenPointRodResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            HiddenPointRodIndex = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
            Execution = completed.Details
        };
}

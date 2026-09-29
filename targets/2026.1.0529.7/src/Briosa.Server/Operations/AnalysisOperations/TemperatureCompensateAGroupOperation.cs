using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class TemperatureCompensateAGroupOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.temperature_compensate_a_group", "Temperature Compensate a group",
        "briosa.AnalysisOperations", "TemperatureCompensateAGroup", "/briosa.AnalysisOperations/TemperatureCompensateAGroup",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.TemperatureCompensateAGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Original Group", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.OriginalGroup, "original_group"), "SetCollectionObjectNameArg2"),
                new("Scaling Origin (coordinate frame)", WorkerMpValueKind.FrameName,
                    FrameNameMapper.Required(request.ScalingOrigin, "scaling_origin"), "SetFrameNameArg"),
                new("Material CTE (1/Deg F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.MaterialCte), "SetDoubleArg"),
                new("Initial Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.InitialTemperature), "SetDoubleArg"),
                new("Final Temperature (F)", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.FinalTemperature), "SetDoubleArg"),
                new("Scaled Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ScaledGroupName, "scaled_group_name"), "SetCollectionObjectNameArg2")
            ], []);
    }

    public static Api.TemperatureCompensateAGroupResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}

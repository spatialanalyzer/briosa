using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class GetWorkingTransformOfObjectFixedXyzOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.get_working_transform_of_object_fixed_xyz",
        "Get Working Transform of Object (Fixed XYZ)", "briosa.ConstructionOperations",
        "GetWorkingTransformOfObjectFixedXyz", "/briosa.ConstructionOperations/GetWorkingTransformOfObjectFixedXyz",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("transform", "Transform", WorkerMpValueKind.Transform)];

    public static WorkerMpCommand CreateCommand(Api.GetWorkingTransformOfObjectFixedXyzRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectName, "object_name"), "SetCollectionObjectNameArg2")],
            [new("Transform", WorkerMpValueKind.Transform, "GetTransformArg")]);
    }

    public static Api.GetWorkingTransformOfObjectFixedXyzResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Transform = TransformMapper.ToProtocol(completed.Execution.OutputValues[0]
                .RequireValue<WorkerTransformValue>()),
            Execution = completed.Details
        };
}

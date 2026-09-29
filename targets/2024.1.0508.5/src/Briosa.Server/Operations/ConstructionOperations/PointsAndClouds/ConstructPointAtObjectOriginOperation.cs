using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointAtObjectOriginOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_at_object_origin", "Construct Point at Object Origin",
        "briosa.ConstructionOperations", "ConstructPointAtObjectOrigin", "/briosa.ConstructionOperations/ConstructPointAtObjectOrigin",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("origin.vector_representation", "Vector Representation", WorkerMpValueKind.Vector),
        new("origin.x_value", "X Value", WorkerMpValueKind.FloatingPoint),
        new("origin.y_value", "Y Value", WorkerMpValueKind.FloatingPoint),
        new("origin.z_value", "Z Value", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointAtObjectOriginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Object Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ObjectName, "object_name", WorkerObjectTypeValue.Any), "SetCollectionObjectNameArg2"),
            new("Resultant Point Name", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.ResultantPointName, "resultant_point_name"), "SetPointNameArg")
        ],
        [
            new("Vector Representation", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("X Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Y Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Z Value", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
        ]);
    }

    public static Api.ConstructPointAtObjectOriginResult CreateResult(SuccessfulOperationExecution completed) =>
        new()
        {
            Origin = new Api.ObjectOriginResult
            {
                VectorRepresentation = VectorMapper.ToProtocol(completed.Execution.OutputValues[0].RequireValue<WorkerVectorValue>()),
                XValue = completed.Execution.OutputValues[1].RequireValue<WorkerDoubleValue>().Value,
                YValue = completed.Execution.OutputValues[2].RequireValue<WorkerDoubleValue>().Value,
                ZValue = completed.Execution.OutputValues[3].RequireValue<WorkerDoubleValue>().Value
            },
            Execution = completed.Details
        };
}

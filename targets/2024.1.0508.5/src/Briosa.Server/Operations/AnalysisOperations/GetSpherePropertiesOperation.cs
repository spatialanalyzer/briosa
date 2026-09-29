using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetSpherePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_sphere_properties", "Get Sphere Properties",
        "briosa.AnalysisOperations", "GetSphereProperties", "/briosa.AnalysisOperations/GetSphereProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("center_coordinate", "Center Coordinate", WorkerMpValueKind.Vector),
        new("radius", "Radius", WorkerMpValueKind.FloatingPoint),
        new("diameter", "Diameter", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetSpherePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Sphere Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SphereName, "sphere_name"), "SetCollectionObjectNameArg2")],
            [
                new("Center Coordinate", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Radius", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Diameter", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetSpherePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            CenterCoordinate = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            Radius = values[1].RequireValue<WorkerDoubleValue>().Value,
            Diameter = values[2].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}

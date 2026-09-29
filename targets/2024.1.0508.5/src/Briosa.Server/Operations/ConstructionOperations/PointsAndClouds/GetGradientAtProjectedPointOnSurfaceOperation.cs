using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class GetGradientAtProjectedPointOnSurfaceOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.get_gradient_at_projected_point_on_surface", "Get Gradient At Projected Point On Surface",
        "briosa.ConstructionOperations", "GetGradientAtProjectedPointOnSurface",
        "/briosa.ConstructionOperations/GetGradientAtProjectedPointOnSurface",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("gradient.projected_point", "Projected Point", WorkerMpValueKind.Vector),
        new("gradient.normal_vector", "Normal Vector", WorkerMpValueKind.Vector),
        new("gradient.u_direction", "U Direction", WorkerMpValueKind.Vector),
        new("gradient.v_direction", "V Direction", WorkerMpValueKind.Vector)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetGradientAtProjectedPointOnSurfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Point to Project", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.PointToProject, "point_to_project"), "SetPointNameArg"),
            new("Surface Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SurfaceName, "surface_name", WorkerObjectTypeValue.Surface), "SetCollectionObjectNameArg2"),
            new("Generate output vector lines?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasGenerateOutputVectorLines && request.GenerateOutputVectorLines), "SetBoolArg")
        ],
        [
            new("Projected Point", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("Normal Vector", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("U Direction", WorkerMpValueKind.Vector, "GetVectorArg"),
            new("V Direction", WorkerMpValueKind.Vector, "GetVectorArg")
        ]);
    }

    public static Api.GetGradientAtProjectedPointOnSurfaceResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Gradient = new Api.ProjectedPointGradient
            {
                ProjectedPoint = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
                NormalVector = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
                UDirection = VectorMapper.ToProtocol(values[2].RequireValue<WorkerVectorValue>()),
                VDirection = VectorMapper.ToProtocol(values[3].RequireValue<WorkerVectorValue>())
            },
            Execution = completed.Details
        };
    }
}

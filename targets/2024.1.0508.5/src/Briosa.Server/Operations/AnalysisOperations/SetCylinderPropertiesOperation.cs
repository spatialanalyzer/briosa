using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetCylinderPropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_cylinder_properties", "Set Cylinder Properties",
        "briosa.AnalysisOperations", "SetCylinderProperties", "/briosa.AnalysisOperations/SetCylinderProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetCylinderPropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Cylinder Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.CylinderName, "cylinder_name"), "SetCollectionObjectNameArg2"),
                new("Begin Coordinate", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.BeginCoordinate, "begin_coordinate"), "SetVectorArg"),
                new("Axis Direction", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.AxisDirection, "axis_direction"), "SetVectorArg"),
                new("Length", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasLength ? request.Length : 0d), "SetDoubleArg"),
                new("Diameter", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasDiameter ? request.Diameter : 0d), "SetDoubleArg"),
                new("Nominals Point Inward", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasNominalsPointInward || request.NominalsPointInward), "SetBoolArg"),
                new("Facets", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasFacets ? request.Facets : 32), "SetIntegerArg"),
                new("Enable Theta Extent Display Mode", WorkerMpValueKind.Logical,
                    new WorkerBooleanValue(!request.HasEnableThetaExtentDisplayMode || request.EnableThetaExtentDisplayMode), "SetBoolArg"),
                new("Theta Start in Degrees", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasThetaStartInDegrees ? request.ThetaStartInDegrees : 0d), "SetDoubleArg"),
                new("Theta Span in Degrees", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasThetaSpanInDegrees ? request.ThetaSpanInDegrees : 360d), "SetDoubleArg")
            ], []);
    }

    public static Api.SetCylinderPropertiesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}

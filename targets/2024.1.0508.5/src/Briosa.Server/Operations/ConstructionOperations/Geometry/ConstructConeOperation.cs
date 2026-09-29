using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructConeOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_cone", "Construct Cone",
        "briosa.ConstructionOperations", "ConstructCone", "/briosa.ConstructionOperations/ConstructCone",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructConeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cone Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ConeName, "cone_name", WorkerObjectTypeValue.Cone), "SetCollectionObjectNameArg2"),
            new("Cone End Point (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.ConeEndPoint, "cone_end_point"), "SetVectorArg"),
            new("Cone Axis (in working coordinates)", WorkerMpValueKind.Vector,
                VectorMapper.Required(request.ConeAxis, "cone_axis"), "SetVectorArg"),
            new("Cone Length", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasConeLength ? request.ConeLength : 0), "SetDoubleArg"),
            new("Cone Theta Start", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasConeThetaStart ? request.ConeThetaStart : 0), "SetDoubleArg"),
            new("Cone Theta Span", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasConeThetaSpan ? request.ConeThetaSpan : 0), "SetDoubleArg"),
            new("Cone Included Angle", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasConeIncludedAngle ? request.ConeIncludedAngle : 0), "SetDoubleArg")
        ], []);
    }

    public static Api.ConstructConeResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

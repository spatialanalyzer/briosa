using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class SetConePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.set_cone_properties", "Set Cone Properties",
        "briosa.AnalysisOperations", "SetConeProperties", "/briosa.AnalysisOperations/SetConeProperties",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetConePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Cone Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.ConeName, "cone_name"), "SetCollectionObjectNameArg2"),
                new("Cone End Point (in working coordinates)", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.ConeEndPoint, "cone_end_point"), "SetVectorArg"),
                new("Cone Axis (in working coordinates)", WorkerMpValueKind.Vector,
                    VectorMapper.Required(request.ConeAxis, "cone_axis"), "SetVectorArg"),
                new("Cone Length", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasConeLength ? request.ConeLength : 0d), "SetDoubleArg"),
                new("Cone Theta Start", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasConeThetaStart ? request.ConeThetaStart : 0d), "SetDoubleArg"),
                new("Cone Theta Span", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasConeThetaSpan ? request.ConeThetaSpan : 0d), "SetDoubleArg"),
                new("Cone Included Angle", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasConeIncludedAngle ? request.ConeIncludedAngle : 0d), "SetDoubleArg"),
                new("Cut Length from Apex", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasCutLengthFromApex ? request.CutLengthFromApex : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.SetConePropertiesResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}

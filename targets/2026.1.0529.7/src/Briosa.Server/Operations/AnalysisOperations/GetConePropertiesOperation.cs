using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class GetConePropertiesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.get_cone_properties", "Get Cone Properties",
        "briosa.AnalysisOperations", "GetConeProperties", "/briosa.AnalysisOperations/GetConeProperties",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("cone_end_point", "Cone End Point (in working coordinates)", WorkerMpValueKind.Vector),
        new("cone_axis", "Cone Axis (in working coordinates)", WorkerMpValueKind.Vector),
        new("cone_length", "Cone Length", WorkerMpValueKind.FloatingPoint),
        new("cone_theta_start", "Cone Theta Start", WorkerMpValueKind.FloatingPoint),
        new("cone_theta_span", "Cone Theta Span", WorkerMpValueKind.FloatingPoint),
        new("cone_included_angle", "Cone Included Angle", WorkerMpValueKind.FloatingPoint),
        new("cut_length_from_apex", "Cut Length from Apex", WorkerMpValueKind.FloatingPoint)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetConePropertiesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Cone Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.ConeName, "cone_name"), "SetCollectionObjectNameArg2")],
            [
                new("Cone End Point (in working coordinates)", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Cone Axis (in working coordinates)", WorkerMpValueKind.Vector, "GetVectorArg"),
                new("Cone Length", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Cone Theta Start", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Cone Theta Span", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Cone Included Angle", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
                new("Cut Length from Apex", WorkerMpValueKind.FloatingPoint, "GetDoubleArg")
            ]);
    }

    public static Api.GetConePropertiesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            ConeEndPoint = VectorMapper.ToProtocol(values[0].RequireValue<WorkerVectorValue>()),
            ConeAxis = VectorMapper.ToProtocol(values[1].RequireValue<WorkerVectorValue>()),
            ConeLength = values[2].RequireValue<WorkerDoubleValue>().Value,
            ConeThetaStart = values[3].RequireValue<WorkerDoubleValue>().Value,
            ConeThetaSpan = values[4].RequireValue<WorkerDoubleValue>().Value,
            ConeIncludedAngle = values[5].RequireValue<WorkerDoubleValue>().Value,
            CutLengthFromApex = values[6].RequireValue<WorkerDoubleValue>().Value,
            Execution = completed.Details
        };
    }
}

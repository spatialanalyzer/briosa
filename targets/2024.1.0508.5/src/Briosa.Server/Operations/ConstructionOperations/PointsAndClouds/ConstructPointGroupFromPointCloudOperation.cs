using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ConstructionOperations;

internal static class ConstructPointGroupFromPointCloudOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "construction_operations.construct_point_group_from_point_cloud", "Construct Point Group from Point Cloud",
        "briosa.ConstructionOperations", "ConstructPointGroupFromPointCloud", "/briosa.ConstructionOperations/ConstructPointGroupFromPointCloud",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.ConstructPointGroupFromPointCloudRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.CloudName, "cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Point Group Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.PointGroupName, "point_group_name", WorkerObjectTypeValue.PointGroup), "SetCollectionObjectNameArg2"),
            new("Point Prefix", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasPointPrefix ? request.PointPrefix : "pt"), "SetStringArg"),
            new("Starting Point Number", WorkerMpValueKind.WholeNumber,
                new WorkerIntegerValue(request.StartingPointNumber), "SetIntegerArg"),
            new("Point Offset", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.PointOffset), "SetDoubleArg"),
            new("Sub-Sampling?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.SubSampling), "SetBoolArg"),
            new("Sub-Sampling Distance", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasSubSamplingDistance ? request.SubSamplingDistance : 0.5), "SetDoubleArg"),
            new("Show Progress?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.ShowProgress), "SetBoolArg")
        ], []);
    }

    public static Api.ConstructPointGroupFromPointCloudResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

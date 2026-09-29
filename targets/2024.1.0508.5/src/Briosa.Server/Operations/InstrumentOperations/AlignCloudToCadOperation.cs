using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.InstrumentOperations;

internal static class AlignCloudToCadOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "instrument_operations.align_cloud_to_cad", "Align Cloud to CAD",
        "briosa.InstrumentOperations", "AlignCloudToCad", "/briosa.InstrumentOperations/AlignCloudToCad",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("alignment.rms_deviation", "RMS Deviation", WorkerMpValueKind.FloatingPoint),
        new("alignment.average_deviation", "Average Deviation", WorkerMpValueKind.FloatingPoint),
        new("alignment.maximum_absolute_deviation", "Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint),
        new("alignment.resultant_transform_in_working", "Resultant Transform in Working Frame", WorkerMpValueKind.Transform)
    ];

    public static WorkerMpCommand CreateCommand(Api.AlignCloudToCadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.Cloud, "cloud", WorkerObjectTypeValue.Cloud),
                "SetCollectionObjectNameArg2"),
            new("Surfaces", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.Surfaces, "surfaces"),
                "SetCollectionObjectNameRefListArg"),
            new("Maximum Coarse CAD Mesh Edge Length", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.MaximumCoarseCadMeshEdgeLength), "SetDoubleArg"),
            new("Use Fine CAD Mesh (50% of Coarse)?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.UseFineCadMesh), "SetBoolArg"),
            new("Execute Alignment?", WorkerMpValueKind.Logical,
                new WorkerBooleanValue(request.HasExecuteAlignment ? request.ExecuteAlignment : true), "SetBoolArg")
        ],
        [
            new("RMS Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Average Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Maximum Absolute Deviation", WorkerMpValueKind.FloatingPoint, "GetDoubleArg"),
            new("Resultant Transform in Working Frame", WorkerMpValueKind.Transform, "GetTransformArg")
        ]);
    }

    public static Api.AlignCloudToCadResult CreateResult(SuccessfulOperationExecution completed)
    {
        var values = completed.Execution.OutputValues;
        return new()
        {
            Alignment = new Api.CloudToCadAlignmentResult
            {
                RmsDeviation = values[0].RequireValue<WorkerDoubleValue>().Value,
                AverageDeviation = values[1].RequireValue<WorkerDoubleValue>().Value,
                MaximumAbsoluteDeviation = values[2].RequireValue<WorkerDoubleValue>().Value,
                ResultantTransformInWorking = TransformMapper.ToProtocol(
                    values[3].RequireValue<WorkerTransformValue>())
            },
            Execution = completed.Details
        };
    }
}

using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class GetCloudRGBValuesNearPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.get_cloud_rgb_values_near_point", "Get Cloud RGB Values Near Point",
        "briosa.CloudAndMeshOperations", "GetCloudRGBValuesNearPoint", "/briosa.CloudAndMeshOperations/GetCloudRGBValuesNearPoint",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("low_value", "Low Value", WorkerMpValueKind.WholeNumber),
        new("high_value", "High Value", WorkerMpValueKind.WholeNumber),
        new("average_value", "Average Value", WorkerMpValueKind.WholeNumber),
        new("standard_deviation", "Standard Deviation", WorkerMpValueKind.WholeNumber)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetCloudRGBValuesNearPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var channel = request.HasRgbColorChannel
            ? request.RgbColorChannel
            : Api.RGBColorChannel.Intensity;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Source Cloud Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.SourceCloudName, "source_cloud_name", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameArg2"),
            new("Single Point", WorkerMpValueKind.PointName,
                PointNameMapper.Required(request.SinglePoint, "single_point"), "SetPointNameArg"),
            new("Diameter", WorkerMpValueKind.FloatingPoint,
                new WorkerDoubleValue(request.HasDiameter ? request.Diameter : 10), "SetDoubleArg"),
            new("RGB Color Channel", WorkerMpValueKind.Text, RgbColorChannelMapper.ToMpValue(channel), "SetStringArg")
        ],
        [
            new("Low Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("High Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Average Value", WorkerMpValueKind.WholeNumber, "GetIntegerArg"),
            new("Standard Deviation", WorkerMpValueKind.WholeNumber, "GetIntegerArg")
        ]);
    }

    public static Api.GetCloudRGBValuesNearPointResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details,
        LowValue = completed.Execution.OutputValues[0].RequireValue<WorkerIntegerValue>().Value,
        HighValue = completed.Execution.OutputValues[1].RequireValue<WorkerIntegerValue>().Value,
        AverageValue = completed.Execution.OutputValues[2].RequireValue<WorkerIntegerValue>().Value,
        StandardDeviation = completed.Execution.OutputValues[3].RequireValue<WorkerIntegerValue>().Value
    };
}

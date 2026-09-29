using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class RGBCloudPointFilterOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "cloud_and_mesh_operations.rgb_cloud_point_filter", "RGB Cloud Point Filter",
        "briosa.CloudAndMeshOperations", "RGBCloudPointFilter", "/briosa.CloudAndMeshOperations/RGBCloudPointFilter",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RGBCloudPointFilterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var filterOperation = request.HasRgbFilterOperation
            ? request.RgbFilterOperation
            : Api.RGBFilterOperation.ResetAndApplyFilter;
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Filter Name", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasFilterName ? request.FilterName : "Default Filter"), "SetStringArg"),
            new("Clouds To Be Filtered", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.CloudsToBeFiltered, "clouds_to_be_filtered", WorkerObjectTypeValue.Cloud), "SetCollectionObjectNameRefListArg"),
            new("Red Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasRedEnabled || request.RedEnabled), "SetBoolArg"),
            new("Red High Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasRedHighEnabled && request.RedHighEnabled), "SetBoolArg"),
            new("Red High Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasRedHighThreshold ? request.RedHighThreshold : 255), "SetIntegerArg"),
            new("Red Low Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasRedLowEnabled && request.RedLowEnabled), "SetBoolArg"),
            new("Red Low Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasRedLowThreshold ? request.RedLowThreshold : 0), "SetIntegerArg"),
            new("Green Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasGreenEnabled || request.GreenEnabled), "SetBoolArg"),
            new("Green High Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasGreenHighEnabled && request.GreenHighEnabled), "SetBoolArg"),
            new("Green High Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasGreenHighThreshold ? request.GreenHighThreshold : 255), "SetIntegerArg"),
            new("Green Low Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasGreenLowEnabled && request.GreenLowEnabled), "SetBoolArg"),
            new("Green Low Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasGreenLowThreshold ? request.GreenLowThreshold : 0), "SetIntegerArg"),
            new("Blue Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(!request.HasBlueEnabled || request.BlueEnabled), "SetBoolArg"),
            new("Blue High Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasBlueHighEnabled && request.BlueHighEnabled), "SetBoolArg"),
            new("Blue High Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasBlueHighThreshold ? request.BlueHighThreshold : 255), "SetIntegerArg"),
            new("Blue Low Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasBlueLowEnabled && request.BlueLowEnabled), "SetBoolArg"),
            new("Blue Low Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasBlueLowThreshold ? request.BlueLowThreshold : 0), "SetIntegerArg"),
            new("Gray Scale Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasGrayScaleEnabled && request.GrayScaleEnabled), "SetBoolArg"),
            new("Gray Scale High Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasGrayScaleHighEnabled && request.GrayScaleHighEnabled), "SetBoolArg"),
            new("Gray Scale High Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasGrayScaleHighThreshold ? request.GrayScaleHighThreshold : 255), "SetIntegerArg"),
            new("Gray Scale Low Enabled", WorkerMpValueKind.Logical, new WorkerBooleanValue(request.HasGrayScaleLowEnabled && request.GrayScaleLowEnabled), "SetBoolArg"),
            new("Gray Scale Low Threshold", WorkerMpValueKind.WholeNumber, new WorkerIntegerValue(request.HasGrayScaleLowThreshold ? request.GrayScaleLowThreshold : 0), "SetIntegerArg"),
            new("RGB Filter Operation", WorkerMpValueKind.Text, RgbFilterOperationMapper.ToMpValue(filterOperation), "SetStringArg")
        ], []);
    }

    public static Api.RGBCloudPointFilterResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

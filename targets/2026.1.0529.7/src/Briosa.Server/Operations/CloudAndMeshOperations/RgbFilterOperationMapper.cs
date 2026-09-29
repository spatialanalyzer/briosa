using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class RgbFilterOperationMapper
{
    public static WorkerTextValue ToMpValue(Api.RGBFilterOperation operation) => new(operation switch
    {
        Api.RGBFilterOperation.IncrementallyApplyFilter => "Incrementally Apply Filter",
        Api.RGBFilterOperation.ResetAndApplyFilter => "Reset and Apply Filter",
        Api.RGBFilterOperation.ResetAllCloudPointsVisible => "Reset All Cloud Points Visible",
        _ => throw new ArgumentException("Unsupported RGB filter operation.", nameof(operation))
    });
}

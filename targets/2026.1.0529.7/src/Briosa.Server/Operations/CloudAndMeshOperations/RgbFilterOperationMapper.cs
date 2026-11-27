using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.CloudAndMeshOperations;

internal static class RgbFilterOperationMapper
{
    public static WorkerTextValue ToMpValue(Api.RGBFilterOperation operation, string fieldName) => new(operation switch
    {
        Api.RGBFilterOperation.IncrementallyApplyFilter => "Incrementally Apply Filter",
        Api.RGBFilterOperation.ResetAndApplyFilter => "Reset and Apply Filter",
        Api.RGBFilterOperation.ResetAllCloudPointsVisible => "Reset All Cloud Points Visible",
        _ => throw RequestFieldViolation.SupportedChoice(fieldName)
    });
}

using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.AnalysisOperations;

internal static class PatchNormalShiftPointOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "analysis_operations.patch_normal_shift_point", "Patch Normal Shift - Point",
        "briosa.AnalysisOperations", "PatchNormalShiftPoint", "/briosa.AnalysisOperations/PatchNormalShiftPoint",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.PatchNormalShiftPointRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Plane Points Group Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.PlanePointsGroupName, "plane_points_group_name"), "SetCollectionObjectNameArg2"),
                new("Point to Shift", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.PointToShift, "point_to_shift"), "SetPointNameArg"),
                new("Resulting Point Name", WorkerMpValueKind.PointName,
                    PointNameMapper.Required(request.ResultingPointName, "resulting_point_name"), "SetPointNameArg"),
                new("Additional Material Thickness", WorkerMpValueKind.FloatingPoint,
                    new WorkerDoubleValue(request.HasAdditionalMaterialThickness ? request.AdditionalMaterialThickness : 0d), "SetDoubleArg")
            ], []);
    }

    public static Api.PatchNormalShiftPointResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        Execution = completed.Details
    };
}

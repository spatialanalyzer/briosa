using Briosa.Server.Security;
using Briosa.Server.Services;
using Grpc.Core;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ScaleBarOperations;

internal sealed class ScaleBarOperationsService(OperationExecutor executor)
    : Api.ScaleBarOperations.ScaleBarOperationsBase
{
    [OperationImplementation("scale_bar_operations.delete_scale_bar")]
    public override Task<Api.DeleteScaleBarResult> DeleteScaleBar(
        Api.DeleteScaleBarRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, DeleteScaleBarOperation.Descriptor,
            DeleteScaleBarOperation.CreateCommand, DeleteScaleBarOperation.OutputContracts,
            DeleteScaleBarOperation.CreateResult);

    [OperationImplementation("scale_bar_operations.get_scale_bar_stats")]
    public override Task<Api.GetScaleBarStatsResult> GetScaleBarStats(
        Api.GetScaleBarStatsRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, GetScaleBarStatsOperation.Descriptor,
            GetScaleBarStatsOperation.CreateCommand, GetScaleBarStatsOperation.OutputContracts,
            GetScaleBarStatsOperation.CreateResult);

    [OperationImplementation("scale_bar_operations.scale_bar_check")]
    public override Task<Api.ScaleBarCheckResult> ScaleBarCheck(
        Api.ScaleBarCheckRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, ScaleBarCheckOperation.Descriptor,
            ScaleBarCheckOperation.CreateCommand, ScaleBarCheckOperation.OutputContracts,
            ScaleBarCheckOperation.CreateResult);

    [OperationImplementation("scale_bar_operations.set_inward_positive_normal")]
    public override Task<Api.SetInwardPositiveNormalResult> SetInwardPositiveNormal(
        Api.SetInwardPositiveNormalRequest request,
        ServerCallContext context) =>
        executor.ExecuteAsync(request, context, SetInwardPositiveNormalOperation.Descriptor,
            SetInwardPositiveNormalOperation.CreateCommand, SetInwardPositiveNormalOperation.OutputContracts,
            SetInwardPositiveNormalOperation.CreateResult);

}

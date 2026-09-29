using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class DeleteFeatureChecksOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.delete_feature_checks", "Delete Feature Checks",
        "briosa.GdtOperations", "DeleteFeatureChecks", "/briosa.GdtOperations/DeleteFeatureChecks",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, ["destructive"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteFeatureChecksRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("Feature Check Name List", WorkerMpValueKind.CollectionObjectNameList,
                CollectionObjectNameMapper.RequiredList(request.FeatureChecks, "feature_checks"),
                "SetCollectionObjectNameRefListArg")
        ], []);
    }

    public static Api.DeleteFeatureChecksResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

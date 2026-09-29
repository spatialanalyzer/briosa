using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class RefreshDatumsFeatureChecksFromAnnotationsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.refresh_datums_feature_checks_from_annotations",
        "Refresh Datums/Feature Checks from Annotations", "briosa.GdtOperations",
        "RefreshDatumsFeatureChecksFromAnnotations", "/briosa.GdtOperations/RefreshDatumsFeatureChecksFromAnnotations",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.RefreshDatumsFeatureChecksFromAnnotationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection", WorkerMpValueKind.CollectionName,
                GdtCollectionNameMapper.Required(request.Collection), "SetCollectionNameArg")], []);
    }

    public static Api.RefreshDatumsFeatureChecksFromAnnotationsResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

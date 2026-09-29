using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.GdtOperations;

internal static class MakeFeatureChecksOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "gdt_operations.make_feature_checks", "Make Feature Checks",
        "briosa.GdtOperations", "MakeFeatureChecks", "/briosa.GdtOperations/MakeFeatureChecks",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];
    public static WorkerMpCommand CreateCommand(Api.MakeFeatureChecksRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Collection Name", WorkerMpValueKind.CollectionName,
                GdtCollectionNameMapper.Required(request.Collection), "SetCollectionNameArg")], []);
    }
    public static Api.MakeFeatureChecksResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

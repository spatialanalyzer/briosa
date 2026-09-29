using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.ReportingOperations;

internal static class DeleteSaDocOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "reporting_operations.delete_sa_doc", "Delete SA Doc", "briosa.ReportingOperations",
        "DeleteSaDoc", "/briosa.ReportingOperations/DeleteSaDoc", "state_mutation",
        Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);
    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.DeleteSaDocRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Doc Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.DocName, "doc_name"), "SetCollectionObjectNameArg2")], []);
    }

    public static Api.DeleteSaDocResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

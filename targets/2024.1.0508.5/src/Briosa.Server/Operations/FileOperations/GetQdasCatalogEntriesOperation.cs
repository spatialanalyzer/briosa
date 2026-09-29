using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.FileOperations;

internal static class GetQdasCatalogEntriesOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "file_operations.get_qdas_catalog_entries", "Get QDAS Catalog Entries", "briosa.FileOperations",
        "GetQdasCatalogEntries", "/briosa.FileOperations/GetQdasCatalogEntries", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("catalog_entries", "Catalog Entries", WorkerMpValueKind.StringList)];

    public static WorkerMpCommand CreateCommand(Api.GetQdasCatalogEntriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
        [
            new("K-Field Target", WorkerMpValueKind.Text,
                new WorkerTextValue(request.HasKFieldTarget ? request.KFieldTarget : string.Empty), "SetStringArg")
        ],
        [new("Catalog Entries", WorkerMpValueKind.StringList, "GetStringRefListArg")]);
    }

    public static Api.GetQdasCatalogEntriesResult CreateResult(SuccessfulOperationExecution completed)
    {
        var result = new Api.GetQdasCatalogEntriesResult { Execution = completed.Details };
        result.CatalogEntries.AddRange(completed.Execution.OutputValues[0]
            .RequireValue<WorkerStringListValue>().Values);
        return result;
    }
}

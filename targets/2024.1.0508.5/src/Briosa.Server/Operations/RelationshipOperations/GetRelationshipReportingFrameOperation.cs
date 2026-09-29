using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetRelationshipReportingFrameOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_relationship_reporting_frame", "Get Relationship Reporting Frame",
        "briosa.RelationshipOperations", "GetRelationshipReportingFrame",
        "/briosa.RelationshipOperations/GetRelationshipReportingFrame", "read_only",
        Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe,
        ["fixture_validation_pending"]);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
        [new("reporting_frame", "Reporting Frame", WorkerMpValueKind.CollectionObjectName)];

    public static WorkerMpCommand CreateCommand(Api.GetRelationshipReportingFrameRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"),
                "SetCollectionObjectNameArg2")],
            [new("Reporting Frame", WorkerMpValueKind.CollectionObjectName, "GetCollectionObjectNameArg")]);
    }

    public static Api.GetRelationshipReportingFrameResult CreateResult(SuccessfulOperationExecution completed) => new()
    {
        ReportingFrame = CollectionObjectNameMapper.ToProtocol(
            completed.Execution.OutputValues[0].RequireValue<WorkerCollectionObjectNameValue>()),
        Execution = completed.Details
    };
}

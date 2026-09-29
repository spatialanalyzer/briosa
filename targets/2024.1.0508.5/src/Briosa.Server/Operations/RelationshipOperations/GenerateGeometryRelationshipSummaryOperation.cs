using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GenerateGeometryRelationshipSummaryOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.generate_geometry_relationship_summary",
        "Generate Geometry Relationship Summary",
        "briosa.RelationshipOperations", "GenerateGeometryRelationshipSummary",
        "/briosa.RelationshipOperations/GenerateGeometryRelationshipSummary",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.GenerateGeometryRelationshipSummaryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Ref List", WorkerMpValueKind.CollectionObjectNameList,
                    CollectionObjectNameMapper.RequiredList(request.RelationshipRefList, "relationship_ref_list"), "SetCollectionObjectNameRefListArg"),
                new("Summary Table Name", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasSummaryTableName ? request.SummaryTableName : "Geometry Relationship Summary"), "SetStringArg")
            ], []);
    }

    public static Api.GenerateGeometryRelationshipSummaryResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

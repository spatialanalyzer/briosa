using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class GetGeomRelationshipAutoVectorsOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.get_geom_relationship_auto_vectors", "Get Geom Relationship Auto Vectors",
        "briosa.RelationshipOperations", "GetGeomRelationshipAutoVectors",
        "/briosa.RelationshipOperations/GetGeomRelationshipAutoVectors",
        "read_only", Api.OperationExecutionScope.GlobalStateRead, Api.ReplaySafety.Safe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } =
    [
        new("auto_vectors_nominal_enabled", "Auto Vectors Nominal (AVN) - Enabled?", WorkerMpValueKind.Logical),
        new("auto_vectors_nominal_name", "Auto Vectors Nominal (AVN) - Name", WorkerMpValueKind.CollectionObjectName),
        new("auto_vectors_fit_enabled", "Auto Vectors Fit (AVF) - Enabled?", WorkerMpValueKind.Logical),
        new("auto_vectors_fit_name", "Auto Vectors Fit (AVF) - Name", WorkerMpValueKind.CollectionObjectName),
        new("points_type", "Points Type", WorkerMpValueKind.Text)
    ];

    public static WorkerMpCommand CreateCommand(Api.GetGeomRelationshipAutoVectorsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2")],
            [
                new("Auto Vectors Nominal (AVN) - Enabled?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Auto Vectors Nominal (AVN) - Name", WorkerMpValueKind.CollectionObjectName,
                    "GetCollectionObjectNameArg", WorkerObjectTypeValue.VectorGroup),
                new("Auto Vectors Fit (AVF) - Enabled?", WorkerMpValueKind.Logical, "GetBoolArg"),
                new("Auto Vectors Fit (AVF) - Name", WorkerMpValueKind.CollectionObjectName,
                    "GetCollectionObjectNameArg", WorkerObjectTypeValue.VectorGroup),
                new("Points Type", WorkerMpValueKind.Text, "GetStringArg")
            ]);
    }

    public static Api.GetGeomRelationshipAutoVectorsResult CreateResult(SuccessfulOperationExecution completed)
    {
        var outputs = completed.Execution.OutputValues;
        return new()
        {
            AutoVectorsNominalEnabled = outputs[0].RequireValue<WorkerBooleanValue>().Value,
            AutoVectorsNominalName = CollectionObjectNameMapper.ToProtocol(
                outputs[1].RequireValue<WorkerCollectionObjectNameValue>()),
            AutoVectorsFitEnabled = outputs[2].RequireValue<WorkerBooleanValue>().Value,
            AutoVectorsFitName = CollectionObjectNameMapper.ToProtocol(
                outputs[3].RequireValue<WorkerCollectionObjectNameValue>()),
            PointsType = outputs[4].RequireValue<WorkerTextValue>().Value,
            Execution = completed.Details
        };
    }
}

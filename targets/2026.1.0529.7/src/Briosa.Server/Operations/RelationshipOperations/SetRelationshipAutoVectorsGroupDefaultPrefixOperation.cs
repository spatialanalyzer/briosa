using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipAutoVectorsGroupDefaultPrefixOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_auto_vectors_group_default_prefix",
        "Set Relationship Auto Vectors Group Default Prefix",
        "briosa.RelationshipOperations", "SetRelationshipAutoVectorsGroupDefaultPrefix",
        "/briosa.RelationshipOperations/SetRelationshipAutoVectorsGroupDefaultPrefix",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipAutoVectorsGroupDefaultPrefixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Geom Rel AVN VG Default Prefix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasGeomRelAvnVgDefaultPrefix ? request.GeomRelAvnVgDefaultPrefix : "GR-AVN-"), "SetStringArg"),
                new("Geom Rel AVF VG Default Prefix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasGeomRelAvfVgDefaultPrefix ? request.GeomRelAvfVgDefaultPrefix : "GR-AVF-"), "SetStringArg"),
                new("Non-Geom Rel VG Default Prefix", WorkerMpValueKind.Text,
                    new WorkerTextValue(request.HasNonGeomRelVgDefaultPrefix ? request.NonGeomRelVgDefaultPrefix : "Auto Vectors:"), "SetStringArg")
            ], []);
    }

    public static Api.SetRelationshipAutoVectorsGroupDefaultPrefixResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}

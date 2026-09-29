using Briosa.Server.Operations.Values;
using Briosa.Server.Security;
using Briosa.Server.Services;
using Briosa.Worker.Control;
using Api = global::Briosa;

namespace Briosa.Server.Operations.RelationshipOperations;

internal static class SetRelationshipDesiredMeasCountOperation
{
    public static OperationDescriptor Descriptor { get; } = new(
        "relationship_operations.set_relationship_desired_meas_count", "Set Relationship Desired Meas Count",
        "briosa.RelationshipOperations", "SetRelationshipDesiredMeasCount",
        "/briosa.RelationshipOperations/SetRelationshipDesiredMeasCount",
        "state_mutation", Api.OperationExecutionScope.GlobalStateMutation, Api.ReplaySafety.Unsafe, []);

    public static IReadOnlyList<OperationOutputContract> OutputContracts { get; } = [];

    public static WorkerMpCommand CreateCommand(Api.SetRelationshipDesiredMeasCountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(Descriptor.OperationId, Descriptor.MpStep,
            [
                new("Relationship Name", WorkerMpValueKind.CollectionObjectName,
                    CollectionObjectNameMapper.Required(request.RelationshipName, "relationship_name"), "SetCollectionObjectNameArg2"),
                new("Desired Measurement Count", WorkerMpValueKind.WholeNumber,
                    new WorkerIntegerValue(request.HasDesiredMeasurementCount ? request.DesiredMeasurementCount : 0), "SetIntegerArg")
            ], []);
    }

    public static Api.SetRelationshipDesiredMeasCountResult CreateResult(SuccessfulOperationExecution completed) =>
        new() { Execution = completed.Details };
}
